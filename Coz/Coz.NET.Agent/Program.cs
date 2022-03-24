using System;
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
            var codeLocation = new CodeLocation
            {
                SolutionFolder = @"C:\Users\tamas\Documents\Coz.NET\SampleApp.Latency",
                GeneratedSolutionFolder = @"C:\Users\tamas\Documents\Coz.NET\Temp_SampleApp.Latency",
                SolutionFilename = @"SampleApp.Latency.sln"
            }; 
            //codeProcessor.RegenerateSolution(codeLocation);
            //codeProcessor.BuildProjects(codeLocation);

            var temp = @"C:\Users\tamas\Documents\Coz.NET\SampleApp.Latency\SampleApp.Latency\bin\Debug\netcoreapp3.1\SampleApp.Latency.exe";
            //TODO: this only works if the generated exe is within the solution folder
            var executablePath = temp.Replace(codeLocation.SolutionFolder, codeLocation.GeneratedSolutionFolder);
            var arguments = string.Join(string.Empty, args.Skip(1));

            var config = new AnalysisConfig
            {
                BaselineRuns = 3,
                CutoffPercentage = 0.05f,
                ExecutablePath = executablePath,
                Arguments = arguments,
                PercentageSpeedups = Enumerable.Range(1, 9).Select(x=>1f/10).ToList()
            };
            var engine = new AnalysisEngine();
            engine.Start();
            var report = engine.Analyze(config);
            var json = JsonSerializer.Serialize(report);
            File.WriteAllText("Report.json", json);

            Console.WriteLine("Hello World!");
        }
    }
}
