using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Coz.NET.Profiler.Utils;

namespace Coz.NET.Profiler.Experiment
{
    public static class ProfileMeasurement
    {
        private static long currentCallId;
        private static readonly ConcurrentDictionary<string, LatencyMeasurement> MethodLatencies;
        private static readonly ConcurrentBag<(string, LatencyMeasurement)> ProcessedLatencies;

        static ProfileMeasurement()
        {
            MethodLatencies = new ConcurrentDictionary<string, LatencyMeasurement>();
            ProcessedLatencies = new ConcurrentBag<(string, LatencyMeasurement)>();
            currentCallId = long.MinValue;
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
            var method = $"{callerFilePath}:{callerMember}:{callerLineNumber}:{startedCallId}";

            if (!MethodLatencies.TryRemove(method, out LatencyMeasurement latency))
                throw new ArgumentException($"Could not end latency measurement for method: [{method}]");

            latency.Stop();
            ProcessedLatencies.Add((method, latency));
        }
    }
}
