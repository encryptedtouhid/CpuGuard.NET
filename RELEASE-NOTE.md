# Release Notes for CpuGuard.NET

## Version 2.0.0

### Major Release - Comprehensive Resource Management

This release transforms CpuGuard.NET into a full-featured resource management middleware with 10 new capabilities.

#### New Features

1. **Memory Limiting (MemoryGuard)**
   - Monitor and limit memory usage with percentage or absolute byte thresholds
   - Configurable response handling when limits are exceeded

2. **Custom Response Handlers**
   - Define custom responses for throttled requests
   - Support for JSON, HTML, or any content type

3. **Event Callbacks**
   - Subscribe to `OnCpuLimitExceeded` and `OnMemoryLimitExceeded` events
   - Integrate with logging, alerting, and monitoring systems

4. **Health Check Integration**
   - ASP.NET Core health check support with `AddCpuGuardHealthChecks()`
   - Configurable degraded and unhealthy thresholds

5. **OpenTelemetry Metrics**
   - Built-in metrics: `cpuguard_requests_throttled_total`, `cpuguard_cpu_usage_percent`, etc.
   - Export to Prometheus, Grafana, and other observability platforms

6. **Gradual Throttling**
   - Progressive request delays as resource usage increases
   - Soft limit (start delaying) and hard limit (reject) thresholds
   - Linear or exponential delay modes

7. **Path Exclusions**
   - Exclude specific paths from all guards (e.g., `/health`, `/metrics`)
   - Support for custom exclusion predicates

8. **Rate Limiting**
   - Per-client rate limiting with Fixed Window, Sliding Window, or Token Bucket algorithms
   - CPU-aware rate limiting that reduces limits under high load
   - Standard `X-RateLimit-*` response headers

9. **Real-time Dashboard**
   - Built-in HTML dashboard with live charts at `/cpuguard/dashboard`
   - JSON stats API at `/cpuguard/stats` and `/cpuguard/stats/full`
   - CPU/memory gauges, request statistics, and system info

10. **Background Resource Monitoring**
    - Async CPU sampling via `ResourceMonitorService`
    - Accurate resource tracking without blocking requests

#### Breaking Changes

- Middleware now requires service registration via `AddCpuGuard()`, `AddMemoryGuard()`, etc.
- Legacy API (`UseCpuLimitMiddleware`) still supported but deprecated

#### New Dependencies

- `Microsoft.Extensions.Hosting.Abstractions` (6.0.0)
- `Microsoft.Extensions.Diagnostics.HealthChecks` (6.0.0)
- `OpenTelemetry.Api` (1.7.0)
- `System.Text.Json` (8.0.5)

---

## Version 1.0.1
#### Whole Application CPU Limit:

    1. Introduced a middleware component to limit CPU usage across the entire ASP.NET Core application.
    2. Configuration options:
        1. cpuLimitPercentage: Sets the maximum allowed CPU usage as a percentage.
        2. monitoringInterval: Defines the interval at which CPU usage is monitored.
    3. When the CPU usage exceeds the specified limit, the middleware responds with HTTP 503 Service Unavailable, indicating that the server is overloaded.



##  Version 1.0.2
#### Limit CPU Process for a Specific Method:

    1. Added middleware to limit CPU usage for specific requests.
    2. Configuration options:
            1. cpuLimitPercentage: Sets the maximum allowed CPU usage for the specific  request.
            2. monitoringInterval: Defines the interval at which CPU usage is monitored for the request.
    3. When the CPU usage for a request exceeds the specified limit, the middleware responds with HTTP 503 Service Unavailable, indicating that the server is temporarily overloaded for that request.
