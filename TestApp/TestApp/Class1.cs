using System;

namespace TestApp
{
    class Class1
    {
        public void Foo()
        {
            Console.WriteLine("Foo was called");
        }

        public int Faz()
        {
            Console.WriteLine("Faz was called");

            if (1 == 2)
                return 1;

            if (3 + 3 == 2)
            {
                return 5;
            }

            if (3 + 3 == 2)
            {
                if (5 == 6)
                {
                    return 5;
                }
                else
                {
                    return 7;
                }
            }

            return 1;
        }
    }
}