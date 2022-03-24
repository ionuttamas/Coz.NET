using System;
using System.Threading;
using Coz.NET.Profiler.Marker;

namespace SampleApp.Latency
{
    class Program
    {
        static void Main(string[] args)
        {
            var processor1 = new Processor1();
            var processor2 = new Processor2();

            CozMarker.StartLatency("MainProcess");

            var thread1 = new Thread(processor1.Process); 
            var thread2 = new Thread(processor2.Process); 

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

            Console.WriteLine("Final processing");
            Thread.Sleep(1000);

            CozMarker.EndLatency("MainProcess");
        }
    }
}
