using CpuGuard.NET.Configuration;
using CpuGuard.NET.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================
// CpuGuard.NET Configuration
// ============================================

// 1. Add CpuGuard with custom options
builder.Services.AddCpuGuard(options =>
{
    options.MaxCpuPercentage = 80.0;
    options.ResponseStatusCode = 503;
    options.ResponseMessage = "Server is under heavy CPU load. Please try again later.";
    options.ResponseContentType = "text/plain";

    // Exclude health and stats endpoints from CPU limiting
    options.ExcludedPaths.Add("/health");
    options.ExcludedPaths.Add("/cpuguard");

    // Subscribe to CPU limit exceeded events
    options.OnCpuLimitExceeded += (sender, args) =>
    {
        Console.WriteLine($"[CpuGuard] CPU limit exceeded! Usage: {args.CpuUsagePercentage:F1}% (Threshold: {args.Threshold}%)");
        Console.WriteLine($"[CpuGuard] Request: {args.HttpMethod} {args.RequestPath}");
    };
});

// 2. Add MemoryGuard with custom options
builder.Services.AddMemoryGuard(options =>
{
    options.MaxMemoryPercentage = 85.0;
    options.UsePercentage = true;
    options.ResponseStatusCode = 503;
    options.ResponseMessage = "Server memory usage is too high. Please try again later.";

    options.ExcludedPaths.Add("/health");
    options.ExcludedPaths.Add("/cpuguard");

    options.OnMemoryLimitExceeded += (sender, args) =>
    {
        Console.WriteLine($"[MemoryGuard] Memory limit exceeded! Usage: {args.MemoryUsagePercentage:F1}%");
    };
});

// 3. Add Gradual Throttling
builder.Services.AddGradualThrottling(options =>
{
    options.SoftLimitPercentage = 50.0;  // Start delaying at 50% CPU
    options.HardLimitPercentage = 85.0;  // Reject at 85% CPU
    options.MinDelay = TimeSpan.FromMilliseconds(100);
    options.MaxDelay = TimeSpan.FromSeconds(3);
    options.Mode = ThrottlingMode.Linear;

    options.ExcludedPaths.Add("/health");
    options.ExcludedPaths.Add("/cpuguard");
});

// 4. Add Rate Limiting with CPU awareness
builder.Services.AddCpuGuardRateLimiting(options =>
{
    options.RequestsPerWindow = 100;
    options.Window = TimeSpan.FromMinutes(1);
    options.Mode = RateLimitMode.SlidingWindow;
    options.CombineWithCpuLimit = true;  // Stricter limits when CPU is high
    options.CpuThresholdForStricterLimits = 70.0;
    options.HighCpuRateLimitFactor = 0.5;  // Reduce to 50% rate limit when CPU is high
    options.IncludeRateLimitHeaders = true;

    options.ExcludedPaths.Add("/health");
    options.ExcludedPaths.Add("/cpuguard");
});

// 5. Add Health Checks
builder.Services.AddHealthChecks()
    .AddCpuGuardCpuCheck(configure: opts =>
    {
        opts.DegradedThreshold = 60.0;
        opts.UnhealthyThreshold = 85.0;
    })
    .AddCpuGuardMemoryCheck(configure: opts =>
    {
        opts.DegradedThreshold = 70.0;
        opts.UnhealthyThreshold = 90.0;
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================
// CpuGuard.NET Middleware Pipeline
// ============================================

// Order matters! Rate limiting first, then throttling, then resource guards

// Map CpuGuard endpoints (stats, dashboard)
app.MapCpuGuardEndpoints("/cpuguard");

// Apply middleware
app.UseCpuGuardRateLimiting();   // Rate limit first
app.UseGradualThrottling();       // Then gradual throttling
app.UseCpuGuard();                // Then CPU guard
app.UseMemoryGuard();             // Then memory guard

// Health checks endpoint
app.MapHealthChecks("/health");

// ============================================
// Sample API Endpoints
// ============================================

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Normal endpoint
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// CPU-intensive endpoint (for testing)
app.MapGet("/cpu-intensive", () =>
{
    // Simulate CPU-intensive work
    var result = 0.0;
    for (int i = 0; i < 10_000_000; i++)
    {
        result += Math.Sqrt(i) * Math.Sin(i);
    }
    return new { Result = result, Message = "CPU-intensive operation completed" };
})
.WithName("CpuIntensive")
.WithOpenApi();

// Memory-intensive endpoint (for testing)
app.MapGet("/memory-intensive", () =>
{
    // Simulate memory-intensive work
    var data = new List<byte[]>();
    for (int i = 0; i < 10; i++)
    {
        data.Add(new byte[1024 * 1024]); // Allocate 1MB per iteration
    }
    return new { AllocatedMB = data.Count, Message = "Memory-intensive operation completed" };
})
.WithName("MemoryIntensive")
.WithOpenApi();

// Slow endpoint (for testing gradual throttling)
app.MapGet("/slow", async () =>
{
    await Task.Delay(2000); // Simulate slow operation
    return new { Message = "Slow operation completed" };
})
.WithName("SlowEndpoint")
.WithOpenApi();

Console.WriteLine("==============================================");
Console.WriteLine("CpuGuard.NET Sample Application");
Console.WriteLine("==============================================");
Console.WriteLine();
Console.WriteLine("Endpoints:");
Console.WriteLine("  - GET /weatherforecast     - Normal API endpoint");
Console.WriteLine("  - GET /cpu-intensive       - CPU-intensive endpoint (for testing)");
Console.WriteLine("  - GET /memory-intensive    - Memory-intensive endpoint (for testing)");
Console.WriteLine("  - GET /slow                - Slow endpoint (for testing)");
Console.WriteLine();
Console.WriteLine("CpuGuard Endpoints:");
Console.WriteLine("  - GET /cpuguard/stats      - JSON stats API");
Console.WriteLine("  - GET /cpuguard/stats/full - Full stats with history");
Console.WriteLine("  - GET /cpuguard/dashboard  - HTML dashboard");
Console.WriteLine("  - GET /health              - Health check endpoint");
Console.WriteLine();
Console.WriteLine("Swagger UI: http://localhost:5000/swagger");
Console.WriteLine("Dashboard:  http://localhost:5000/cpuguard/dashboard");
Console.WriteLine("==============================================");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
