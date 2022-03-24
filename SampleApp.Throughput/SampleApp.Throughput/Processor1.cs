using System.Threading;

namespace SampleApp.Throughput
{
    public class Processor1
    {
        public void Process()
        {
            Step1();
            Step2();
            Step3();
        }

        private void Step1()
        {
            Thread.Sleep(1000);
        }

        private void Step2()
        {
            Thread.Sleep(2000);
        }

        private void Step3()
        {
            Thread.Sleep(3000);
        }
    }
}