using System;

namespace CpuGuard.NET.Metrics
{
    /// <summary>
    /// Interface for custom metrics collection implementations.
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Records CPU usage.
        /// </summary>
        /// <param name="percentage">CPU usage percentage (0-100).</param>
        void RecordCpuUsage(double percentage);

        /// <summary>
        /// Records memory usage.
        /// </summary>
        /// <param name="percentage">Memory usage percentage (0-100).</param>
        void RecordMemoryUsage(double percentage);

        /// <summary>
        /// Increments the throttled requests counter.
        /// </summary>
        /// <param name="reason">Reason for throttling (e.g., "cpu", "memory", "hard_limit").</param>
        void IncrementRequestsThrottled(string reason);

        /// <summary>
        /// Increments the delayed requests counter.
        /// </summary>
        void IncrementRequestsDelayed();

        /// <summary>
        /// Increments the rate limited requests counter.
        /// </summary>
        void IncrementRequestsRateLimited();

        /// <summary>
        /// Records delay applied to a request.
        /// </summary>
        /// <param name="delayMs">Delay in milliseconds.</param>
        void RecordDelayApplied(double delayMs);

        /// <summary>
        /// Records request processing time.
        /// </summary>
        /// <param name="durationMs">Duration in milliseconds.</param>
        void RecordRequestDuration(double durationMs);
    }
}
