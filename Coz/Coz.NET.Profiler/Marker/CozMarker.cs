﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Coz.NET.Profiler.IPC;
using Coz.NET.Profiler.Profile;

namespace Coz.NET.Profiler.Marker
{
    public static class CozMarker
    {
        private static readonly ConcurrentDictionary<string, long> Counters;
        private static readonly ConcurrentDictionary<string, LatencyMeasurement> Latencies;
        private static readonly ConcurrentBag<(string, LatencyMeasurement)> ProcessedLatencies;
        private static readonly Experiment.Experiment Experiment;
        private static readonly IPCService IpcService;

        static CozMarker()
        {
            Counters = new ConcurrentDictionary<string, long>();
            Latencies = new ConcurrentDictionary<string, LatencyMeasurement>();
            ProcessedLatencies = new ConcurrentBag<(string, LatencyMeasurement)>();
            IpcService = new IPCService();
            IpcService.Start();
            Experiment = IpcService.Receive<Experiment.Experiment>(); 
            AppDomain.CurrentDomain.ProcessExit += OnExited;
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
            latency.Resume();

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

            latency.Finish();
            ProcessedLatencies.Add((tag, latency));
        }

        public static (List<string>, List<double>) GetThroughputSnapshot()
        {   
            var snapshot = Counters.ToArray();

            return (snapshot.Select(x => x.Key).ToList(), snapshot.Select(x => (double)x.Value).ToList());
        }

        public static (List<string>, List<long>) GetLatenciesSnapshot()
        {
            var snapshot = ProcessedLatencies.ToArray().Where(x => x.Item2.Completed).ToList();

            return (snapshot.Select(x => x.Item1).ToList(), snapshot.Select(x => x.Item2.Duration).ToList());
        }

        private static void OnExited(object sender, EventArgs e)
        {
            //We don't send coz snapshot info in case of baseline experiments as these will be sent by ProfileMarker 
            if(Experiment.IsBaseline)
                return;

            var (throughputTags, throughputs) = GetThroughputSnapshot();
            var (latencyTags, latencies) = GetLatenciesSnapshot();
            var snapshot = new CozSnapshot
            {
                ExperimentId = Experiment.Id,
                ThroughputTags = throughputTags,
                Throughputs = throughputs,
                LatencyTags = latencyTags,
                Latencies = latencies,
            };
            IpcService.Send(snapshot);
        }
    }
}
