# Release Notes for CpuGuard.NET 
##  Version 1.0.1
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
