using OPS5.Engine.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OPS5.Engine.Contracts
{
    internal interface IOPS5Console
    {
        Task<ConsoleResult> RunConsole();
        void WriteDots(string ruleName);
    }
}
