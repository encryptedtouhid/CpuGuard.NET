using System;
using System.Threading;
using System.Threading.Tasks;
using CpuGuard.NET.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CpuGuard.NET.HealthChecks
{
    /// <summary>
    /// Health check that reports memory usage status.
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly ResourceMonitorService _resourceMonitor;
        private readonly MemoryHealthCheckOptions _options;

        /// <summary>
        /// Creates a new MemoryHealthCheck.
        /// </summary>
        public MemoryHealthCheck(
            ResourceMonitorService resourceMonitor,
            IOptions<MemoryHealthCheckOptions>? options = null)
        {
            _resourceMonitor = resourceMonitor;
            _options = options?.Value ?? new MemoryHealthCheckOptions();
        }

        /// <summary>
        /// Checks memory health status.
        /// </summary>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var memoryUsage = _resourceMonitor.CurrentMemoryUsage;
            var memoryBytes = _resourceMonitor.CurrentMemoryBytes;
            var totalBytes = _resourceMonitor.TotalMemoryBytes;

            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "memory_usage_percent", Math.Round(memoryUsage, 2) },
                { "memory_used_mb", Math.Round(memoryBytes / (1024.0 * 1024.0), 2) },
                { "memory_total_mb", Math.Round(totalBytes / (1024.0 * 1024.0), 2) },
                { "average_memory_percent", Math.Round(_resourceMonitor.AverageMemoryUsage, 2) },
                { "peak_memory_percent", Math.Round(_resourceMonitor.PeakMemoryUsage, 2) }
            };

            if (memoryUsage >= _options.UnhealthyThreshold)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Memory usage is critical: {memoryUsage:F1}%",
                    data: data));
            }

            if (memoryUsage >= _options.DegradedThreshold)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Memory usage is elevated: {memoryUsage:F1}%",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Memory usage is normal: {memoryUsage:F1}%",
                data: data));
        }
    }

    /// <summary>
    /// Options for memory health check thresholds.
    /// </summary>
    public class MemoryHealthCheckOptions
    {
        /// <summary>
        /// Memory percentage above which status is Degraded. Default is 70%.
        /// </summary>
        public double DegradedThreshold { get; set; } = 70.0;

        /// <summary>
        /// Memory percentage above which status is Unhealthy. Default is 90%.
        /// </summary>
        public double UnhealthyThreshold { get; set; } = 90.0;
    }
}
