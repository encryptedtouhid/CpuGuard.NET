using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CpuGuard.NET.Dashboard;
using CpuGuard.NET.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CpuGuard.NET.Extensions
{
    /// <summary>
    /// Extension methods for mapping CpuGuard endpoints using middleware.
    /// </summary>
    public static class EndpointExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Maps the CpuGuard stats JSON endpoint.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="path">The route path. Default is /cpuguard/stats.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder MapCpuGuardStats(
            this IApplicationBuilder app,
            string path = "/cpuguard/stats")
        {
            return app.Map(path, statsApp =>
            {
                statsApp.Run(async context =>
                {
                    var statsService = context.RequestServices.GetService<GuardStatsService>();
                    if (statsService == null)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("GuardStatsService not configured. Call services.AddCpuGuard() first.");
                        return;
                    }

                    context.Response.ContentType = "application/json";
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";

                    var summary = statsService.GetSummary();
                    var json = JsonSerializer.Serialize(summary, _jsonOptions);
                    await context.Response.WriteAsync(json);
                });
            });
        }

        /// <summary>
        /// Maps the CpuGuard full stats JSON endpoint (includes history).
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="path">The route path. Default is /cpuguard/stats/full.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder MapCpuGuardFullStats(
            this IApplicationBuilder app,
            string path = "/cpuguard/stats/full")
        {
            return app.Map(path, statsApp =>
            {
                statsApp.Run(async context =>
                {
                    var statsService = context.RequestServices.GetService<GuardStatsService>();
                    if (statsService == null)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("GuardStatsService not configured. Call services.AddCpuGuard() first.");
                        return;
                    }

                    context.Response.ContentType = "application/json";
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";

                    var stats = statsService.GetStats();
                    var json = JsonSerializer.Serialize(stats, _jsonOptions);
                    await context.Response.WriteAsync(json);
                });
            });
        }

        /// <summary>
        /// Maps the CpuGuard dashboard HTML endpoint.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="path">The route path. Default is /cpuguard/dashboard.</param>
        /// <param name="statsPath">The stats endpoint path. Default is /cpuguard/stats.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder MapCpuGuardDashboard(
            this IApplicationBuilder app,
            string path = "/cpuguard/dashboard",
            string statsPath = "/cpuguard/stats")
        {
            return app.Map(path, dashboardApp =>
            {
                dashboardApp.Run(async context =>
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";

                    var html = GetDashboardHtml(statsPath);
                    await context.Response.WriteAsync(html);
                });
            });
        }

        /// <summary>
        /// Maps all CpuGuard endpoints (stats, full stats, and dashboard).
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="basePath">Base path for all endpoints. Default is /cpuguard.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder MapCpuGuardEndpoints(
            this IApplicationBuilder app,
            string basePath = "/cpuguard")
        {
            basePath = basePath.TrimEnd('/');

            app.MapCpuGuardStats($"{basePath}/stats");
            app.MapCpuGuardFullStats($"{basePath}/stats/full");
            app.MapCpuGuardDashboard($"{basePath}/dashboard", $"{basePath}/stats");

            return app;
        }

        private static string GetDashboardHtml(string statsEndpoint)
        {
            var assembly = typeof(EndpointExtensions).Assembly;
            var resourceName = "CpuGuard.NET.Dashboard.dashboard.html";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var html = reader.ReadToEnd();
                return html.Replace("{{STATS_ENDPOINT}}", statsEndpoint);
            }

            return GenerateFallbackDashboard(statsEndpoint);
        }

        private static string GenerateFallbackDashboard(string statsEndpoint)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <title>CpuGuard.NET Dashboard</title>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: sans-serif; background: #1a1a2e; color: #fff; padding: 20px; }}
        h1 {{ color: #00d4ff; }}
        .stat {{ background: #16213e; padding: 20px; margin: 10px 0; border-radius: 8px; }}
        .value {{ font-size: 2em; color: #00d4ff; }}
    </style>
</head>
<body>
    <h1>CpuGuard.NET Dashboard</h1>
    <div class=""stat""><strong>CPU:</strong> <span class=""value"" id=""cpu"">--</span>%</div>
    <div class=""stat""><strong>Memory:</strong> <span class=""value"" id=""mem"">--</span>%</div>
    <div class=""stat""><strong>Throttled:</strong> <span class=""value"" id=""throttled"">--</span></div>
    <script>
        async function update() {{
            const r = await fetch('{statsEndpoint}');
            const d = await r.json();
            document.getElementById('cpu').textContent = d.currentCpuUsage.toFixed(1);
            document.getElementById('mem').textContent = d.currentMemoryUsage.toFixed(1);
            document.getElementById('throttled').textContent = d.totalRequestsThrottled;
        }}
        update(); setInterval(update, 2000);
    </script>
</body>
</html>";
        }
    }
}
