using System;
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
    /// Middleware that gradually throttles requests based on CPU/memory usage.
    /// Instead of hard cutoffs, it progressively delays requests as resource usage increases.
    /// </summary>
    public class GradualThrottlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ThrottlingOptions _options;
        private readonly ResourceMonitorService? _resourceMonitor;

        /// <summary>
        /// Event raised when throttling is applied.
        /// </summary>
        public event EventHandler<ThrottlingEventArgs>? OnThrottling;

        /// <summary>
        /// Creates a new GradualThrottlingMiddleware.
        /// </summary>
        public GradualThrottlingMiddleware(
            RequestDelegate next,
            IOptions<ThrottlingOptions> options,
            ResourceMonitorService? resourceMonitor = null)
        {
            _next = next;
            _options = options.Value;
            _resourceMonitor = resourceMonitor;
        }

        /// <summary>
        /// Processes the request with gradual throttling.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this path is excluded
            if (IsExcluded(context))
            {
                await _next(context);
                return;
            }

            // Get current resource usage
            double resourceUsage = GetResourceUsage();

            // Check against hard limit (reject)
            if (resourceUsage >= _options.HardLimitPercentage)
            {
                GuardMetrics.IncrementRequestsThrottled("hard_limit");
                _resourceMonitor?.IncrementThrottled();

                RaiseThrottlingEvent(context, resourceUsage, TimeSpan.Zero, true);

                if (_options.CustomResponseHandler != null)
                {
                    await _options.CustomResponseHandler(context, resourceUsage);
                }
                else
                {
                    context.Response.StatusCode = _options.ResponseStatusCode;
                    context.Response.ContentType = _options.ResponseContentType;
                    await context.Response.WriteAsync(_options.ResponseMessage);
                }

                return;
            }

            // Check against soft limit (delay)
            if (resourceUsage >= _options.SoftLimitPercentage)
            {
                var delay = CalculateDelay(resourceUsage);

                if (delay > TimeSpan.Zero)
                {
                    GuardMetrics.IncrementRequestsDelayed();
                    GuardMetrics.RecordDelayApplied(delay.TotalMilliseconds);
                    _resourceMonitor?.IncrementDelayed();

                    RaiseThrottlingEvent(context, resourceUsage, delay, false);

                    await Task.Delay(delay);
                }
            }

            await _next(context);
        }

        private double GetResourceUsage()
        {
            if (_resourceMonitor == null)
                return 0;

            double cpuUsage = _resourceMonitor.CurrentCpuUsage;

            if (!_options.IncludeMemory)
                return cpuUsage;

            double memoryUsage = _resourceMonitor.CurrentMemoryUsage;
            double cpuWeight = 1.0 - _options.MemoryWeight;

            return (cpuUsage * cpuWeight) + (memoryUsage * _options.MemoryWeight);
        }

        private TimeSpan CalculateDelay(double resourceUsage)
        {
            // Calculate position between soft and hard limits (0-1)
            double range = _options.HardLimitPercentage - _options.SoftLimitPercentage;
            if (range <= 0) return TimeSpan.Zero;

            double position = (resourceUsage - _options.SoftLimitPercentage) / range;
            position = Math.Max(0, Math.Min(1, position)); // Clamp to 0-1

            double delayMs;

            switch (_options.Mode)
            {
                case ThrottlingMode.Exponential:
                    // Exponential: delay increases slowly at first, then rapidly
                    delayMs = _options.MinDelay.TotalMilliseconds +
                        (Math.Pow(position, 2) * (_options.MaxDelay.TotalMilliseconds - _options.MinDelay.TotalMilliseconds));
                    break;

                case ThrottlingMode.Linear:
                default:
                    // Linear: delay increases proportionally
                    delayMs = _options.MinDelay.TotalMilliseconds +
                        (position * (_options.MaxDelay.TotalMilliseconds - _options.MinDelay.TotalMilliseconds));
                    break;
            }

            return TimeSpan.FromMilliseconds(delayMs);
        }

        private void RaiseThrottlingEvent(HttpContext context, double cpuUsage, TimeSpan delay, bool wasRejected)
        {
            OnThrottling?.Invoke(this, new ThrottlingEventArgs
            {
                Context = context,
                CpuUsagePercentage = cpuUsage,
                DelayApplied = delay,
                WasRejected = wasRejected,
                SoftLimit = _options.SoftLimitPercentage,
                HardLimit = _options.HardLimitPercentage,
                RequestPath = context.Request.Path,
                HttpMethod = context.Request.Method
            });
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
    }
}
