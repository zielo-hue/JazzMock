using System;
using System.Text;
using Tensorflow;
using static Tensorflow.Binding;

namespace tfNET_sandbox.Examples
{
    public class HelloWorldExample : Example, IExample
    {
        public ExampleConfig InitConfig()
            => Config = new ExampleConfig()
            {
                Enabled = true,
                Name = "Hello World",
                IsImportingGraph = false,
                Priority = 1
            };

        public bool Run()
        {
            var str = "Hello TensorFlow dot net";
            var hello = tf.constant(str);

            using var sess = tf.Session();
            var result = sess.run(hello);
            var output = UTF8Encoding.UTF8.GetString((byte[]) result);
            Console.WriteLine(output);
            return output.Equals(str);
        }
    }
}