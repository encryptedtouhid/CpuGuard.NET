using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CpuGuard.NET.Services;
using Microsoft.AspNetCore.Http;

namespace CpuGuard.NET.Dashboard
{
    /// <summary>
    /// Middleware that serves the CpuGuard dashboard HTML page.
    /// </summary>
    public class DashboardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _path;
        private readonly string _statsPath;
        private readonly Lazy<string> _dashboardHtml;

        /// <summary>
        /// Creates a new DashboardMiddleware.
        /// </summary>
        public DashboardMiddleware(RequestDelegate next, string path = "/cpuguard/dashboard", string statsPath = "/cpuguard/stats")
        {
            _next = next;
            _path = path;
            _statsPath = statsPath;
            _dashboardHtml = new Lazy<string>(() => LoadDashboardHtml());
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Equals(_path, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";

                var html = _dashboardHtml.Value.Replace("{{STATS_ENDPOINT}}", _statsPath);
                await context.Response.WriteAsync(html);
                return;
            }

            await _next(context);
        }

        private string LoadDashboardHtml()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CpuGuard.NET.Dashboard.dashboard.html";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }

            // Fallback: generate basic dashboard HTML
            return GenerateFallbackDashboard();
        }

        private string GenerateFallbackDashboard()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>CpuGuard.NET Dashboard</title>
    <script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #1a1a2e; color: #eee; padding: 20px; }
        h1 { color: #00d4ff; margin-bottom: 20px; }
        .dashboard { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
        .card { background: #16213e; border-radius: 12px; padding: 20px; box-shadow: 0 4px 6px rgba(0,0,0,0.3); }
        .card h2 { color: #00d4ff; font-size: 14px; text-transform: uppercase; margin-bottom: 15px; }
        .metric { font-size: 36px; font-weight: bold; }
        .metric.cpu { color: #ff6b6b; }
        .metric.memory { color: #4ecdc4; }
        .metric.requests { color: #ffe66d; }
        .sub-metric { font-size: 14px; color: #888; margin-top: 5px; }
        .chart-container { height: 200px; margin-top: 15px; }
        .stats-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
        .stat-item { background: #0f3460; padding: 15px; border-radius: 8px; }
        .stat-label { font-size: 12px; color: #888; }
        .stat-value { font-size: 24px; font-weight: bold; color: #00d4ff; }
        .status { display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 12px; }
        .status.healthy { background: #2ecc71; color: #fff; }
        .status.degraded { background: #f39c12; color: #fff; }
        .status.unhealthy { background: #e74c3c; color: #fff; }
        .refresh-info { text-align: center; color: #666; font-size: 12px; margin-top: 20px; }
    </style>
</head>
<body>
    <h1>CpuGuard.NET Dashboard</h1>
    <div class=""dashboard"">
        <div class=""card"">
            <h2>CPU Usage</h2>
            <div class=""metric cpu"" id=""cpu-value"">--%</div>
            <div class=""sub-metric"">Peak: <span id=""cpu-peak"">--</span>% | Avg: <span id=""cpu-avg"">--</span>%</div>
            <div class=""chart-container""><canvas id=""cpu-chart""></canvas></div>
        </div>
        <div class=""card"">
            <h2>Memory Usage</h2>
            <div class=""metric memory"" id=""memory-value"">--%</div>
            <div class=""sub-metric"">Used: <span id=""memory-used"">--</span> MB / <span id=""memory-total"">--</span> MB</div>
            <div class=""chart-container""><canvas id=""memory-chart""></canvas></div>
        </div>
        <div class=""card"">
            <h2>Request Statistics</h2>
            <div class=""stats-grid"">
                <div class=""stat-item"">
                    <div class=""stat-label"">Total Requests</div>
                    <div class=""stat-value"" id=""total-requests"">--</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-label"">Throttled</div>
                    <div class=""stat-value"" id=""throttled"">--</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-label"">Delayed</div>
                    <div class=""stat-value"" id=""delayed"">--</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-label"">Rate Limited</div>
                    <div class=""stat-value"" id=""rate-limited"">--</div>
                </div>
            </div>
        </div>
        <div class=""card"">
            <h2>System Status</h2>
            <div class=""stats-grid"">
                <div class=""stat-item"">
                    <div class=""stat-label"">Uptime</div>
                    <div class=""stat-value"" id=""uptime"">--</div>
                </div>
                <div class=""stat-item"">
                    <div class=""stat-label"">Status</div>
                    <div class=""stat-value""><span class=""status healthy"" id=""status"">Healthy</span></div>
                </div>
            </div>
        </div>
    </div>
    <div class=""refresh-info"">Auto-refreshing every 2 seconds | Last update: <span id=""last-update"">--</span></div>
    <script>
        const statsEndpoint = '{{STATS_ENDPOINT}}';
        const cpuData = { labels: [], data: [] };
        const memoryData = { labels: [], data: [] };
        const maxDataPoints = 30;

        const cpuChart = new Chart(document.getElementById('cpu-chart'), {
            type: 'line',
            data: { labels: cpuData.labels, datasets: [{ label: 'CPU %', data: cpuData.data, borderColor: '#ff6b6b', tension: 0.3, fill: false }] },
            options: { responsive: true, maintainAspectRatio: false, scales: { y: { min: 0, max: 100 } }, plugins: { legend: { display: false } } }
        });

        const memoryChart = new Chart(document.getElementById('memory-chart'), {
            type: 'line',
            data: { labels: memoryData.labels, datasets: [{ label: 'Memory %', data: memoryData.data, borderColor: '#4ecdc4', tension: 0.3, fill: false }] },
            options: { responsive: true, maintainAspectRatio: false, scales: { y: { min: 0, max: 100 } }, plugins: { legend: { display: false } } }
        });

        function formatUptime(seconds) {
            const h = Math.floor(seconds / 3600);
            const m = Math.floor((seconds % 3600) / 60);
            const s = Math.floor(seconds % 60);
            return `${h}h ${m}m ${s}s`;
        }

        function getStatus(cpu, memory) {
            if (cpu > 90 || memory > 90) return { class: 'unhealthy', text: 'Unhealthy' };
            if (cpu > 70 || memory > 70) return { class: 'degraded', text: 'Degraded' };
            return { class: 'healthy', text: 'Healthy' };
        }

        async function updateDashboard() {
            try {
                const response = await fetch(statsEndpoint);
                const data = await response.json();
                const now = new Date().toLocaleTimeString();

                document.getElementById('cpu-value').textContent = data.currentCpuUsage.toFixed(1) + '%';
                document.getElementById('cpu-peak').textContent = data.peakCpuUsage.toFixed(1);
                document.getElementById('cpu-avg').textContent = data.averageCpuUsage.toFixed(1);

                document.getElementById('memory-value').textContent = data.currentMemoryUsage.toFixed(1) + '%';
                document.getElementById('memory-used').textContent = (data.currentMemoryBytes / 1024 / 1024).toFixed(0);
                document.getElementById('memory-total').textContent = (data.totalMemoryBytes / 1024 / 1024).toFixed(0);

                document.getElementById('total-requests').textContent = data.totalRequests;
                document.getElementById('throttled').textContent = data.totalRequestsThrottled;
                document.getElementById('delayed').textContent = data.totalRequestsDelayed;
                document.getElementById('rate-limited').textContent = data.totalRequestsRateLimited;

                document.getElementById('uptime').textContent = formatUptime(data.uptimeSeconds);

                const status = getStatus(data.currentCpuUsage, data.currentMemoryUsage);
                const statusEl = document.getElementById('status');
                statusEl.textContent = status.text;
                statusEl.className = 'status ' + status.class;

                document.getElementById('last-update').textContent = now;

                cpuData.labels.push(now);
                cpuData.data.push(data.currentCpuUsage);
                memoryData.labels.push(now);
                memoryData.data.push(data.currentMemoryUsage);

                if (cpuData.labels.length > maxDataPoints) {
                    cpuData.labels.shift();
                    cpuData.data.shift();
                    memoryData.labels.shift();
                    memoryData.data.shift();
                }

                cpuChart.update();
                memoryChart.update();
            } catch (e) {
                console.error('Failed to fetch stats:', e);
            }
        }

        updateDashboard();
        setInterval(updateDashboard, 2000);
    </script>
</body>
</html>";
        }
    }
}
