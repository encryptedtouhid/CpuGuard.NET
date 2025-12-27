using System;

namespace CpuGuard.NET.Events
{
    /// <summary>
    /// Event arguments for when rate limit is exceeded.
    /// </summary>
    public class RateLimitExceededEventArgs : GuardEventArgs
    {
        /// <summary>
        /// The client identifier (usually IP address).
        /// </summary>
        public string ClientIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// The number of requests made in the current window.
        /// </summary>
        public int RequestCount { get; set; }

        /// <summary>
        /// The configured rate limit.
        /// </summary>
        public int RateLimit { get; set; }

        /// <summary>
        /// Time until the rate limit resets.
        /// </summary>
        public TimeSpan ResetIn { get; set; }
    }
}
