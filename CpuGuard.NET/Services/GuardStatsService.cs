using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CpuGuard.NET.Services
{
    /// <summary>
    /// Service for collecting and exposing guard statistics.
    /// </summary>
    public class GuardStatsService
    {
        private readonly ResourceMonitorService _resourceMonitor;
        private readonly ConcurrentQueue<HistoricalDataPoint> _cpuHistory;
        private readonly ConcurrentQueue<HistoricalDataPoint> _memoryHistory;
        private readonly int _maxHistorySize;
        private readonly object _lock = new object();

        // Request tracking
        private long _totalRequests;
        private DateTime _startTime;

        /// <summary>
        /// Creates a new GuardStatsService.
        /// </summary>
        /// <param name="resourceMonitor">The resource monitor service.</param>
        /// <param name="maxHistorySize">Maximum number of historical data points to keep. Default is 60.</param>
        public GuardStatsService(ResourceMonitorService resourceMonitor, int maxHistorySize = 60)
        {
            _resourceMonitor = resourceMonitor;
            _maxHistorySize = maxHistorySize;
            _cpuHistory = new ConcurrentQueue<HistoricalDataPoint>();
            _memoryHistory = new ConcurrentQueue<HistoricalDataPoint>();
            _startTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Records a data point in history.
        /// </summary>
        public void RecordDataPoint()
        {
            var now = DateTime.UtcNow;

            // Add CPU history
            _cpuHistory.Enqueue(new HistoricalDataPoint
            {
                Timestamp = now,
                Value = _resourceMonitor.CurrentCpuUsage
            });

            // Add memory history
            _memoryHistory.Enqueue(new HistoricalDataPoint
            {
                Timestamp = now,
                Value = _resourceMonitor.CurrentMemoryUsage
            });

            // Trim history if needed
            while (_cpuHistory.Count > _maxHistorySize)
                _cpuHistory.TryDequeue(out _);
            while (_memoryHistory.Count > _maxHistorySize)
                _memoryHistory.TryDequeue(out _);
        }

        /// <summary>
        /// Increments the total request counter.
        /// </summary>
        public void IncrementTotalRequests()
        {
            lock (_lock) _totalRequests++;
        }

        /// <summary>
        /// Gets the total number of requests processed.
        /// </summary>
        public long TotalRequests
        {
            get { lock (_lock) return _totalRequests; }
        }

        /// <summary>
        /// Gets the uptime since the service started.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        /// <summary>
        /// Gets comprehensive statistics.
        /// </summary>
        public GuardStats GetStats()
        {
            var snapshot = _resourceMonitor.GetSnapshot();

            return new GuardStats
            {
                CurrentCpuUsage = snapshot.CpuUsagePercentage,
                CurrentMemoryUsage = snapshot.MemoryUsagePercentage,
                CurrentMemoryBytes = snapshot.MemoryUsageBytes,
                TotalMemoryBytes = snapshot.TotalMemoryBytes,
                AverageCpuUsage = snapshot.AverageCpuUsage,
                PeakCpuUsage = snapshot.PeakCpuUsage,
                AverageMemoryUsage = snapshot.AverageMemoryUsage,
                PeakMemoryUsage = snapshot.PeakMemoryUsage,
                TotalRequestsThrottled = snapshot.RequestsThrottled,
                TotalRequestsDelayed = snapshot.RequestsDelayed,
                TotalRequestsRateLimited = snapshot.RequestsRateLimited,
                TotalRequests = TotalRequests,
                Uptime = Uptime,
                LastUpdated = snapshot.Timestamp,
                CpuHistory = _cpuHistory.ToArray(),
                MemoryHistory = _memoryHistory.ToArray()
            };
        }

        /// <summary>
        /// Gets a summary of statistics (without history).
        /// </summary>
        public GuardStatsSummary GetSummary()
        {
            var snapshot = _resourceMonitor.GetSnapshot();

            return new GuardStatsSummary
            {
                CurrentCpuUsage = snapshot.CpuUsagePercentage,
                CurrentMemoryUsage = snapshot.MemoryUsagePercentage,
                CurrentMemoryBytes = snapshot.MemoryUsageBytes,
                TotalMemoryBytes = snapshot.TotalMemoryBytes,
                AverageCpuUsage = snapshot.AverageCpuUsage,
                PeakCpuUsage = snapshot.PeakCpuUsage,
                AverageMemoryUsage = snapshot.AverageMemoryUsage,
                PeakMemoryUsage = snapshot.PeakMemoryUsage,
                TotalRequestsThrottled = snapshot.RequestsThrottled,
                TotalRequestsDelayed = snapshot.RequestsDelayed,
                TotalRequestsRateLimited = snapshot.RequestsRateLimited,
                TotalRequests = TotalRequests,
                UptimeSeconds = Uptime.TotalSeconds,
                LastUpdated = snapshot.Timestamp
            };
        }
    }

    /// <summary>
    /// A historical data point.
    /// </summary>
    public class HistoricalDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// Comprehensive guard statistics including history.
    /// </summary>
    public class GuardStats
    {
        public double CurrentCpuUsage { get; set; }
        public double CurrentMemoryUsage { get; set; }
        public long CurrentMemoryBytes { get; set; }
        public long TotalMemoryBytes { get; set; }
        public double AverageCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double PeakMemoryUsage { get; set; }
        public long TotalRequestsThrottled { get; set; }
        public long TotalRequestsDelayed { get; set; }
        public long TotalRequestsRateLimited { get; set; }
        public long TotalRequests { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime LastUpdated { get; set; }
        public HistoricalDataPoint[] CpuHistory { get; set; } = Array.Empty<HistoricalDataPoint>();
        public HistoricalDataPoint[] MemoryHistory { get; set; } = Array.Empty<HistoricalDataPoint>();
    }

    /// <summary>
    /// Summary of guard statistics (without history, for JSON API).
    /// </summary>
    public class GuardStatsSummary
    {
        public double CurrentCpuUsage { get; set; }
        public double CurrentMemoryUsage { get; set; }
        public long CurrentMemoryBytes { get; set; }
        public long TotalMemoryBytes { get; set; }
        public double AverageCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double PeakMemoryUsage { get; set; }
        public long TotalRequestsThrottled { get; set; }
        public long TotalRequestsDelayed { get; set; }
        public long TotalRequestsRateLimited { get; set; }
        public long TotalRequests { get; set; }
        public double UptimeSeconds { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
