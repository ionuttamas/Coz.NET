using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading;
using Coz.NET.Profiler.Utils;

namespace Coz.NET.Profiler.Experiment
{
    public static class Experiment
    {
        private static readonly MethodSlowdown MethodSlowdown;

        static Experiment()
        { 
            MethodSlowdown = new MethodSlowdown();
            var ipcService = new IPCService();
            ipcService.Open();
            MethodSlowdown = ipcService.Receive<MethodSlowdown>();
        }

        public static void Slowdown([CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMember = "", [CallerLineNumber] long callerLineNumber = 0)
        {
            if (callerFilePath==MethodSlowdown.FilePath && callerMember == MethodSlowdown.MethodName && callerLineNumber == MethodSlowdown.LineNumber)
            {
                Thread.Sleep(MethodSlowdown.Slowdown);    
            }
        }
    }
}