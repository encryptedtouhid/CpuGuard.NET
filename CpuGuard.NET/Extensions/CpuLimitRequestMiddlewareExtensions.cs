namespace CpuGuard.NET.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using System;
    public static class CpuLimitRequestMiddlewareExtensions
    {
        public static IApplicationBuilder UseCpuLimitRequestMiddleware(this IApplicationBuilder builder, double cpuLimitPercentage, TimeSpan monitoringInterval)
        {
            return builder.UseMiddleware<CpuLimitRequestMiddleware>(cpuLimitPercentage, monitoringInterval);
        }
    }
}
