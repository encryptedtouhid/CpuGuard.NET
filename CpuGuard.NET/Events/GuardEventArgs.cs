using System;
using Microsoft.AspNetCore.Http;

namespace CpuGuard.NET.Events
{
    /// <summary>
    /// Base class for guard event arguments.
    /// </summary>
    public abstract class GuardEventArgs : EventArgs
    {
        /// <summary>
        /// The HTTP context of the request that triggered the event.
        /// </summary>
        public HttpContext Context { get; set; } = null!;

        /// <summary>
        /// The timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The request path that triggered the event.
        /// </summary>
        public string RequestPath { get; set; } = string.Empty;

        /// <summary>
        /// The HTTP method of the request.
        /// </summary>
        public string HttpMethod { get; set; } = string.Empty;
    }
}
