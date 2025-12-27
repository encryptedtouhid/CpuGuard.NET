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
    /// Middleware that monitors CPU usage on a per-request basis.
    /// Measures CPU time consumed by each individual request.
    /// </summary>
    public class CpuLimitRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CpuGuardOptions _options;
        private readonly ResourceMonitorService? _resourceMonitor;
        private readonly GuardStatsService? _statsService;

        // Legacy constructor for backward compatibility
        /// <summary>
        /// Creates a new CpuLimitRequestMiddleware with explicit parameters (legacy).
        /// </summary>
        public CpuLimitRequestMiddleware(RequestDelegate next, double cpuLimitPercentage, TimeSpan monitoringInterval)
        {
            _next = next;
            _options = new CpuGuardOptions
            {
                MaxCpuPercentage = cpuLimitPercentage,
                MonitoringInterval = monitoringInterval
            };
            _resourceMonitor = null;
            _statsService = null;
        }

        /// <summary>
        /// Creates a new CpuLimitRequestMiddleware with options pattern.
        /// </summary>
        public CpuLimitRequestMiddleware(
            RequestDelegate next,
            IOptions<CpuGuardOptions> options,
            ResourceMonitorService? resourceMonitor = null,
            GuardStatsService? statsService = null)
        {
            _next = next;
            _options = options.Value;
            _resourceMonitor = resourceMonitor;
            _statsService = statsService;
        }

        /// <summary>
        /// Processes the request and checks CPU usage after request completion.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this path is excluded
            if (IsExcluded(context))
            {
                await _next(context);
                return;
            }

            // Track request for stats
            _statsService?.IncrementTotalRequests();

            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuTime = process.TotalProcessorTime;

            await _next(context);

            var endTime = DateTime.UtcNow;
            var endCpuTime = process.TotalProcessorTime;

            var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;

            // Record request duration
            GuardMetrics.RecordRequestDuration(totalMsPassed);

            if (totalMsPassed <= 0)
                return;

            var cpuUsagePercentage = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;
            cpuUsagePercentage = Math.Min(100, Math.Max(0, cpuUsagePercentage)); // Clamp to 0-100

            // Record metric
            GuardMetrics.RecordCpuUsage(cpuUsagePercentage);

            if (cpuUsagePercentage > _options.MaxCpuPercentage)
            {
                // Record metric
                GuardMetrics.IncrementRequestsThrottled("cpu_request");

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

                // Handle response (note: response may have already started)
                if (!context.Response.HasStarted)
                {
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
