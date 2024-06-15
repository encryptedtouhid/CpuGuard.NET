namespace CpuGuard.NET.Extensions
{
    using System;
    using Microsoft.AspNetCore.Builder;

    public static class CpuLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseCpuLimitMiddleware(this IApplicationBuilder builder, double cpuLimitPercentage, TimeSpan monitoringInterval)
        {
            return builder.UseMiddleware<CpuLimitMiddleware>(cpuLimitPercentage, monitoringInterval);
        }
    }
}
