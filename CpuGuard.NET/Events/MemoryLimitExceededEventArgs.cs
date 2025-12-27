using System;

namespace CpuGuard.NET.Events
{
    /// <summary>
    /// Event arguments for when memory limit is exceeded.
    /// </summary>
    public class MemoryLimitExceededEventArgs : GuardEventArgs
    {
        /// <summary>
        /// The current memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// The current memory usage as a percentage.
        /// </summary>
        public double MemoryUsagePercentage { get; set; }

        /// <summary>
        /// The configured memory threshold (bytes or percentage based on configuration).
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// Whether the threshold is a percentage (true) or absolute bytes (false).
        /// </summary>
        public bool IsPercentageThreshold { get; set; }
    }
}
