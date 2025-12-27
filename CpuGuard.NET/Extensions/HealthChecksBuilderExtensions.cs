using System;
using CpuGuard.NET.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CpuGuard.NET.Extensions
{
    /// <summary>
    /// Extension methods for adding CpuGuard health checks.
    /// </summary>
    public static class HealthChecksBuilderExtensions
    {
        /// <summary>
        /// Adds CpuGuard CPU health check.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. Default is "cpuguard-cpu".</param>
        /// <param name="failureStatus">Status when check fails. Default is Unhealthy.</param>
        /// <param name="tags">Tags for the health check.</param>
        /// <param name="configure">Optional configuration for thresholds.</param>
        /// <returns>The health checks builder.</returns>
        public static IHealthChecksBuilder AddCpuGuardCpuCheck(
            this IHealthChecksBuilder builder,
            string name = "cpuguard-cpu",
            HealthStatus? failureStatus = null,
            string[]? tags = null,
            Action<CpuHealthCheckOptions>? configure = null)
        {
            if (configure != null)
            {
                builder.Services.Configure(configure);
            }

            return builder.AddCheck<CpuHealthCheck>(
                name,
                failureStatus,
                tags ?? new[] { "cpuguard", "cpu", "resource" });
        }

        /// <summary>
        /// Adds CpuGuard memory health check.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. Default is "cpuguard-memory".</param>
        /// <param name="failureStatus">Status when check fails. Default is Unhealthy.</param>
        /// <param name="tags">Tags for the health check.</param>
        /// <param name="configure">Optional configuration for thresholds.</param>
        /// <returns>The health checks builder.</returns>
        public static IHealthChecksBuilder AddCpuGuardMemoryCheck(
            this IHealthChecksBuilder builder,
            string name = "cpuguard-memory",
            HealthStatus? failureStatus = null,
            string[]? tags = null,
            Action<MemoryHealthCheckOptions>? configure = null)
        {
            if (configure != null)
            {
                builder.Services.Configure(configure);
            }

            return builder.AddCheck<MemoryHealthCheck>(
                name,
                failureStatus,
                tags ?? new[] { "cpuguard", "memory", "resource" });
        }

        /// <summary>
        /// Adds all CpuGuard health checks (CPU and Memory).
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="tags">Additional tags for the health checks.</param>
        /// <returns>The health checks builder.</returns>
        public static IHealthChecksBuilder AddCpuGuardHealthChecks(
            this IHealthChecksBuilder builder,
            string[]? tags = null)
        {
            string[] allTags;
            if (tags != null)
            {
                allTags = new string[2 + tags.Length];
                allTags[0] = "cpuguard";
                allTags[1] = "resource";
                Array.Copy(tags, 0, allTags, 2, tags.Length);
            }
            else
            {
                allTags = new[] { "cpuguard", "resource" };
            }

            return builder
                .AddCpuGuardCpuCheck(tags: allTags)
                .AddCpuGuardMemoryCheck(tags: allTags);
        }
    }
}
