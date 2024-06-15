using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Threading.Tasks;


namespace CpuGuard.NET
{
    public class CpuLimitRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly double _cpuLimitPercentage;
        private readonly TimeSpan _monitoringInterval;

        public CpuLimitRequestMiddleware(RequestDelegate next, double cpuLimitPercentage, TimeSpan monitoringInterval)
        {
            _next = next;
            _cpuLimitPercentage = cpuLimitPercentage;
            _monitoringInterval = monitoringInterval;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuTime = process.TotalProcessorTime;

            await _next(context);

            var endTime = DateTime.UtcNow;
            var endCpuTime = process.TotalProcessorTime;

            var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;

            var cpuUsagePercentage = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;

            if (cpuUsagePercentage > _cpuLimitPercentage)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("CPU usage limit exceeded. Try again later.");
            }
        }
    }
}
