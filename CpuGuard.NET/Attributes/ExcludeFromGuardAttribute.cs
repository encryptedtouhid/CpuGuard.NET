using System;

namespace CpuGuard.NET.Attributes
{
    /// <summary>
    /// Excludes a controller or action from CpuGuard middleware processing.
    /// When applied, the decorated endpoint bypasses CPU, memory, throttling, and rate limit checks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ExcludeFromGuardAttribute : Attribute
    {
        /// <summary>
        /// If true, only excludes from CPU limit checks. Default is false (excludes from all).
        /// </summary>
        public bool CpuOnly { get; set; } = false;

        /// <summary>
        /// If true, only excludes from memory limit checks. Default is false (excludes from all).
        /// </summary>
        public bool MemoryOnly { get; set; } = false;

        /// <summary>
        /// If true, only excludes from rate limiting. Default is false (excludes from all).
        /// </summary>
        public bool RateLimitOnly { get; set; } = false;

        /// <summary>
        /// If true, only excludes from throttling. Default is false (excludes from all).
        /// </summary>
        public bool ThrottlingOnly { get; set; } = false;

        /// <summary>
        /// Creates a new ExcludeFromGuardAttribute that excludes from all guard checks.
        /// </summary>
        public ExcludeFromGuardAttribute()
        {
        }
    }
}
