using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface ICheckFactory
    {
        ICheck GetCheck(string check);
    }
    internal interface ICheck
    {
        void SetProperties(string check);
        bool Evaluate(string val, IToken token);

    }
}
