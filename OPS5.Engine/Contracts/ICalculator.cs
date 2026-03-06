using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface ICalculator
    {
        string Calc(List<string> commands);
        bool ValidCommand(string cmd);
        string CalcType();
        string DoCalc(string commands, IToken thisToken);
        string DoCalc(List<string> commands, IToken thisToken);
    }
}
