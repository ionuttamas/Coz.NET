using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Coz.NET.CodeProcessor.Processor;
using Coz.NET.Profiler.Analysis;

namespace Coz.NET.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            var codeProcessor = new CodeProcessor.Processor.CodeProcessor();
            var solutionFolder =          @"C:\Users\tamas\Documents\Coz.NET\SampleApp.Latency";
            var generatedSolutionFolder = @"C:\Users\tamas\Documents\Coz.NET\Temp_SampleApp.Latency";
            var solutionFilename = @"SampleApp.Latency.sln";
            var excludedMethodIds = new List<string> {$@"{generatedSolutionFolder}\SampleApp.Latency\Program.cs:Main" };
            var executablePath = $@"{generatedSolutionFolder}\SampleApp.Latency\bin\Release\netcoreapp3.1\SampleApp.Latency.exe";
            var codeLocation = new CodeLocation
            {
                SolutionFolder = solutionFolder,
                GeneratedSolutionFolder = generatedSolutionFolder,
                SolutionFilename = solutionFilename
            };
            //codeProcessor.RegenerateSolution(codeLocation);
            //codeProcessor.BuildProjects(codeLocation);
            var arguments = string.Join(string.Empty, args.Skip(1));
            var config = new AnalysisConfig
            {
                BaselineRuns = 3,
                CutoffPercentage = 0.05f,
                ExecutablePath = executablePath,
                Arguments = arguments,
                ExcludedMethodIds = excludedMethodIds,
                PercentageSpeedups = Enumerable.Range(1, 9).Select(x=>1f*x/10).ToList()
            };
            var engine = new AnalysisEngine();
            engine.Start();
            var report = engine.Analyze(config);
            engine.Stop();
            var json = JsonSerializer.Serialize(report);
            File.WriteAllText("Report.json", json); 
        }
    }
}
