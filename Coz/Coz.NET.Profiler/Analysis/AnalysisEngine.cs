using System;
using System.Collections.Generic;
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
        private readonly List<ProfileMeasurement> profileMeasurements;
        private readonly List<CozSnapshot> cozSnapshots;

        public AnalysisEngine()
        {
            profileMeasurements = new List<ProfileMeasurement>();
            cozSnapshots = new List<CozSnapshot>();
            ipcService = new IPCService();
        }

        public void Start()
        {
            ipcService.Open();
        }

        public void Stop()
        {
            ipcService.Close();
        }

        public void Analyze(AnalysisConfig config)
        {
            ExecuteBaselineRuns(config);
            List<Experiment.Experiment> experiments = ScheduleExperiments(config);
            ExecuteExperimentalRuns(config, experiments);
        }

        public AnalysisReport GenerateReport()
        {
            return null;
        }

        private void ExecuteBaselineRuns(AnalysisConfig config)
        {
            for (int i = 0; i < config.BaselineRuns; i++)
            {
                var experiment = new Experiment.Experiment
                {
                    Id = Guid.NewGuid().ToString()
                };
                ipcService.Send(experiment); 
                StartProcess(config.ExecutablePath, config.Arguments);
                var profileMeasurement = ipcService.Receive<ProfileMeasurement>();
                profileMeasurements.Add(profileMeasurement);
            }
        }

        private List<Experiment.Experiment> ScheduleExperiments(AnalysisConfig config)
        {
            var averageMethodDurations = profileMeasurements
                .SelectMany(x => x.MethodMeasurements)
                .ToDictionary(x => x.MethodId, x => x.Latencies.Average());
            var apportionedTotalDuration = averageMethodDurations.Values.Sum();
            averageMethodDurations = averageMethodDurations
                .Where(x => x.Value / apportionedTotalDuration > config.CutoffPercentage)
                .ToDictionary(x => x.Key, x => x.Value);

            var experiments = new List<Experiment.Experiment>();

            foreach (float percentageSpeedup in config.PercentageSpeedups)
            {
                foreach (string methodId in averageMethodDurations.Keys)
                {
                    var duration = averageMethodDurations[methodId];
                    var percentageSlowdown = percentageSpeedup / (1 - percentageSpeedup);
                    var slowdown = (int)(percentageSlowdown * duration);

                    if (slowdown == 0)
                    {
                        Console.WriteLine("WARN: Experiment for method was dropped");
                        continue;
                    }

                    var experiment = new Experiment.Experiment
                    {
                        Id = Guid.NewGuid().ToString(),
                        MethodId = methodId,
                        MethodSlowdown = slowdown
                    };

                    experiments.Add(experiment);
                }
            }

            return experiments;
        }

        private void ExecuteExperimentalRuns(AnalysisConfig config, List<Experiment.Experiment> experiments)
        {
            foreach (var experiment in experiments)
            {
                ipcService.Send(experiment);
                StartProcess(config.ExecutablePath, config.Arguments);
                var cozSnapshot = ipcService.Receive<CozSnapshot>();
                cozSnapshots.Add(cozSnapshot);
            }
        }

        private static void StartProcess(string executablePath, string arguments)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        } 
    }


}
