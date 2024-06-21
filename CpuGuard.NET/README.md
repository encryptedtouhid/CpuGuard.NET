# CpuGuard.NET

A middleware to limit CPU usage for ASP.NET Core applications. Effortless CPU Usage Control for ASP.NET Core Applications.

## Installation

Install the NuGet package:

    dotnet add package CpuGuard.NET

## Usage
### Whole Application CPU Limit Example:
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
    
            // Use the whole application CPU limit middleware with 20% CPU limit and 1 second monitoring interval
            app.UseCpuLimitMiddleware(20.0, TimeSpan.FromMilliseconds(1000));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

### Specific Request CPU Limit Example:

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
    
            // Use the specific request CPU limit middleware with 15% CPU limit and 1 second monitoring interval
            app.UseCpuLimitRequestMiddleware(15.0, TimeSpan.FromMilliseconds(1000));

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

## Resources
- [Homepage](https://github.com/encryptedtouhid/CpuGuard.NET)
- [NuGet Package](https://www.nuget.org/packages/CpuGuard.NET)
