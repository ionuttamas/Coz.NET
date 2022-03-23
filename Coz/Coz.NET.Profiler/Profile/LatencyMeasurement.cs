using System.Diagnostics;

namespace Coz.NET.Profiler.Profile
{
    public class LatencyMeasurement
    {
        private readonly Stopwatch stopwatch;

        public LatencyMeasurement()
        {
            stopwatch = new Stopwatch();
        }

        public bool IsFinished => !stopwatch.IsRunning;

        public long Duration => stopwatch.ElapsedTicks;

        public void Start() 
        {
            stopwatch.Start();
        }

        public void Stop()
        {
            stopwatch.Stop();
        }
    }
}