using System;
using System.Threading;
using System.Threading.Tasks;
using CpuGuard.NET.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CpuGuard.NET.HealthChecks
{
    /// <summary>
    /// Health check that reports CPU usage status.
    /// </summary>
    public class CpuHealthCheck : IHealthCheck
    {
        private readonly ResourceMonitorService _resourceMonitor;
        private readonly CpuHealthCheckOptions _options;

        /// <summary>
        /// Creates a new CpuHealthCheck.
        /// </summary>
        public CpuHealthCheck(
            ResourceMonitorService resourceMonitor,
            IOptions<CpuHealthCheckOptions>? options = null)
        {
            _resourceMonitor = resourceMonitor;
            _options = options?.Value ?? new CpuHealthCheckOptions();
        }

        /// <summary>
        /// Checks CPU health status.
        /// </summary>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var cpuUsage = _resourceMonitor.CurrentCpuUsage;
            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "cpu_usage_percent", Math.Round(cpuUsage, 2) },
                { "average_cpu_percent", Math.Round(_resourceMonitor.AverageCpuUsage, 2) },
                { "peak_cpu_percent", Math.Round(_resourceMonitor.PeakCpuUsage, 2) }
            };

            if (cpuUsage >= _options.UnhealthyThreshold)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"CPU usage is critical: {cpuUsage:F1}%",
                    data: data));
            }

            if (cpuUsage >= _options.DegradedThreshold)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"CPU usage is elevated: {cpuUsage:F1}%",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"CPU usage is normal: {cpuUsage:F1}%",
                data: data));
        }
    }

    /// <summary>
    /// Options for CPU health check thresholds.
    /// </summary>
    public class CpuHealthCheckOptions
    {
        /// <summary>
        /// CPU percentage above which status is Degraded. Default is 70%.
        /// </summary>
        public double DegradedThreshold { get; set; } = 70.0;

        /// <summary>
        /// CPU percentage above which status is Unhealthy. Default is 90%.
        /// </summary>
        public double UnhealthyThreshold { get; set; } = 90.0;
    }
}
