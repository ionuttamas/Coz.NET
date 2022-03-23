using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Coz.NET.Profiler.Utils;

namespace Coz.NET.Profiler.Marker
{
    public static class Coz
    {
        private static readonly ConcurrentDictionary<string, long> Counters;
        private static readonly ConcurrentDictionary<string, LatencyMeasurement> Latencies;
        private static readonly ConcurrentBag<(string, LatencyMeasurement)> ProcessedLatencies;

        static Coz()
        {
            Counters = new ConcurrentDictionary<string, long>();
            Latencies = new ConcurrentDictionary<string, LatencyMeasurement>();
            ProcessedLatencies = new ConcurrentBag<(string, LatencyMeasurement)>();
            Process.GetCurrentProcess().Exited += Coz_Exited;
        }

        private static void Coz_Exited(object sender, EventArgs e)
        {
            var throughputSnapshot = GetThroughputSnapshot();
            var latencySnapshot = GetLatencySnapshot();
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

        public static string GetLatencySnapshot()
        {
            var snapshot = ProcessedLatencies.ToArray().Where(x => x.Item2.IsFinished);

            return string.Join(',', snapshot.Select(x => $"{x.Item1}:{x.Item2.GetDuration()}"));
        }
    }
}
