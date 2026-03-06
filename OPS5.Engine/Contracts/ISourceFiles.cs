using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface ISourceFiles
    {
        Dictionary<string, SourceFile> ClassFiles { get; set; }
        Dictionary<string, SourceFile> RuleFiles { get; set; }
        SourceFile BindingFile { get; set; }
        SourceFile DataFile { get; set; }
        SourceFile ProjectFile { get; set; }
        SourceFile OPS5File { get; set; }

    }
}
