using System;
using System.Diagnostics;
using System.Threading;

namespace PersonaAI.Server
{
    /// <summary>
    /// Tracks server-wide metrics: uptime, request count, connected clients.
    /// </summary>
    public class ServerMetrics
    {
        private readonly Stopwatch _uptimeWatch;
        private long _totalRequests;
        private int _activeConnections;

        public ServerMetrics()
        {
            _uptimeWatch = Stopwatch.StartNew();
        }

        public TimeSpan Uptime => _uptimeWatch.Elapsed;

        public long TotalRequests => Interlocked.Read(ref _totalRequests);

        public int ActiveConnections => _activeConnections;

        public void IncrementRequests()
        {
            Interlocked.Increment(ref _totalRequests);
        }

        public void AddConnection()
        {
            Interlocked.Increment(ref _activeConnections);
        }

        public void RemoveConnection()
        {
            Interlocked.Decrement(ref _activeConnections);
        }

        public string GetUptimeDisplay()
        {
            var ts = _uptimeWatch.Elapsed;
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m {ts.Seconds}s";
        }
    }
}
