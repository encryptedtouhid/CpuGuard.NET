# CpuGuard.NET

Comprehensive resource management middleware for ASP.NET Core applications. Protect your application from overload with CPU limiting, memory limiting, gradual throttling, rate limiting, health checks, OpenTelemetry metrics, and a real-time dashboard.

## Features

- **CPU Limiting** - Monitor and limit CPU usage across your application
- **Memory Limiting** - Monitor and limit memory usage
- **Gradual Throttling** - Progressive request delays as resources increase
- **Rate Limiting** - Per-client request rate limiting with CPU-aware adjustments
- **Health Checks** - ASP.NET Core health check integration
- **OpenTelemetry Metrics** - Export metrics to Prometheus, Grafana, etc.
- **Real-time Dashboard** - Built-in HTML dashboard with live charts
- **Custom Response Handlers** - Customize throttled responses
- **Event Callbacks** - Subscribe to limit exceeded events
- **Path Exclusions** - Exclude specific endpoints from limiting

## Installation

```bash
dotnet add package CpuGuard.NET
```

## Quick Start

```csharp
using CpuGuard.NET.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add CpuGuard services
builder.Services.AddCpuGuard();

var app = builder.Build();

// Use CpuGuard middleware
app.UseCpuGuard();

// Map dashboard and stats endpoints
app.MapCpuGuardEndpoints("/cpuguard");

app.Run();
```

Visit `/cpuguard/dashboard` to see the real-time monitoring dashboard.

## Configuration

### CPU Guard

```csharp
builder.Services.AddCpuGuard(options =>
{
    options.MaxCpuPercentage = 80.0;
    options.ResponseStatusCode = 503;
    options.ResponseMessage = "Server is under heavy load.";

    // Exclude paths
    options.ExcludedPaths.Add("/health");
    options.ExcludedPaths.Add("/cpuguard");

    // Subscribe to events
    options.OnCpuLimitExceeded += (sender, args) =>
    {
        Console.WriteLine($"CPU limit exceeded: {args.CpuUsagePercentage}%");
    };

    // Custom response handler
    options.CustomResponseHandler = async (context, cpuUsage) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync($"{{\"error\":\"CPU overload\",\"usage\":{cpuUsage}}}");
    };
});
```

### Memory Guard

```csharp
builder.Services.AddMemoryGuard(options =>
{
    options.MaxMemoryPercentage = 85.0;
    options.UsePercentage = true;  // or use MaxMemoryBytes for absolute limit

    options.OnMemoryLimitExceeded += (sender, args) =>
    {
        Console.WriteLine($"Memory limit exceeded: {args.MemoryUsagePercentage}%");
    };
});

app.UseMemoryGuard();
```

### Gradual Throttling

Instead of hard cutoffs, gradually delay requests as resources increase:

```csharp
builder.Services.AddGradualThrottling(options =>
{
    options.SoftLimitPercentage = 60.0;  // Start delaying
    options.HardLimitPercentage = 90.0;  // Reject requests
    options.MinDelay = TimeSpan.FromMilliseconds(100);
    options.MaxDelay = TimeSpan.FromSeconds(5);
    options.Mode = ThrottlingMode.Linear;  // or Exponential
});

app.UseGradualThrottling();
```

### Rate Limiting

```csharp
builder.Services.AddCpuGuardRateLimiting(options =>
{
    options.RequestsPerWindow = 100;
    options.Window = TimeSpan.FromMinutes(1);
    options.Mode = RateLimitMode.SlidingWindow;  // or FixedWindow, TokenBucket

    // CPU-aware rate limiting
    options.CombineWithCpuLimit = true;
    options.CpuThresholdForStricterLimits = 70.0;
    options.HighCpuRateLimitFactor = 0.5;  // Reduce limit by 50% when CPU is high

    options.IncludeRateLimitHeaders = true;  // Add X-RateLimit-* headers
});

app.UseCpuGuardRateLimiting();
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCpuGuardCpuCheck(configure: opts =>
    {
        opts.DegradedThreshold = 70.0;
        opts.UnhealthyThreshold = 90.0;
    })
    .AddCpuGuardMemoryCheck(configure: opts =>
    {
        opts.DegradedThreshold = 70.0;
        opts.UnhealthyThreshold = 90.0;
    });

app.MapHealthChecks("/health");
```

### Dashboard & Stats Endpoints

```csharp
// Map all endpoints at once
app.MapCpuGuardEndpoints("/cpuguard");

// Or map individually
app.MapCpuGuardStats("/cpuguard/stats");
app.MapCpuGuardFullStats("/cpuguard/stats/full");
app.MapCpuGuardDashboard("/cpuguard/dashboard");
```

**Endpoints:**
- `GET /cpuguard/stats` - JSON stats summary
- `GET /cpuguard/stats/full` - Full stats with history
- `GET /cpuguard/dashboard` - Real-time HTML dashboard

## Full Example

```csharp
using CpuGuard.NET.Configuration;
using CpuGuard.NET.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add all CpuGuard services
builder.Services.AddCpuGuard(options =>
{
    options.MaxCpuPercentage = 80.0;
    options.ExcludedPaths.Add("/health");
});

builder.Services.AddMemoryGuard(options =>
{
    options.MaxMemoryPercentage = 85.0;
});

builder.Services.AddGradualThrottling(options =>
{
    options.SoftLimitPercentage = 60.0;
    options.HardLimitPercentage = 90.0;
});

builder.Services.AddCpuGuardRateLimiting(options =>
{
    options.RequestsPerWindow = 100;
    options.Window = TimeSpan.FromMinutes(1);
});

builder.Services.AddHealthChecks()
    .AddCpuGuardHealthChecks();

var app = builder.Build();

// Map endpoints
app.MapCpuGuardEndpoints("/cpuguard");
app.MapHealthChecks("/health");

// Apply middleware (order matters)
app.UseCpuGuardRateLimiting();
app.UseGradualThrottling();
app.UseCpuGuard();
app.UseMemoryGuard();

app.MapGet("/", () => "Hello World!");

app.Run();
```

## OpenTelemetry Metrics

CpuGuard.NET exports the following metrics:

| Metric | Type | Description |
|--------|------|-------------|
| `cpuguard_requests_throttled_total` | Counter | Requests throttled due to limits |
| `cpuguard_requests_delayed_total` | Counter | Requests delayed by throttling |
| `cpuguard_requests_ratelimited_total` | Counter | Requests rejected by rate limiting |
| `cpuguard_cpu_usage_percent` | Histogram | CPU usage percentage |
| `cpuguard_memory_usage_percent` | Histogram | Memory usage percentage |
| `cpuguard_delay_applied_milliseconds` | Histogram | Delay applied to requests |

## Middleware Order

For best results, apply middleware in this order:

```csharp
app.UseCpuGuardRateLimiting();   // 1. Rate limit first
app.UseGradualThrottling();       // 2. Then gradual throttling
app.UseCpuGuard();                // 3. Then CPU guard
app.UseMemoryGuard();             // 4. Then memory guard
```

## Backward Compatibility

The legacy API is still supported:

```csharp
// Legacy usage (still works)
app.UseCpuLimitMiddleware(20.0, TimeSpan.FromMilliseconds(1000));
app.UseCpuLimitRequestMiddleware(15.0, TimeSpan.FromMilliseconds(1000));
```

## Resources

- [Homepage](https://github.com/encryptedtouhid/CpuGuard.NET)
- [NuGet Package](https://www.nuget.org/packages/CpuGuard.NET)
- [Sample Project](https://github.com/encryptedtouhid/CpuGuard.NET/tree/main/samples/CpuGuard.Sample)

## License

MIT License
