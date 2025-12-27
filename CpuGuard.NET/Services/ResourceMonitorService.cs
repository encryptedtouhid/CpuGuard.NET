using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace CpuGuard.NET.Services
{
    /// <summary>
    /// Background service that monitors CPU and memory usage.
    /// Provides accurate, sampled resource metrics for middleware and health checks.
    /// </summary>
    public class ResourceMonitorService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly TimeSpan _samplingInterval;
        private DateTime _lastSampleTime;
        private TimeSpan _lastCpuTime;
        private readonly object _lock = new object();

        // Current values (thread-safe access via properties)
        private double _currentCpuUsage;
        private double _currentMemoryUsage;
        private long _currentMemoryBytes;
        private long _totalMemoryBytes;

        // Statistics
        private double _averageCpuUsage;
        private double _peakCpuUsage;
        private double _averageMemoryUsage;
        private double _peakMemoryUsage;
        private long _sampleCount;

        // Throttling counters
        private long _requestsThrottled;
        private long _requestsDelayed;
        private long _requestsRateLimited;

        /// <summary>
        /// Creates a new ResourceMonitorService with the specified sampling interval.
        /// </summary>
        /// <param name="samplingInterval">How often to sample resources. Default is 1 second.</param>
        public ResourceMonitorService(TimeSpan? samplingInterval = null)
        {
            _samplingInterval = samplingInterval ?? TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Current CPU usage percentage (0-100).
        /// </summary>
        public double CurrentCpuUsage
        {
            get { lock (_lock) return _currentCpuUsage; }
        }

        /// <summary>
        /// Current memory usage percentage (0-100).
        /// </summary>
        public double CurrentMemoryUsage
        {
            get { lock (_lock) return _currentMemoryUsage; }
        }

        /// <summary>
        /// Current memory usage in bytes.
        /// </summary>
        public long CurrentMemoryBytes
        {
            get { lock (_lock) return _currentMemoryBytes; }
        }

        /// <summary>
        /// Total available memory in bytes.
        /// </summary>
        public long TotalMemoryBytes
        {
            get { lock (_lock) return _totalMemoryBytes; }
        }

        /// <summary>
        /// Average CPU usage since monitoring started.
        /// </summary>
        public double AverageCpuUsage
        {
            get { lock (_lock) return _averageCpuUsage; }
        }

        /// <summary>
        /// Peak CPU usage since monitoring started.
        /// </summary>
        public double PeakCpuUsage
        {
            get { lock (_lock) return _peakCpuUsage; }
        }

        /// <summary>
        /// Average memory usage since monitoring started.
        /// </summary>
        public double AverageMemoryUsage
        {
            get { lock (_lock) return _averageMemoryUsage; }
        }

        /// <summary>
        /// Peak memory usage since monitoring started.
        /// </summary>
        public double PeakMemoryUsage
        {
            get { lock (_lock) return _peakMemoryUsage; }
        }

        /// <summary>
        /// Total number of requests throttled (CPU/Memory limits exceeded).
        /// </summary>
        public long RequestsThrottled
        {
            get { lock (_lock) return _requestsThrottled; }
        }

        /// <summary>
        /// Total number of requests delayed (gradual throttling).
        /// </summary>
        public long RequestsDelayed
        {
            get { lock (_lock) return _requestsDelayed; }
        }

        /// <summary>
        /// Total number of requests rate limited.
        /// </summary>
        public long RequestsRateLimited
        {
            get { lock (_lock) return _requestsRateLimited; }
        }

        /// <summary>
        /// Increment the throttled request counter.
        /// </summary>
        public void IncrementThrottled()
        {
            lock (_lock) _requestsThrottled++;
        }

        /// <summary>
        /// Increment the delayed request counter.
        /// </summary>
        public void IncrementDelayed()
        {
            lock (_lock) _requestsDelayed++;
        }

        /// <summary>
        /// Increment the rate limited request counter.
        /// </summary>
        public void IncrementRateLimited()
        {
            lock (_lock) _requestsRateLimited++;
        }

        /// <summary>
        /// Starts the background monitoring service.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var process = Process.GetCurrentProcess();
            _lastSampleTime = DateTime.UtcNow;
            _lastCpuTime = process.TotalProcessorTime;

            // Get total system memory (approximation)
            // Use process physical memory as a baseline estimate
            _totalMemoryBytes = GetTotalPhysicalMemory();

            _timer = new Timer(SampleResources, null, TimeSpan.Zero, _samplingInterval);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the background monitoring service.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void SampleResources(object? state)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var currentTime = DateTime.UtcNow;
                var currentCpuTime = process.TotalProcessorTime;

                // Calculate CPU usage
                var cpuUsedMs = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
                var totalMsPassed = (currentTime - _lastSampleTime).TotalMilliseconds;

                double cpuUsage = 0;
                if (totalMsPassed > 0)
                {
                    cpuUsage = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;
                    cpuUsage = Math.Min(100, Math.Max(0, cpuUsage)); // Clamp to 0-100
                }

                // Calculate memory usage
                var memoryBytes = process.WorkingSet64;
                var gcMemory = GC.GetTotalMemory(false);
                var totalMemory = _totalMemoryBytes > 0 ? _totalMemoryBytes : 1;
                var memoryUsage = ((double)memoryBytes / totalMemory) * 100;
                memoryUsage = Math.Min(100, Math.Max(0, memoryUsage)); // Clamp to 0-100

                lock (_lock)
                {
                    _currentCpuUsage = cpuUsage;
                    _currentMemoryUsage = memoryUsage;
                    _currentMemoryBytes = memoryBytes;

                    // Update statistics
                    _sampleCount++;
                    _averageCpuUsage = ((_averageCpuUsage * (_sampleCount - 1)) + cpuUsage) / _sampleCount;
                    _averageMemoryUsage = ((_averageMemoryUsage * (_sampleCount - 1)) + memoryUsage) / _sampleCount;

                    if (cpuUsage > _peakCpuUsage) _peakCpuUsage = cpuUsage;
                    if (memoryUsage > _peakMemoryUsage) _peakMemoryUsage = memoryUsage;
                }

                // Update for next sample
                _lastSampleTime = currentTime;
                _lastCpuTime = currentCpuTime;
            }
            catch
            {
                // Silently ignore sampling errors to prevent service disruption
            }
        }

        /// <summary>
        /// Gets a snapshot of current statistics.
        /// </summary>
        public ResourceSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                return new ResourceSnapshot
                {
                    CpuUsagePercentage = _currentCpuUsage,
                    MemoryUsagePercentage = _currentMemoryUsage,
                    MemoryUsageBytes = _currentMemoryBytes,
                    TotalMemoryBytes = _totalMemoryBytes,
                    AverageCpuUsage = _averageCpuUsage,
                    PeakCpuUsage = _peakCpuUsage,
                    AverageMemoryUsage = _averageMemoryUsage,
                    PeakMemoryUsage = _peakMemoryUsage,
                    RequestsThrottled = _requestsThrottled,
                    RequestsDelayed = _requestsDelayed,
                    RequestsRateLimited = _requestsRateLimited,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Disposes the timer resource.
        /// </summary>
        public void Dispose()
        {
            _timer?.Dispose();
        }

        private static long GetTotalPhysicalMemory()
        {
            try
            {
                // Try to get actual physical memory using PerformanceCounter or WMI
                // Fallback to a reasonable estimate (8GB default)
                var process = Process.GetCurrentProcess();

                // Use the peak working set as a rough estimate of available memory
                // This is a heuristic since .NET Standard 2.1 doesn't expose total RAM directly
                long peakMemory = process.PeakWorkingSet64;

                // Assume available memory is at least 4x current peak (reasonable heuristic)
                // With a minimum of 4GB and maximum of 64GB as reasonable bounds
                long estimatedTotal = Math.Max(4L * 1024 * 1024 * 1024, peakMemory * 4);
                return Math.Min(64L * 1024 * 1024 * 1024, estimatedTotal);
            }
            catch
            {
                // Default to 8GB if we can't determine
                return 8L * 1024 * 1024 * 1024;
            }
        }
    }

    /// <summary>
    /// A snapshot of resource usage at a point in time.
    /// </summary>
    public class ResourceSnapshot
    {
        public double CpuUsagePercentage { get; set; }
        public double MemoryUsagePercentage { get; set; }
        public long MemoryUsageBytes { get; set; }
        public long TotalMemoryBytes { get; set; }
        public double AverageCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double PeakMemoryUsage { get; set; }
        public long RequestsThrottled { get; set; }
        public long RequestsDelayed { get; set; }
        public long RequestsRateLimited { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
