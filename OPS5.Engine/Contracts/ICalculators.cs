using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface ICalculators
    {
        void SetDefault(string calcType);
        ICalculator Default();
        ICalculator GetCalculator(string calcType);
    }
}
