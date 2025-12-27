using System;

namespace CpuGuard.NET.Events
{
    /// <summary>
    /// Event arguments for when CPU limit is exceeded.
    /// </summary>
    public class CpuLimitExceededEventArgs : GuardEventArgs
    {
        /// <summary>
        /// The current CPU usage percentage.
        /// </summary>
        public double CpuUsagePercentage { get; set; }

        /// <summary>
        /// The configured CPU threshold that was exceeded.
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// The amount by which the CPU usage exceeded the threshold.
        /// </summary>
        public double ExceededBy => CpuUsagePercentage - Threshold;
    }
}
