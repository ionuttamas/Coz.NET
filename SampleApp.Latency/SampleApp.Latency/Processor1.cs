using System;
using System.Threading;

namespace SampleApp
{
    public class Processor1
    {
        public void Process()
        {
            Step1();
            Step2();
            Step3();
            Console.WriteLine("Completed: Processor1/Process");
        }

        private void Step1()
        {
            Thread.Sleep(100);
            Console.WriteLine("Completed: Processor1/Step1");
        }

        private void Step2()
        {
            Thread.Sleep(200);
            Console.WriteLine("Completed: Processor1/Step2");
        }

        private void Step3()
        {
            Thread.Sleep(300);
            Console.WriteLine("Completed: Processor1/Step3");
        }
    }
}