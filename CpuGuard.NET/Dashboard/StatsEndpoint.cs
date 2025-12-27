using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CpuGuard.NET.Services;
using Microsoft.AspNetCore.Http;

namespace CpuGuard.NET.Dashboard
{
    /// <summary>
    /// Endpoint handler for the stats JSON API.
    /// </summary>
    public class StatsEndpoint
    {
        private readonly GuardStatsService _statsService;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Creates a new StatsEndpoint.
        /// </summary>
        public StatsEndpoint(GuardStatsService statsService)
        {
            _statsService = statsService;
        }

        /// <summary>
        /// Handles the stats endpoint request.
        /// </summary>
        public async Task HandleAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";

            var summary = _statsService.GetSummary();
            var json = JsonSerializer.Serialize(summary, _jsonOptions);

            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Handles the full stats endpoint request (includes history).
        /// </summary>
        public async Task HandleFullAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";

            var stats = _statsService.GetStats();
            var json = JsonSerializer.Serialize(stats, _jsonOptions);

            await context.Response.WriteAsync(json);
        }
    }
}
