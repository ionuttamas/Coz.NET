using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Coz.NET.Profiler.IPC;
using Coz.NET.Profiler.Marker;
using static System.AppDomain;

namespace Coz.NET.Profiler.Profile
{
    public static class ProfileMarker
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint ResumeThread(IntPtr hThread);

        private static long currentCallId;
        private static readonly Experiment.Experiment Experiment;
        private static readonly ConcurrentDictionary<string, LatencyMeasurement> MethodLatencies;
        private static readonly ConcurrentBag<(string, LatencyMeasurement)> ProcessedLatencies;
        private static long methodCalls;
        private static readonly IPCService IpcService;

        static ProfileMarker()
        {
            MethodLatencies = new ConcurrentDictionary<string, LatencyMeasurement>();
            ProcessedLatencies = new ConcurrentBag<(string, LatencyMeasurement)>();
            methodCalls = 0;
            currentCallId = long.MinValue;

            IpcService = new IPCService();
            IpcService.Start();
            Experiment = IpcService.Receive<Experiment.Experiment>(); 
            CurrentDomain.ProcessExit += OnExited;
        }
         
        public static void Slowdown([CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            var methodId = $"{callerFilePath}:{callerMember}"; //:{callerLineNumber}

            if (methodId!=Experiment.MethodId)
                return;

            ProcessThreadCollection threads = Process.GetCurrentProcess().Threads;
            var currentThreadId = GetCurrentThreadId();
            var suspendedThreadHandles = new List<IntPtr>();

            System.Timers.Timer timer = new System.Timers.Timer
            {
                Interval = Experiment.MethodSlowdown
            };
            timer.Elapsed += (sender, args) =>
            {
                foreach (IntPtr handle in suspendedThreadHandles)
                {
                    ResumeThread(handle);
                    Marshal.FreeHGlobal(handle);
                }
            };

            for (int i = 0; i < threads.Count; i++)
            {
                ProcessThread processThread = threads[i];
                var threadId = processThread.Id;

                if (threadId != currentThreadId)
                {
                    IntPtr threadHandle = OpenThread(2, false, (uint)threadId);
                    suspendedThreadHandles.Add(threadHandle);
                }
            }

            foreach (var handle in suspendedThreadHandles)
            {
                SuspendThread(handle);
            }

            timer.Enabled = true;

            Interlocked.Increment(ref methodCalls);
        }

        public static long Start([CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            if (currentCallId == long.MaxValue)
                throw new InvalidOperationException("Too may method calls were recorded for this experiment.");

            var snapshotCallId = currentCallId;
            var method = $"{callerFilePath}:{callerMember}:{currentCallId}"; //TODO: need to get unique identifier per caller :{callerLineNumber}
            Interlocked.Increment(ref currentCallId);
            var latency = new LatencyMeasurement();
            latency.Resume();

            if (!MethodLatencies.TryAdd(method, latency))
                throw new InvalidOperationException($"Could not start the latency measurement for method: [{method}]");

            return snapshotCallId;
        }

        public static void End(long callId, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            var augmentedMethodId = $"{callerFilePath}:{callerMember}:{callId}";//TODO: need to get unique identifier per caller :{callerLineNumber}

            if (!MethodLatencies.TryRemove(augmentedMethodId, out LatencyMeasurement latency))
                throw new ArgumentException($"Could not end latency measurement for method: [{augmentedMethodId}]");

            latency.Finish();
            ProcessedLatencies.Add((augmentedMethodId, latency));
        }

        public static void Resume(long callId, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            var augmentedMethodId = $"{callerFilePath}:{callerMember}:{callId}";//TODO: need to get unique identifier per caller :{callerLineNumber}

            if (!MethodLatencies.TryGetValue(augmentedMethodId, out LatencyMeasurement latency))
                throw new ArgumentException($"Could not resume latency measurement for method: [{augmentedMethodId}]");

            if(latency.Completed)
                throw new ArgumentException($"Could not resume a completed latency measurement for method: [{augmentedMethodId}]");

            latency.Resume();
        }

        public static void Pause(long callId, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            var augmentedMethodId = $"{callerFilePath}:{callerMember}:{callId}";//TODO: need to get unique identifier per caller :{callerLineNumber}

            if (!MethodLatencies.TryGetValue(augmentedMethodId, out LatencyMeasurement latency))
                throw new ArgumentException($"Could not pause latency measurement for method: [{augmentedMethodId}]");

            if (latency.Completed)
                throw new ArgumentException($"Could not pause a completed latency measurement for method: [{augmentedMethodId}]");

            latency.Pause();
        }
        
        private static void OnExited(object sender, EventArgs e)
        {
            var (throughputTags, throughputs) = CozMarker.GetThroughputSnapshot();
            var (latencyTags, latencies) = CozMarker.GetLatenciesSnapshot();
            var snapshot = new CozSnapshot
            {
                ThroughputTags = throughputTags,
                Throughputs = throughputs,
                LatencyTags = latencyTags,
                Latencies = latencies
            };
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
                MethodMeasurements = methodMeasurements,
                CozSnapshot = snapshot,
                Calls = methodCalls
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
