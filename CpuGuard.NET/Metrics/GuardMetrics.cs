using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace CpuGuard.NET.Metrics
{
    /// <summary>
    /// OpenTelemetry metrics for CpuGuard.NET.
    /// </summary>
    public static class GuardMetrics
    {
        /// <summary>
        /// The meter for CpuGuard.NET metrics.
        /// </summary>
        public static readonly Meter Meter = new Meter("CpuGuard.NET", "1.0.0");

        private static readonly Counter<long> _requestsThrottled;
        private static readonly Counter<long> _requestsDelayed;
        private static readonly Counter<long> _requestsRateLimited;
        private static readonly Histogram<double> _cpuUsage;
        private static readonly Histogram<double> _memoryUsage;
        private static readonly Histogram<double> _delayApplied;
        private static readonly Histogram<double> _requestDuration;

        private static IMetricsCollector? _customCollector;

        static GuardMetrics()
        {
            _requestsThrottled = Meter.CreateCounter<long>(
                "cpuguard_requests_throttled_total",
                "requests",
                "Total number of requests throttled due to resource limits");

            _requestsDelayed = Meter.CreateCounter<long>(
                "cpuguard_requests_delayed_total",
                "requests",
                "Total number of requests delayed due to gradual throttling");

            _requestsRateLimited = Meter.CreateCounter<long>(
                "cpuguard_requests_ratelimited_total",
                "requests",
                "Total number of requests rejected due to rate limiting");

            _cpuUsage = Meter.CreateHistogram<double>(
                "cpuguard_cpu_usage_percent",
                "percent",
                "CPU usage percentage");

            _memoryUsage = Meter.CreateHistogram<double>(
                "cpuguard_memory_usage_percent",
                "percent",
                "Memory usage percentage");

            _delayApplied = Meter.CreateHistogram<double>(
                "cpuguard_delay_applied_milliseconds",
                "ms",
                "Delay applied to requests in milliseconds");

            _requestDuration = Meter.CreateHistogram<double>(
                "cpuguard_request_duration_milliseconds",
                "ms",
                "Request processing duration in milliseconds");
        }

        /// <summary>
        /// Sets a custom metrics collector. When set, metrics are also sent to this collector.
        /// </summary>
        public static void SetCustomCollector(IMetricsCollector? collector)
        {
            _customCollector = collector;
        }

        /// <summary>
        /// Records CPU usage percentage.
        /// </summary>
        public static void RecordCpuUsage(double percentage)
        {
            _cpuUsage.Record(percentage);
            _customCollector?.RecordCpuUsage(percentage);
        }

        /// <summary>
        /// Records memory usage percentage.
        /// </summary>
        public static void RecordMemoryUsage(double percentage)
        {
            _memoryUsage.Record(percentage);
            _customCollector?.RecordMemoryUsage(percentage);
        }

        /// <summary>
        /// Increments the throttled requests counter.
        /// </summary>
        public static void IncrementRequestsThrottled(string reason = "unknown")
        {
            _requestsThrottled.Add(1, new KeyValuePair<string, object?>("reason", reason));
            _customCollector?.IncrementRequestsThrottled(reason);
        }

        /// <summary>
        /// Increments the delayed requests counter.
        /// </summary>
        public static void IncrementRequestsDelayed()
        {
            _requestsDelayed.Add(1);
            _customCollector?.IncrementRequestsDelayed();
        }

        /// <summary>
        /// Increments the rate limited requests counter.
        /// </summary>
        public static void IncrementRequestsRateLimited()
        {
            _requestsRateLimited.Add(1);
            _customCollector?.IncrementRequestsRateLimited();
        }

        /// <summary>
        /// Records delay applied to a request.
        /// </summary>
        public static void RecordDelayApplied(double delayMs)
        {
            _delayApplied.Record(delayMs);
            _customCollector?.RecordDelayApplied(delayMs);
        }

        /// <summary>
        /// Records request duration.
        /// </summary>
        public static void RecordRequestDuration(double durationMs)
        {
            _requestDuration.Record(durationMs);
            _customCollector?.RecordRequestDuration(durationMs);
        }
    }
}
