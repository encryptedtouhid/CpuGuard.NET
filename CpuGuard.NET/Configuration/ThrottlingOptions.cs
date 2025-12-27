using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CpuGuard.NET.Configuration
{
    /// <summary>
    /// Configuration options for gradual throttling middleware.
    /// </summary>
    public class ThrottlingOptions
    {
        /// <summary>
        /// CPU percentage at which gradual throttling begins (delays start). Default is 60%.
        /// </summary>
        public double SoftLimitPercentage { get; set; } = 60.0;

        /// <summary>
        /// CPU percentage at which requests are rejected (hard limit). Default is 90%.
        /// </summary>
        public double HardLimitPercentage { get; set; } = 90.0;

        /// <summary>
        /// Minimum delay when throttling begins. Default is 100ms.
        /// </summary>
        public TimeSpan MinDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Maximum delay before rejecting. Default is 5 seconds.
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Throttling mode. Default is Linear.
        /// </summary>
        public ThrottlingMode Mode { get; set; } = ThrottlingMode.Linear;

        /// <summary>
        /// HTTP status code returned when hard limit is exceeded. Default is 503.
        /// </summary>
        public int ResponseStatusCode { get; set; } = 503;

        /// <summary>
        /// Message returned when hard limit is exceeded.
        /// </summary>
        public string ResponseMessage { get; set; } = "Server is overloaded. Try again later.";

        /// <summary>
        /// Content type for the response. Default is text/plain.
        /// </summary>
        public string ResponseContentType { get; set; } = "text/plain";

        /// <summary>
        /// Custom response handler for hard limit exceeded.
        /// </summary>
        public Func<HttpContext, double, Task>? CustomResponseHandler { get; set; }

        /// <summary>
        /// Paths to exclude from throttling.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Custom predicate to determine if a request should be excluded.
        /// </summary>
        public Func<HttpContext, bool>? ExclusionPredicate { get; set; }

        /// <summary>
        /// Whether to include memory usage in throttling calculation. Default is false.
        /// </summary>
        public bool IncludeMemory { get; set; } = false;

        /// <summary>
        /// Weight of memory in combined calculation (0-1). Default is 0.3.
        /// </summary>
        public double MemoryWeight { get; set; } = 0.3;
    }

    /// <summary>
    /// Throttling delay calculation mode.
    /// </summary>
    public enum ThrottlingMode
    {
        /// <summary>
        /// Linear interpolation between soft and hard limits.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponential increase in delay as CPU approaches hard limit.
        /// </summary>
        Exponential
    }
}
