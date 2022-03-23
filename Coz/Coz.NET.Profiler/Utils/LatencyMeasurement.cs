using System.Diagnostics;

namespace Coz.NET.Profiler.Utils
{
    public class LatencyMeasurement
    {
        private readonly Stopwatch stopwatch;

        public LatencyMeasurement()
        {
            stopwatch = new Stopwatch();
        }

        public bool IsFinished => !stopwatch.IsRunning;

        public void Start() 
        {
            stopwatch.Start();
        }

        public void Stop()
        {
            stopwatch.Stop();
        }

        public long GetDuration()
        {
            return stopwatch.ElapsedTicks;
        } 
    }
}