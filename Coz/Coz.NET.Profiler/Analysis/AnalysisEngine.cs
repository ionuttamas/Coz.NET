using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Coz.NET.Profiler.IPC;
using Coz.NET.Profiler.Marker;
using Coz.NET.Profiler.Profile;

namespace Coz.NET.Profiler.Analysis
{
    public class AnalysisEngine
    {
        private readonly IPCService ipcService; 

        public AnalysisEngine()
        { 
            ipcService = new IPCService();
        }

        public void Start()
        {
            ipcService.Start();
        }

        public void Stop()
        {
            ipcService.Stop();
        }

        public AnalysisReport Analyze(AnalysisConfig config)
        {
            List<ProfileMeasurement> baselineMeasurements = ExecuteBaselineRuns(config);
            BaselineSummary baselineSummary = ComputeBaselineSummary(config, baselineMeasurements);
            List<Experiment.Experiment> experiments = ScheduleExperiments(config, baselineSummary);
            List<CozSnapshot> cozSnapshots = ExecuteExperimentalRuns(config, experiments);
            AnalysisReport report = GenerateReport(experiments, baselineSummary, cozSnapshots);

            return report;
        }

        private List<ProfileMeasurement> ExecuteBaselineRuns(AnalysisConfig config)
        {
            var baselineMeasurements = new List<ProfileMeasurement>();

            for (int i = 0; i < config.BaselineRuns; i++)
            {
                var experiment = new Experiment.Experiment
                {
                    Id = Guid.NewGuid().ToString()
                };
                ipcService.Send(experiment); 
                StartProcess(config.ExecutablePath, config.Arguments);
                var profileMeasurement = ipcService.Receive<ProfileMeasurement>();
                baselineMeasurements.Add(profileMeasurement);
            }

            return baselineMeasurements;
        }

        private List<Experiment.Experiment> ScheduleExperiments(AnalysisConfig config, BaselineSummary baselineSummary)
        {
            var experiments = new List<Experiment.Experiment>();

            foreach (float percentageSpeedup in config.PercentageSpeedups)
            {
                foreach (string methodId in baselineSummary.MethodsLatencies.Keys)
                {
                    if(config.ExcludedMethodIds.Contains(methodId))
                        continue;

                    var duration = baselineSummary.MethodsLatencies[methodId];
                    var percentageSlowdown = percentageSpeedup / (1 - percentageSpeedup);
                    var slowdown = (int)(percentageSlowdown * duration);

                    if (slowdown == 0)
                    {
                        Console.WriteLine($"WARN: Experiment for [methodId: {methodId}] was dropped");
                        continue;
                    }

                    var experiment = new Experiment.Experiment
                    {
                        Id = Guid.NewGuid().ToString(),
                        MethodId = methodId,
                        MethodPercentageSlowdown = percentageSlowdown,
                        MethodSlowdown = slowdown
                    };
                    experiments.Add(experiment);
                }
            }

            return experiments;
        }

        private List<CozSnapshot> ExecuteExperimentalRuns(AnalysisConfig config, List<Experiment.Experiment> experiments)
        {
            var cozSnapshots = new List<CozSnapshot>();

            foreach (var experiment in experiments)
            {
                ipcService.Send(experiment);
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                StartProcess(config.ExecutablePath, config.Arguments);
                stopwatch.Stop();
                var cozSnapshot = ipcService.Receive<CozSnapshot>();
                cozSnapshot.Throughputs = cozSnapshot.Throughputs.Select(x => x / stopwatch.ElapsedMilliseconds).ToList();
                cozSnapshots.Add(cozSnapshot);
            }

            return cozSnapshots;
        }

        private BaselineSummary ComputeBaselineSummary(AnalysisConfig config, List<ProfileMeasurement> baselineMeasurements)
        {
            var methodLatencies = baselineMeasurements
                .SelectMany(x => x.MethodMeasurements)
                .GroupBy(x=>x.MethodId)
                .ToDictionary(x => x.Key, x => x.SelectMany(m=>m.Latencies).Average());
            var apportionedTotalDuration = methodLatencies.Values.Sum(); 
            //methodLatencies = methodLatencies
            //    .Where(x => x.Value / apportionedTotalDuration > config.CutoffPercentage)
            //    .ToDictionary(x => x.Key, x => x.Value);
            var baselineSummary = new BaselineSummary();
            var cozLatencies = baselineMeasurements
                .Select(x => FlattenLatencies(x.CozSnapshot))
                .SelectMany(x=>x)
                .GroupBy(x=>x.Key)
                .ToDictionary(x=>x.Key, x=>x.SelectMany(m=>m.Value).Average());
            var cozThroughputs = baselineMeasurements
                .Select(x => FlattenThroughputs(x.CozSnapshot))
                .SelectMany(x => x)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.SelectMany(m => m.Value).Average()); 

            baselineSummary.MethodsLatencies = methodLatencies;
            baselineSummary.CozLatencies = cozLatencies;
            baselineSummary.CozThrougputs = cozThroughputs;

            return baselineSummary;
        }

        private AnalysisReport GenerateReport(List<Experiment.Experiment> experiments, BaselineSummary baselineSummary, List<CozSnapshot> cozSnapshots)
        {
            var methodSpeedups = new List<MethodSpeedup>();

            foreach (Experiment.Experiment experiment in experiments)
            {
                var cozSnapshot = cozSnapshots.First(x => x.ExperimentId == experiment.Id);
                var methodPercentageSpeedup = experiment.MethodPercentageSlowdown/(1 + experiment.MethodPercentageSlowdown);
                var latencyPercentageSpeedups = new Dictionary<string, double>();
                var throughputPercentageSpeedups = new Dictionary<string, double>();

                for (int i = 0; i < cozSnapshot.LatencyTags.Count; i++)
                {
                    var latencyTag = cozSnapshot.LatencyTags[i];
                    latencyPercentageSpeedups[latencyTag] = (cozSnapshot.Latencies[i] - baselineSummary.CozLatencies[latencyTag])/baselineSummary.CozLatencies[latencyTag];
                }

                for (int i = 0; i < cozSnapshot.ThroughputTags.Count; i++)
                {
                    var throughputTag = cozSnapshot.ThroughputTags[i];
                    throughputPercentageSpeedups[throughputTag] = (cozSnapshot.Throughputs[i]- baselineSummary.CozThrougputs[throughputTag])/baselineSummary.CozThrougputs[throughputTag];
                }

                var methodSpeedup = new MethodSpeedup
                {
                    MethodId = experiment.MethodId,
                    PercentageSpeedup = methodPercentageSpeedup,
                    LatencySpeedups = latencyPercentageSpeedups,
                    ThroughputSpeedups = throughputPercentageSpeedups
                };
                methodSpeedups.Add(methodSpeedup);
            }

            return new AnalysisReport(methodSpeedups);
        }

        private Dictionary<string, List<long>> FlattenLatencies(CozSnapshot snapshot)
        {
            var latencies = Enumerable.Range(0, snapshot.Latencies.Count)
                .Select(ix=>(snapshot.LatencyTags[ix], snapshot.Latencies[ix]))
                .GroupBy(x => x.Item1)
                .Select(x => (x.Key, x.Select(m => m.Item2).ToList()))
                .ToDictionary(x=>x.Key, x=>x.Item2.ToList());

            return latencies;
        }

        private Dictionary<string, List<double>> FlattenThroughputs(CozSnapshot snapshot)
        {
            var throughputs = Enumerable.Range(0, snapshot.Throughputs.Count)
                .Select(ix => (snapshot.ThroughputTags[ix], snapshot.Throughputs[ix]))
                .GroupBy(x => x.Item1)
                .Select(x => (x.Key, x.Select(m => m.Item2).ToList()))
                .ToDictionary(x => x.Key, x => x.Item2.ToList());

            return throughputs;
        }

        private static void StartProcess(string executablePath, string arguments)
        {
            var process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = Path.GetDirectoryName(executablePath),
                    FileName = executablePath,
                    Arguments = arguments,
                    RedirectStandardOutput = false,
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        } 

        private class BaselineSummary
        {
            public Dictionary<string, double> MethodsLatencies { get; set; }
            public Dictionary<string, double> CozLatencies { get; set; }
            public Dictionary<string, double> CozThrougputs { get; set; }
        }
    }


}
