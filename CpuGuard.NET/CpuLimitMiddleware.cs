using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CpuGuard.NET.Configuration;
using CpuGuard.NET.Events;
using CpuGuard.NET.Metrics;
using CpuGuard.NET.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CpuGuard.NET
{
    /// <summary>
    /// Middleware that monitors and limits CPU usage for the application.
    /// </summary>
    public class CpuLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CpuGuardOptions _options;
        private readonly ResourceMonitorService? _resourceMonitor;

        // Legacy constructor for backward compatibility
        /// <summary>
        /// Creates a new CpuLimitMiddleware with explicit parameters (legacy).
        /// </summary>
        public CpuLimitMiddleware(RequestDelegate next, double cpuLimitPercentage, TimeSpan monitoringInterval)
        {
            _next = next;
            _options = new CpuGuardOptions
            {
                MaxCpuPercentage = cpuLimitPercentage,
                MonitoringInterval = monitoringInterval
            };
            _resourceMonitor = null;
        }

        /// <summary>
        /// Creates a new CpuLimitMiddleware with options pattern.
        /// </summary>
        public CpuLimitMiddleware(
            RequestDelegate next,
            IOptions<CpuGuardOptions> options,
            ResourceMonitorService? resourceMonitor = null)
        {
            _next = next;
            _options = options.Value;
            _resourceMonitor = resourceMonitor;
        }

        /// <summary>
        /// Processes the request and checks CPU usage.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this path is excluded
            if (IsExcluded(context))
            {
                await _next(context);
                return;
            }

            // Get CPU usage - prefer background sampled value if available
            double cpuUsagePercentage;

            if (_resourceMonitor != null)
            {
                cpuUsagePercentage = _resourceMonitor.CurrentCpuUsage;
            }
            else
            {
                // Fallback to per-request measurement
                var process = Process.GetCurrentProcess();
                var startTime = DateTime.UtcNow;
                var startCpuTime = process.TotalProcessorTime;

                await _next(context);

                var endTime = DateTime.UtcNow;
                var endCpuTime = process.TotalProcessorTime;

                var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;

                cpuUsagePercentage = totalMsPassed > 0
                    ? (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100
                    : 0;

                // For legacy mode, check after request completes
                if (cpuUsagePercentage > _options.MaxCpuPercentage)
                {
                    await HandleLimitExceeded(context, cpuUsagePercentage);
                }

                return;
            }

            // Record metric
            GuardMetrics.RecordCpuUsage(cpuUsagePercentage);

            // Check against threshold (pre-request check when using ResourceMonitor)
            if (cpuUsagePercentage > _options.MaxCpuPercentage)
            {
                await HandleLimitExceeded(context, cpuUsagePercentage);
                return;
            }

            await _next(context);
        }

        private async Task HandleLimitExceeded(HttpContext context, double cpuUsagePercentage)
        {
            // Record metric
            GuardMetrics.IncrementRequestsThrottled("cpu");

            // Increment counter
            _resourceMonitor?.IncrementThrottled();

            // Raise event
            var eventArgs = new CpuLimitExceededEventArgs
            {
                Context = context,
                CpuUsagePercentage = cpuUsagePercentage,
                Threshold = _options.MaxCpuPercentage,
                RequestPath = context.Request.Path,
                HttpMethod = context.Request.Method
            };
            _options.RaiseCpuLimitExceeded(eventArgs);

            // Handle response
            if (_options.CustomResponseHandler != null)
            {
                await _options.CustomResponseHandler(context, cpuUsagePercentage);
            }
            else
            {
                context.Response.StatusCode = _options.ResponseStatusCode;
                context.Response.ContentType = _options.ResponseContentType;
                await context.Response.WriteAsync(_options.ResponseMessage);
            }
        }

        private bool IsExcluded(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Check exact path exclusions
            if (_options.ExcludedPaths.Contains(path))
                return true;

            // Check prefix exclusions
            foreach (var excludedPath in _options.ExcludedPaths)
            {
                if (path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Check custom predicate
            if (_options.ExclusionPredicate != null && _options.ExclusionPredicate(context))
                return true;

            return false;
        }
    }
}
