using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IOPS5Logger
    {
        void WriteError(string message, string procedure);
        void WriteInfo(string message, int importance);
        void WriteOutput(string message);
        string? ReadInput(string? prompt = null);
        string? ReadInputLine(string? prompt = null);
        int ErrorCount { get; set; }
        int Verbosity { get; set; }
        void SetVerbosity(int v);
        void ClearErrors();
    }

}
