using System;
using System.Diagnostics;
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
    /// Middleware that monitors and limits memory usage.
    /// </summary>
    public class MemoryGuardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MemoryGuardOptions _options;
        private readonly ResourceMonitorService? _resourceMonitor;

        /// <summary>
        /// Creates a new MemoryGuardMiddleware.
        /// </summary>
        public MemoryGuardMiddleware(
            RequestDelegate next,
            IOptions<MemoryGuardOptions> options,
            ResourceMonitorService? resourceMonitor = null)
        {
            _next = next;
            _options = options.Value;
            _resourceMonitor = resourceMonitor;
        }

        /// <summary>
        /// Processes the request and checks memory usage.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this path is excluded
            if (IsExcluded(context))
            {
                await _next(context);
                return;
            }

            // Get current memory usage
            double memoryValue;
            long memoryBytes;
            long totalMemory;

            if (_resourceMonitor != null)
            {
                memoryValue = _options.UsePercentage
                    ? _resourceMonitor.CurrentMemoryUsage
                    : _resourceMonitor.CurrentMemoryBytes;
                memoryBytes = _resourceMonitor.CurrentMemoryBytes;
                totalMemory = _resourceMonitor.TotalMemoryBytes;
            }
            else
            {
                var process = Process.GetCurrentProcess();
                memoryBytes = process.WorkingSet64;
                // Estimate total memory (use peak as heuristic since .NET Standard 2.1 doesn't expose total RAM)
                long peakMemory = process.PeakWorkingSet64;
                totalMemory = Math.Max(4L * 1024 * 1024 * 1024, peakMemory * 4);
                totalMemory = Math.Min(64L * 1024 * 1024 * 1024, totalMemory);
                memoryValue = _options.UsePercentage
                    ? ((double)memoryBytes / totalMemory) * 100
                    : memoryBytes;
            }

            // Check against threshold
            double threshold = _options.UsePercentage
                ? _options.MaxMemoryPercentage
                : _options.MaxMemoryBytes;

            if (memoryValue > threshold)
            {
                // Record metric
                GuardMetrics.RecordMemoryUsage(memoryValue);
                GuardMetrics.IncrementRequestsThrottled("memory");

                // Increment counter
                _resourceMonitor?.IncrementThrottled();

                // Raise event
                var eventArgs = new MemoryLimitExceededEventArgs
                {
                    Context = context,
                    MemoryUsageBytes = memoryBytes,
                    MemoryUsagePercentage = ((double)memoryBytes / totalMemory) * 100,
                    Threshold = threshold,
                    IsPercentageThreshold = _options.UsePercentage,
                    RequestPath = context.Request.Path,
                    HttpMethod = context.Request.Method
                };
                _options.RaiseMemoryLimitExceeded(eventArgs);

                // Handle response
                if (_options.CustomResponseHandler != null)
                {
                    await _options.CustomResponseHandler(context, memoryValue);
                }
                else
                {
                    context.Response.StatusCode = _options.ResponseStatusCode;
                    context.Response.ContentType = _options.ResponseContentType;
                    await context.Response.WriteAsync(_options.ResponseMessage);
                }

                return;
            }

            // Record metric for normal operation
            GuardMetrics.RecordMemoryUsage(_options.UsePercentage
                ? memoryValue
                : ((double)memoryBytes / totalMemory) * 100);

            await _next(context);
        }

        private bool IsExcluded(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Check path exclusions
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
