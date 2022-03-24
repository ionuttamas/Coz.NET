using System.Collections.Generic;

namespace Coz.NET.Profiler.Analysis
{
    public class AnalysisReport
    {
        public AnalysisReport(List<MethodSpeedup> methodSpeedups)
        {
            MethodSpeedups = methodSpeedups;
        }

        public List<MethodSpeedup> MethodSpeedups { get; }
    }
}