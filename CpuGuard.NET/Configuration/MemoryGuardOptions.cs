using System;
using System.Collections.Generic;
using CpuGuard.NET.Events;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CpuGuard.NET.Configuration
{
    /// <summary>
    /// Configuration options for Memory limiting middleware.
    /// </summary>
    public class MemoryGuardOptions
    {
        /// <summary>
        /// Maximum memory in bytes before throttling. Default is 500MB.
        /// </summary>
        public long MaxMemoryBytes { get; set; } = 500L * 1024 * 1024;

        /// <summary>
        /// Maximum memory usage percentage before throttling (0-100). Default is 80%.
        /// </summary>
        public double MaxMemoryPercentage { get; set; } = 80.0;

        /// <summary>
        /// If true, uses percentage-based limiting. If false, uses absolute byte limit.
        /// </summary>
        public bool UsePercentage { get; set; } = true;

        /// <summary>
        /// HTTP status code returned when memory limit is exceeded. Default is 503.
        /// </summary>
        public int ResponseStatusCode { get; set; } = 503;

        /// <summary>
        /// Message returned when memory limit is exceeded.
        /// </summary>
        public string ResponseMessage { get; set; } = "Memory usage limit exceeded. Try again later.";

        /// <summary>
        /// Content type for the response. Default is text/plain.
        /// </summary>
        public string ResponseContentType { get; set; } = "text/plain";

        /// <summary>
        /// Custom response handler. When set, overrides default response behavior.
        /// Parameters: HttpContext, current memory usage (bytes or percentage based on UsePercentage).
        /// </summary>
        public Func<HttpContext, double, Task>? CustomResponseHandler { get; set; }

        /// <summary>
        /// Paths to exclude from memory limiting.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Custom predicate to determine if a request should be excluded.
        /// </summary>
        public Func<HttpContext, bool>? ExclusionPredicate { get; set; }

        /// <summary>
        /// Event raised when memory limit is exceeded.
        /// </summary>
        public event EventHandler<MemoryLimitExceededEventArgs>? OnMemoryLimitExceeded;

        internal void RaiseMemoryLimitExceeded(MemoryLimitExceededEventArgs args)
        {
            OnMemoryLimitExceeded?.Invoke(this, args);
        }
    }
}
