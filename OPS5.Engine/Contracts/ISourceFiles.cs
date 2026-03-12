using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface ISourceFiles
    {
        Dictionary<string, SourceFile> RuleFiles { get; set; }
        SourceFile ProjectFile { get; set; }
        SourceFile OPS5File { get; set; }

    }
}
