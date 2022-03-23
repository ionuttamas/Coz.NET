using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Coz.NET.Profiler.IPC;
using Coz.NET.Profiler.Profile;

namespace Coz.NET.Profiler.Marker
{
    public static class Coz
    {
        private static readonly ConcurrentDictionary<string, long> Counters;
        private static readonly ConcurrentDictionary<string, LatencyMeasurement> Latencies;
        private static readonly ConcurrentBag<(string, LatencyMeasurement)> ProcessedLatencies;
        private static readonly Experiment.Experiment Experiment;
        private static readonly IPCService IpcService;

        static Coz()
        {
            Counters = new ConcurrentDictionary<string, long>();
            Latencies = new ConcurrentDictionary<string, LatencyMeasurement>();
            ProcessedLatencies = new ConcurrentBag<(string, LatencyMeasurement)>();
            IpcService = new IPCService();
            IpcService.Open();
            Experiment = IpcService.Receive<Experiment.Experiment>();
            Process.GetCurrentProcess().Exited += OnExited;
        }

        public static void Throughput(string tag)
        {
            if(tag.Any(x=>!char.IsLetterOrDigit(x)))
                throw new ArgumentException($"Specified tag: [{tag}] must contain only letters or digits");

            if (Counters.ContainsKey(tag))
                throw new ArgumentException($"Specified tag: [{tag}] was already used for marking");

            if (Counters.TryAdd(tag, 0)) 
                return;
            
            while (true)
            {
                if(!Counters.TryGetValue(tag, out long counter))
                    throw new InvalidOperationException($"Could not add nor get the counter value for tag: [{tag}]");

                if (Counters.TryUpdate(tag, counter + 1, counter))
                    return;
            }
        }

        public static void StartLatency(string tag)
        {
            var latency = new LatencyMeasurement();
            latency.Start();

            if (tag.Any(x => !char.IsLetterOrDigit(x)))
                throw new ArgumentException($"Specified tag: [{tag}] must contain only letters or digits");

            if (Latencies.ContainsKey(tag))
                //TODO: this is more tricky; at the moment we only allow non-concurrent latency measurements, just multiple & sequential latency measurements
                throw new ArgumentException($"Specified tag: [{tag}] was already used for tracking latency start");

            if (Latencies.TryAdd(tag, latency))
                return;

            throw new InvalidOperationException($"Could not add the latency measurement for tag: [{tag}]");
        }

        public static void EndLatency(string tag)
        {
            if (tag.Any(x => !char.IsLetterOrDigit(x)))
                throw new ArgumentException($"Specified tag: [{tag}] must contain only letters or digits");

            if (!Latencies.ContainsKey(tag))
                throw new ArgumentException($"Specified tag: [{tag}] was not used for latency start");

            if (!Latencies.TryRemove(tag, out LatencyMeasurement latency))
                throw new ArgumentException($"Could not complete latency processing for tag: [{tag}]");

            latency.Stop();
            ProcessedLatencies.Add((tag, latency));
        }

        public static string GetThroughputSnapshot()
        {   
            var snapshot = Counters.ToArray();

            return string.Join(',', snapshot.Select(x => $"{x.Key}:{x.Value}"));
        }

        public static string GetLatenciesSnapshot()
        {
            var snapshot = ProcessedLatencies.ToArray().Where(x => x.Item2.IsFinished);

            return string.Join(',', snapshot.Select(x => $"{x.Item1}:{x.Item2.Duration}"));
        }

        private static void OnExited(object sender, EventArgs e)
        {
            var throughput = GetThroughputSnapshot();
            var latencies = GetLatenciesSnapshot();
            var snapshot = new CozSnapshot
            {
                ExperimentId = Experiment.Id,
                Throughput = throughput,
                Latencies = latencies
            };
            IpcService.Send(snapshot);
        }
    }
}
