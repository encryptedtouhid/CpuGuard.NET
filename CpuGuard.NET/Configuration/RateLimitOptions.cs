using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CpuGuard.NET.Configuration
{
    /// <summary>
    /// Configuration options for rate limiting middleware.
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// Maximum number of requests allowed per window. Default is 100.
        /// </summary>
        public int RequestsPerWindow { get; set; } = 100;

        /// <summary>
        /// Time window for rate limiting. Default is 1 minute.
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Rate limiting mode. Default is SlidingWindow.
        /// </summary>
        public RateLimitMode Mode { get; set; } = RateLimitMode.SlidingWindow;

        /// <summary>
        /// If true, rate limiting is stricter when CPU is under pressure. Default is true.
        /// </summary>
        public bool CombineWithCpuLimit { get; set; } = true;

        /// <summary>
        /// CPU threshold at which rate limits become stricter. Default is 70%.
        /// </summary>
        public double CpuThresholdForStricterLimits { get; set; } = 70.0;

        /// <summary>
        /// Factor by which to reduce rate limit when CPU is high (0-1). Default is 0.5.
        /// </summary>
        public double HighCpuRateLimitFactor { get; set; } = 0.5;

        /// <summary>
        /// Function to extract client identifier from request. Default uses IP address.
        /// </summary>
        public Func<HttpContext, string>? ClientIdentifierFactory { get; set; }

        /// <summary>
        /// HTTP status code returned when rate limit is exceeded. Default is 429.
        /// </summary>
        public int ResponseStatusCode { get; set; } = 429;

        /// <summary>
        /// Message returned when rate limit is exceeded.
        /// </summary>
        public string ResponseMessage { get; set; } = "Rate limit exceeded. Try again later.";

        /// <summary>
        /// Content type for the response. Default is text/plain.
        /// </summary>
        public string ResponseContentType { get; set; } = "text/plain";

        /// <summary>
        /// Custom response handler for rate limit exceeded.
        /// Parameters: HttpContext, requests remaining, reset time.
        /// </summary>
        public Func<HttpContext, int, TimeSpan, Task>? CustomResponseHandler { get; set; }

        /// <summary>
        /// Paths to exclude from rate limiting.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Custom predicate to determine if a request should be excluded.
        /// </summary>
        public Func<HttpContext, bool>? ExclusionPredicate { get; set; }

        /// <summary>
        /// Whether to include rate limit headers in response. Default is true.
        /// </summary>
        public bool IncludeRateLimitHeaders { get; set; } = true;

        /// <summary>
        /// For TokenBucket mode: tokens added per second. Default is calculated from RequestsPerWindow/Window.
        /// </summary>
        public double? TokensPerSecond { get; set; }

        /// <summary>
        /// For TokenBucket mode: maximum bucket size. Default is RequestsPerWindow.
        /// </summary>
        public int? BucketSize { get; set; }
    }

    /// <summary>
    /// Rate limiting algorithm mode.
    /// </summary>
    public enum RateLimitMode
    {
        /// <summary>
        /// Fixed time window. Counter resets at window boundaries.
        /// </summary>
        FixedWindow,

        /// <summary>
        /// Sliding time window. Smooths out request distribution.
        /// </summary>
        SlidingWindow,

        /// <summary>
        /// Token bucket algorithm. Allows bursts while maintaining average rate.
        /// </summary>
        TokenBucket
    }
}
