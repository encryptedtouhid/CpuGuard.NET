using System;
using CpuGuard.NET.Configuration;
using CpuGuard.NET.Dashboard;
using CpuGuard.NET.HealthChecks;
using CpuGuard.NET.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CpuGuard.NET.Extensions
{
    /// <summary>
    /// Extension methods for registering CpuGuard services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds CpuGuard services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for CpuGuardOptions.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddCpuGuard(
            this IServiceCollection services,
            Action<CpuGuardOptions>? configure = null)
        {
            // Register options
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<CpuGuardOptions>(_ => { });
            }

            // Register core services
            services.AddSingleton<ResourceMonitorService>();
            services.AddSingleton<GuardStatsService>();
            services.AddSingleton<StatsEndpoint>();

            // Register as hosted service for background monitoring
            services.AddHostedService(sp => sp.GetRequiredService<ResourceMonitorService>());

            return services;
        }

        /// <summary>
        /// Adds MemoryGuard services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for MemoryGuardOptions.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddMemoryGuard(
            this IServiceCollection services,
            Action<MemoryGuardOptions>? configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<MemoryGuardOptions>(_ => { });
            }

            // Ensure ResourceMonitorService is registered
            EnsureResourceMonitor(services);

            return services;
        }

        /// <summary>
        /// Adds gradual throttling services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for ThrottlingOptions.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddGradualThrottling(
            this IServiceCollection services,
            Action<ThrottlingOptions>? configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<ThrottlingOptions>(_ => { });
            }

            // Ensure ResourceMonitorService is registered
            EnsureResourceMonitor(services);

            return services;
        }

        /// <summary>
        /// Adds rate limiting services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for RateLimitOptions.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddCpuGuardRateLimiting(
            this IServiceCollection services,
            Action<RateLimitOptions>? configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<RateLimitOptions>(_ => { });
            }

            // Ensure ResourceMonitorService is registered
            EnsureResourceMonitor(services);

            return services;
        }

        /// <summary>
        /// Adds all CpuGuard services with default configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddCpuGuardAll(this IServiceCollection services)
        {
            return services
                .AddCpuGuard()
                .AddMemoryGuard()
                .AddGradualThrottling()
                .AddCpuGuardRateLimiting();
        }

        private static void EnsureResourceMonitor(IServiceCollection services)
        {
            // Check if ResourceMonitorService is already registered
            var descriptor = new ServiceDescriptor(
                typeof(ResourceMonitorService),
                typeof(ResourceMonitorService),
                ServiceLifetime.Singleton);

            bool alreadyRegistered = false;
            foreach (var service in services)
            {
                if (service.ServiceType == typeof(ResourceMonitorService))
                {
                    alreadyRegistered = true;
                    break;
                }
            }

            if (!alreadyRegistered)
            {
                services.AddSingleton<ResourceMonitorService>();
                services.AddSingleton<GuardStatsService>();
                services.AddSingleton<StatsEndpoint>();
                services.AddHostedService(sp => sp.GetRequiredService<ResourceMonitorService>());
            }
        }
    }
}
