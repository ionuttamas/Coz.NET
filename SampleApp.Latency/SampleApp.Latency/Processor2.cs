using System.Threading;

namespace SampleApp
{
    public class Processor2
    {
        public void Process()
        {
            Step1();
            Step2(); 
        }

        private void Step1()
        {
            Thread.Sleep(4000);
        }

        private void Step2()
        {
            Thread.Sleep(1000);
        }
    }
}