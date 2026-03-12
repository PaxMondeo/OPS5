using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;

namespace OPS5.Engine
{
    internal class SourceFiles : ISourceFiles
    {
        public Dictionary<string, SourceFile> RuleFiles { get; set; } = new Dictionary<string, SourceFile>(StringComparer.OrdinalIgnoreCase);

        public SourceFile ProjectFile { get; set; } = default!;
        public SourceFile OPS5File { get; set; } = default!;
    }
}
