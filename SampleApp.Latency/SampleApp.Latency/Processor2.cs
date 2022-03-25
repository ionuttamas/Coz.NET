using System;
using System.Threading;

namespace SampleApp
{
    public class Processor2
    {
        public void Process()
        {
            Step1();
            Step2();
            Console.WriteLine("Completed: Processor2/Process");
        }

        private void Step1()
        {
            Thread.Sleep(400);
            Console.WriteLine("Completed: Processor2/Step1");
        }

        private void Step2()
        {
            Thread.Sleep(100);
            Console.WriteLine("Completed: Processor2/Step2");
        }
    }
}