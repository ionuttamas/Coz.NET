using System.Collections.Generic;

namespace Coz.NET.Profiler.Analysis
{
    public class AnalysisConfig
    {
        public int BaselineRuns { get; set; }
        public float CutoffPercentage { get; set; }
        public List<float> PercentageSpeedups { get; set; }
        public string ExecutablePath { get; set; }
        public string Arguments { get; set; }
        public List<string> ExcludedMethodIds { get; set; }
    }
}