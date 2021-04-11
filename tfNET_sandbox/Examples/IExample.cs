using Tensorflow;

namespace tfNET_sandbox.Examples
{
    public interface IExample
    {
        ExampleConfig Config { get; set; }
        ExampleConfig InitConfig();
        bool Run();

        void Train();
        string FreezeModel();
        void Test();

        void Predict();

        Graph ImportGraph();
        Graph BuildGraph();

        void PrepareData();
    }
}