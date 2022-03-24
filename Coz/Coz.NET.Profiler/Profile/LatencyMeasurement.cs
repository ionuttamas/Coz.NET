using System.Diagnostics;

namespace Coz.NET.Profiler.Profile
{
    public class LatencyMeasurement
    {
        private readonly Stopwatch stopwatch;
        private bool completed;

        public LatencyMeasurement()
        {
            stopwatch = new Stopwatch();
            completed = false;
        }

        public bool Completed => completed;

        public long Duration => stopwatch.ElapsedMilliseconds;

        public void Resume() 
        {
            stopwatch.Start();
        }

        public void Pause()
        {
            stopwatch.Stop();
        }

        public void Finish()
        {
            if (!completed)
            {
                stopwatch.Stop();
            }
            
            completed = true;
        }
    }
}