using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IExecuteBinding
    {
        string Name { get; set; }
        string WorkingFolder { get; set; }
        string FileName { get; set; }
        bool IsExe { get; set; }

        void SetProperties(string name, string workingFolder, string programFile, bool isExe);
        void Execute(string arguments);

    }
}

