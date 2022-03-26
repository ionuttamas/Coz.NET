using System;

namespace SampleApp
{
    public class Processor2
    {
        private const int PROCESSING_QUANTA = 1500000;

        public void Process()
        {
            Step1();
            Step2();
            Console.WriteLine("Completed: Processor2/Process");
        }

        private void Step1()
        {
            var sum = 0d;

            for (int i = 0; i < 4 * PROCESSING_QUANTA; i++)
            {
                sum = (sum + Math.Pow(i, 1.1)) % 10;
            }

            Console.WriteLine("Completed: Processor2/Step1");
        }

        private void Step2()
        {
            var sum = 0d;

            for (int i = 0; i < 3 * PROCESSING_QUANTA; i++)
            {
                sum = (sum + Math.Pow(i, 1.1)) % 10;
            }

            Console.WriteLine("Completed: Processor2/Step2");
        }
    }
}