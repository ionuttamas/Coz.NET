using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Coz.NET.Profiler.IPC;

namespace Coz.NET.Profiler.Profile
{
    public static class Profile
    {
        private static long currentCallId;
        private static readonly Experiment.Experiment Experiment;
        private static readonly ConcurrentDictionary<string, LatencyMeasurement> MethodLatencies;
        private static readonly ConcurrentBag<(string, LatencyMeasurement)> ProcessedLatencies;
        private static readonly IPCService IpcService;

        static Profile()
        {
            MethodLatencies = new ConcurrentDictionary<string, LatencyMeasurement>();
            ProcessedLatencies = new ConcurrentBag<(string, LatencyMeasurement)>();
            currentCallId = long.MinValue;

            IpcService = new IPCService();
            IpcService.Open();
            Experiment = IpcService.Receive<Experiment.Experiment>();
            Process.GetCurrentProcess().Exited += OnExited;
        }
         
        public static void Slowdown([CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            var methodId = $"{callerFilePath}:{callerMember}:{callerLineNumber}";

            if (methodId!=Experiment.MethodId)
                return;
             
            Thread.Sleep(Experiment.MethodSlowdown);
        }

        public static long StartRecord([CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            if (currentCallId == long.MaxValue)
                throw new InvalidOperationException("Too may method calls were recorded for this experiment.");

            var snapshotCallId = currentCallId;
            var method = $"{callerFilePath}:{callerMember}:{callerLineNumber}:{currentCallId}";
            Interlocked.Increment(ref currentCallId);

            var latency = new LatencyMeasurement();
            latency.Start();

            if (!MethodLatencies.TryAdd(method, latency))
                throw new InvalidOperationException($"Could not start the latency measurement for method: [{method}]");

            return snapshotCallId;
        }

        public static void EndRecord(long startedCallId, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            var augmentedMethodId = $"{callerFilePath}:{callerMember}:{callerLineNumber}:{startedCallId}";

            if (!MethodLatencies.TryRemove(augmentedMethodId, out LatencyMeasurement latency))
                throw new ArgumentException($"Could not end latency measurement for method: [{augmentedMethodId}]");

            latency.Stop();
            ProcessedLatencies.Add((augmentedMethodId, latency));
        }

        private static void OnExited(object sender, EventArgs e)
        {
            var methodMeasurements = ProcessedLatencies
                .Select(x => (GetMethodId(x.Item1), x.Item2))
                .GroupBy(x => x.Item1)
                .Select(x => new MethodMeasurement
                {
                    MethodId = x.Key,
                    Latencies = x.Select(m => m.Item2.Duration).ToList()
                })
                .ToList();
            var profileMeasurement = new ProfileMeasurement
            {
                MethodMeasurements = methodMeasurements
            };
            IpcService.Send(profileMeasurement);
        }

        private static string GetMethodId(string augmentedMethodId)
        {
            var methodId = augmentedMethodId.Substring(0, augmentedMethodId.LastIndexOf(':'));

            return methodId;
        }
    }
}
