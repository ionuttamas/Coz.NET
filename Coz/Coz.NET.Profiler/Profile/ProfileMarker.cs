using System;
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
        [DllImport("Kernel32", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        public static extern Int32 GetCurrentWin32ThreadId();

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32")]
        private static extern IntPtr CreateThread(IntPtr lpThreadAttributes, IntPtr dwStackSize, Delegate lpStartAddress, IntPtr lpParameter, int dwCreationFlags, out int lpThreadId);

        [DllImport("kernel32")]
        private static extern bool TerminateThread(IntPtr hThread, int dwExitCode);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint ResumeThread(IntPtr hThread);
        
        private static long currentCallId;
        private static readonly Experiment.Experiment Experiment;
        private static readonly ConcurrentDictionary<string, LatencyMeasurement> MethodLatencies;
        private static readonly ConcurrentBag<(string, LatencyMeasurement)> ProcessedLatencies;
        private static readonly ConcurrentQueue<(DateTime, List<IntPtr>)> SuspendedThreads; //TODO: Coarse DateTime granularity
        private static IntPtr ResumeHandle;
        private static int ResumeThreadId;
        private static long methodCalls;
        private static readonly IPCService IpcService;

        static ProfileMarker()
        {
            MethodLatencies = new ConcurrentDictionary<string, LatencyMeasurement>();
            ProcessedLatencies = new ConcurrentBag<(string, LatencyMeasurement)>();
            SuspendedThreads = new ConcurrentQueue<(DateTime, List<IntPtr>)>();
            methodCalls = 0;
            currentCallId = long.MinValue;
            IpcService = new IPCService();
            IpcService.Start();
            Experiment = IpcService.Receive<Experiment.Experiment>(); 
            CurrentDomain.ProcessExit += OnExited;

            Action resumeThreads = () =>
            {
                Console.WriteLine($"Inside resumeThreads");

                while (true)
                {
                    Console.WriteLine($"Heartbeat");

                    (DateTime, List<IntPtr>) tuple;

                    if (SuspendedThreads.Count > 0)
                    {
                        Console.WriteLine($"Before try dequeue {SuspendedThreads.Count}");
                    }
                    
                    while (!SuspendedThreads.TryDequeue(out tuple))
                    {
                        Console.WriteLine($"Tryin' to dequeue failed");
                        Thread.Sleep(2);
                    }

                    Console.WriteLine($"SuspendedThreads.TryDequeue worked");
                    var sleepTimespan = (int)(tuple.Item1 - DateTime.UtcNow).TotalMilliseconds;

                    if (sleepTimespan > 0)
                    {
                        Console.WriteLine($"Sleeping for {sleepTimespan}");
                        Thread.Sleep(sleepTimespan);
                    }

                    Console.WriteLine($"Found handles to resume");

                    foreach (IntPtr handle in tuple.Item2)
                    {
                        Console.WriteLine($"Resuming {handle}");
                        ResumeThread(handle);
                        //CloseHandle(handle);
                    }
                }
            };
            (ResumeHandle, ResumeThreadId) = StartNativeThread(resumeThreads);
            Console.WriteLine($"ResumeThreadsHandle {ResumeHandle}");
        }

        public static void Slowdown([CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            var methodId = $"{callerFilePath}:{callerMember}"; //:{callerLineNumber}

            if (methodId!=Experiment.MethodId)
                return;

            if(Experiment.IsBaseline)
                return;

            Console.WriteLine($"Inside {methodId} with experiment: {Experiment}");
            ProcessThreadCollection threads = Process.GetCurrentProcess().Threads;
            var currentThreadId = GetCurrentWin32ThreadId();
            var handles = new List<IntPtr>();

            lock (Experiment)
            {
                for (int i = 0; i < threads.Count; i++)
                {
                    ProcessThread processThread = threads[i];
                    var threadId = processThread.Id;

                    if (threadId == currentThreadId) 
                        continue;
                    
                    IntPtr threadHandle = OpenThread(2, true, (uint)threadId);

                    if (threadHandle == ResumeHandle || threadId == ResumeThreadId)
                    {
                        Console.WriteLine($"Ignored resume handle = {threadHandle} and threadId={threadId}");
                        continue;
                    }

                    handles.Add(threadHandle);
                    Console.WriteLine($"Added {threadId}");
                }

                Console.WriteLine($"Enqueuing into: SuspendedThreads.Count = {SuspendedThreads.Count}");
                SuspendedThreads.Enqueue((DateTime.UtcNow.AddMilliseconds(Experiment.MethodSlowdown), handles));
                Console.WriteLine($"Enqueued into: SuspendedThreads.Count = {SuspendedThreads.Count}");

                foreach (var handle in handles)
                {
                    Console.WriteLine($"Suspending {handle}");
                    SuspendThread(handle);
                }
            }

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

        private static (IntPtr, int) StartNativeThread(Delegate @delegate)
        {
            IntPtr handle = CreateThread(IntPtr.Zero, IntPtr.Zero, @delegate, IntPtr.Zero, 0, out var threadId);
            Console.WriteLine($"Started handle = {handle} and threadId={threadId}");

            return (handle, threadId);
        }

        private static void OnExited(object sender, EventArgs e)
        {
            TerminateThread(ResumeHandle, 0);

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
