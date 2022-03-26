using System;

namespace SampleApp
{
    public class Processor1
    {
        private const int PROCESSING_QUANTA = 1500000;

        public void Process()
        {
            Step1();
            Step2();
            Step3();
            Step4();
            Step5();
            Console.WriteLine("Completed: Processor1/Process");
        }

        private void Step1()
        {
            var sum = 0d;
            
            for (int i = 0; i < 2* PROCESSING_QUANTA; i++)
            {
                sum = (sum + Math.Pow(i, 1.1)) % 10;
            }

            Console.WriteLine("Completed: Processor1/Step1");
        }

        private void Step2()
        {
            var sum = 0d;

            for (int i = 0; i < 2 * PROCESSING_QUANTA; i++)
            {
                sum = (sum + Math.Pow(i, 1.1)) % 10;
            }

            Console.WriteLine("Completed: Processor1/Step2");
        }

        private void Step3()
        {
            var sum = 0d;

            for (int i = 0; i < 1.5 * PROCESSING_QUANTA; i++)
            {
                sum = (sum + Math.Pow(i, 1.1)) % 10;
            }

            Console.WriteLine("Completed: Processor1/Step3");
        }

        private void Step4()
        {
            var sum = 0d;

            for (int i = 0; i < 1 * PROCESSING_QUANTA; i++)
            {
                sum = (sum + Math.Pow(i, 1.1)) % 10;
            }

            Console.WriteLine("Completed: Processor1/Step4");
        }

        private void Step5()
        {
            var sum = 0d;

            for (int i = 0; i < 2 * PROCESSING_QUANTA; i++)
            {
                sum = (sum + Math.Pow(i, 1.1)) % 10;
            }

            Console.WriteLine("Completed: Processor1/Step5");
        }
    }
}