using System;

namespace CpuGuard.NET.Events
{
    /// <summary>
    /// Event arguments for throttling events.
    /// </summary>
    public class ThrottlingEventArgs : GuardEventArgs
    {
        /// <summary>
        /// The current CPU usage percentage.
        /// </summary>
        public double CpuUsagePercentage { get; set; }

        /// <summary>
        /// The delay applied to the request.
        /// </summary>
        public TimeSpan DelayApplied { get; set; }

        /// <summary>
        /// Whether the request was rejected (true) or just delayed (false).
        /// </summary>
        public bool WasRejected { get; set; }

        /// <summary>
        /// The soft limit threshold.
        /// </summary>
        public double SoftLimit { get; set; }

        /// <summary>
        /// The hard limit threshold.
        /// </summary>
        public double HardLimit { get; set; }
    }
}
