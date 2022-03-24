using System.Collections.Generic;

namespace Coz.NET.Profiler.Analysis
{
    public class MethodSpeedup
    {
        public string MethodId { get; set; }
        public float PercentageSpeedup { get; set; }
        public Dictionary<string, double> LatencySpeedups { get; set; }
        public Dictionary<string, double> ThroughputSpeedups { get; set; }
    }
}