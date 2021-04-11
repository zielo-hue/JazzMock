using System;
using Tensorflow;

namespace tfNET_sandbox.Examples
{
    public class Example
    {
        public ExampleConfig Config { get; set; }

        public virtual Graph BuildGraph()
        {
            throw new NotImplementedException();
        }
        
        public virtual Graph ImportGraph()
        {
            throw new NotImplementedException();
        }

        public virtual void PrepareData()
        {
            throw new NotImplementedException();
        }

        public virtual void Train()
        {
            
        }

        public virtual void Test()
        {

        }

        public virtual void Predict()
        {
        }

        public virtual string FreezeModel()
        {
            throw new NotImplementedException();
        }
    }
}