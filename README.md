# CpuGuard.NET

A middleware to limit CPU usage for ASP.NET Core applications. Effortless CPU Usage Control for ASP.NET Core Applications.

## Installation

Install the NuGet package:

    dotnet add package AspNetCore.CpuLimiter

## Usage

    var builder = WebApplication.CreateBuilder(args);
    var app = builder.Build();

    app.UseCpuLimitMiddleware(20.0, TimeSpan.FromMilliseconds(1000)); // Limit CPU usage to 20%

    app.MapControllers();
    app.Run();

## Resources
- [Homepage]("")
- [Documentation]("")
- [NuGet Package]("https://www.nuget.org/packages/CpuGuard.NET")
- [Release Notes]("")
- [Contributing Guidelines](CONTRIBUTING.md)
- [License](LICENSE.md)
