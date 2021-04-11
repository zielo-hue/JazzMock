using System;
using tfNET_sandbox.Examples;
using static Tensorflow.Binding;

namespace tfNET_sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            var helloExample = new HelloWorldExample();
            helloExample.Run();
        }
    }
}