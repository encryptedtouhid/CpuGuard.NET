using System;
using System.Collections.Generic;
using CpuGuard.NET.Events;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CpuGuard.NET.Configuration
{
    /// <summary>
    /// Configuration options for CPU limiting middleware.
    /// </summary>
    public class CpuGuardOptions
    {
        /// <summary>
        /// Maximum CPU usage percentage before throttling (0-100). Default is 80%.
        /// </summary>
        public double MaxCpuPercentage { get; set; } = 80.0;

        /// <summary>
        /// Interval for monitoring CPU usage. Default is 1 second.
        /// </summary>
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// HTTP status code returned when CPU limit is exceeded. Default is 503.
        /// </summary>
        public int ResponseStatusCode { get; set; } = 503;

        /// <summary>
        /// Message returned when CPU limit is exceeded.
        /// </summary>
        public string ResponseMessage { get; set; } = "CPU usage limit exceeded. Try again later.";

        /// <summary>
        /// Content type for the response. Default is text/plain.
        /// </summary>
        public string ResponseContentType { get; set; } = "text/plain";

        /// <summary>
        /// Custom response handler. When set, overrides default response behavior.
        /// Parameters: HttpContext, current CPU usage percentage.
        /// </summary>
        public Func<HttpContext, double, Task>? CustomResponseHandler { get; set; }

        /// <summary>
        /// Paths to exclude from CPU limiting (e.g., "/health", "/metrics").
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Custom predicate to determine if a request should be excluded.
        /// </summary>
        public Func<HttpContext, bool>? ExclusionPredicate { get; set; }

        /// <summary>
        /// Event raised when CPU limit is exceeded.
        /// </summary>
        public event EventHandler<CpuLimitExceededEventArgs>? OnCpuLimitExceeded;

        internal void RaiseCpuLimitExceeded(CpuLimitExceededEventArgs args)
        {
            OnCpuLimitExceeded?.Invoke(this, args);
        }
    }
}
