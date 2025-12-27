using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CpuGuard.NET.Configuration;
using CpuGuard.NET.Events;
using CpuGuard.NET.Metrics;
using CpuGuard.NET.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CpuGuard.NET.Middleware
{
    /// <summary>
    /// Middleware that implements rate limiting with optional CPU-aware adjustment.
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitOptions _options;
        private readonly ResourceMonitorService? _resourceMonitor;
        private readonly ConcurrentDictionary<string, ClientRateLimitInfo> _clients;
        private readonly Timer _cleanupTimer;

        /// <summary>
        /// Event raised when rate limit is exceeded.
        /// </summary>
        public event EventHandler<RateLimitExceededEventArgs>? OnRateLimitExceeded;

        /// <summary>
        /// Creates a new RateLimitMiddleware.
        /// </summary>
        public RateLimitMiddleware(
            RequestDelegate next,
            IOptions<RateLimitOptions> options,
            ResourceMonitorService? resourceMonitor = null)
        {
            _next = next;
            _options = options.Value;
            _resourceMonitor = resourceMonitor;
            _clients = new ConcurrentDictionary<string, ClientRateLimitInfo>();

            // Cleanup old entries every minute
            _cleanupTimer = new Timer(CleanupOldEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Processes the request with rate limiting.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this path is excluded
            if (IsExcluded(context))
            {
                await _next(context);
                return;
            }

            string clientId = GetClientIdentifier(context);
            var clientInfo = _clients.GetOrAdd(clientId, _ => new ClientRateLimitInfo());

            int effectiveLimit = GetEffectiveRateLimit();
            var now = DateTime.UtcNow;

            bool allowed;
            int requestCount;
            TimeSpan resetIn;

            lock (clientInfo)
            {
                switch (_options.Mode)
                {
                    case RateLimitMode.TokenBucket:
                        (allowed, requestCount, resetIn) = CheckTokenBucket(clientInfo, now);
                        break;

                    case RateLimitMode.SlidingWindow:
                        (allowed, requestCount, resetIn) = CheckSlidingWindow(clientInfo, now, effectiveLimit);
                        break;

                    case RateLimitMode.FixedWindow:
                    default:
                        (allowed, requestCount, resetIn) = CheckFixedWindow(clientInfo, now, effectiveLimit);
                        break;
                }
            }

            // Add rate limit headers
            if (_options.IncludeRateLimitHeaders)
            {
                context.Response.Headers["X-RateLimit-Limit"] = effectiveLimit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, effectiveLimit - requestCount).ToString();
                context.Response.Headers["X-RateLimit-Reset"] = ((long)resetIn.TotalSeconds).ToString();
            }

            if (!allowed)
            {
                GuardMetrics.IncrementRequestsRateLimited();
                _resourceMonitor?.IncrementRateLimited();

                RaiseRateLimitExceededEvent(context, clientId, requestCount, effectiveLimit, resetIn);

                if (_options.CustomResponseHandler != null)
                {
                    await _options.CustomResponseHandler(context, effectiveLimit - requestCount, resetIn);
                }
                else
                {
                    context.Response.StatusCode = _options.ResponseStatusCode;
                    context.Response.ContentType = _options.ResponseContentType;
                    context.Response.Headers["Retry-After"] = ((long)resetIn.TotalSeconds).ToString();
                    await context.Response.WriteAsync(_options.ResponseMessage);
                }

                return;
            }

            await _next(context);
        }

        private int GetEffectiveRateLimit()
        {
            int baseLimit = _options.RequestsPerWindow;

            if (!_options.CombineWithCpuLimit || _resourceMonitor == null)
                return baseLimit;

            double cpuUsage = _resourceMonitor.CurrentCpuUsage;

            if (cpuUsage >= _options.CpuThresholdForStricterLimits)
            {
                return (int)(baseLimit * _options.HighCpuRateLimitFactor);
            }

            return baseLimit;
        }

        private (bool allowed, int count, TimeSpan resetIn) CheckFixedWindow(
            ClientRateLimitInfo info, DateTime now, int limit)
        {
            if (now >= info.WindowStart + _options.Window)
            {
                info.WindowStart = now;
                info.RequestCount = 0;
            }

            info.RequestCount++;
            var resetIn = (info.WindowStart + _options.Window) - now;

            return (info.RequestCount <= limit, info.RequestCount, resetIn);
        }

        private (bool allowed, int count, TimeSpan resetIn) CheckSlidingWindow(
            ClientRateLimitInfo info, DateTime now, int limit)
        {
            var windowStart = now - _options.Window;

            // Remove old timestamps
            while (info.RequestTimestamps.Count > 0 &&
                   info.RequestTimestamps.TryPeek(out var oldest) &&
                   oldest < windowStart)
            {
                info.RequestTimestamps.TryDequeue(out _);
            }

            int count = info.RequestTimestamps.Count;

            if (count >= limit)
            {
                // Find when the oldest request in current window will expire
                if (info.RequestTimestamps.TryPeek(out var oldest))
                {
                    return (false, count, oldest + _options.Window - now);
                }
                return (false, count, _options.Window);
            }

            info.RequestTimestamps.Enqueue(now);
            return (true, count + 1, _options.Window);
        }

        private (bool allowed, int count, TimeSpan resetIn) CheckTokenBucket(
            ClientRateLimitInfo info, DateTime now)
        {
            double tokensPerSecond = _options.TokensPerSecond ??
                (_options.RequestsPerWindow / _options.Window.TotalSeconds);
            int bucketSize = _options.BucketSize ?? _options.RequestsPerWindow;

            // Calculate tokens to add since last request
            var elapsed = now - info.LastTokenUpdate;
            var tokensToAdd = elapsed.TotalSeconds * tokensPerSecond;
            info.Tokens = Math.Min(bucketSize, info.Tokens + tokensToAdd);
            info.LastTokenUpdate = now;

            if (info.Tokens >= 1)
            {
                info.Tokens -= 1;
                return (true, bucketSize - (int)info.Tokens, TimeSpan.FromSeconds(1 / tokensPerSecond));
            }

            // Calculate time until next token
            var timeToNextToken = TimeSpan.FromSeconds((1 - info.Tokens) / tokensPerSecond);
            return (false, bucketSize, timeToNextToken);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            if (_options.ClientIdentifierFactory != null)
                return _options.ClientIdentifierFactory(context);

            // Default: use IP address
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private void RaiseRateLimitExceededEvent(HttpContext context, string clientId,
            int requestCount, int limit, TimeSpan resetIn)
        {
            OnRateLimitExceeded?.Invoke(this, new RateLimitExceededEventArgs
            {
                Context = context,
                ClientIdentifier = clientId,
                RequestCount = requestCount,
                RateLimit = limit,
                ResetIn = resetIn,
                RequestPath = context.Request.Path,
                HttpMethod = context.Request.Method
            });
        }

        private void CleanupOldEntries(object? state)
        {
            var cutoff = DateTime.UtcNow - _options.Window - TimeSpan.FromMinutes(5);

            foreach (var kvp in _clients)
            {
                if (kvp.Value.LastAccess < cutoff)
                {
                    _clients.TryRemove(kvp.Key, out _);
                }
            }
        }

        private bool IsExcluded(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            if (_options.ExcludedPaths.Contains(path))
                return true;

            foreach (var excludedPath in _options.ExcludedPaths)
            {
                if (path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (_options.ExclusionPredicate != null && _options.ExclusionPredicate(context))
                return true;

            return false;
        }

        private class ClientRateLimitInfo
        {
            public DateTime WindowStart { get; set; } = DateTime.UtcNow;
            public int RequestCount { get; set; }
            public ConcurrentQueue<DateTime> RequestTimestamps { get; } = new ConcurrentQueue<DateTime>();
            public double Tokens { get; set; }
            public DateTime LastTokenUpdate { get; set; } = DateTime.UtcNow;
            public DateTime LastAccess { get; set; } = DateTime.UtcNow;
        }
    }
}
