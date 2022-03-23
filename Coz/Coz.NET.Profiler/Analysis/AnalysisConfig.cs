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
    }

    public class AnalysisReport
    {
        public List<MethodReport> MethodReports { get; set; }
    }

    public class MethodReport
    {
        public string MethodId { get; set; }
        public string PercentageSpeedup { get; set; }
    }
}