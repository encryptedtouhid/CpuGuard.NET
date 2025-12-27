using System;
using CpuGuard.NET.Configuration;
using CpuGuard.NET.Dashboard;
using CpuGuard.NET.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CpuGuard.NET.Extensions
{
    /// <summary>
    /// Extension methods for adding CpuGuard middleware to the application pipeline.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the CpuGuard middleware using configured options.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseCpuGuard(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CpuLimitMiddleware>();
        }

        /// <summary>
        /// Adds the MemoryGuard middleware using configured options.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseMemoryGuard(this IApplicationBuilder app)
        {
            return app.UseMiddleware<MemoryGuardMiddleware>();
        }

        /// <summary>
        /// Adds the GradualThrottling middleware using configured options.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseGradualThrottling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GradualThrottlingMiddleware>();
        }

        /// <summary>
        /// Adds the RateLimit middleware using configured options.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseCpuGuardRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitMiddleware>();
        }

        /// <summary>
        /// Adds the CpuGuard dashboard middleware.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="path">Path for the dashboard. Default is /cpuguard/dashboard.</param>
        /// <param name="statsPath">Path for the stats API. Default is /cpuguard/stats.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseCpuGuardDashboard(
            this IApplicationBuilder app,
            string path = "/cpuguard/dashboard",
            string statsPath = "/cpuguard/stats")
        {
            return app.UseMiddleware<DashboardMiddleware>(path, statsPath);
        }

        /// <summary>
        /// Adds all CpuGuard middleware in recommended order.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseCpuGuardAll(this IApplicationBuilder app)
        {
            return app
                .UseCpuGuardRateLimiting()
                .UseGradualThrottling()
                .UseCpuGuard()
                .UseMemoryGuard();
        }
    }
}
