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
        }

        private void Step1()
        {
            Thread.Sleep(100);
        }

        private void Step2()
        {
            Thread.Sleep(200);
        }

        private void Step3()
        {
            Thread.Sleep(300);
        }
    }
}