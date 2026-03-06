using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine
{
    internal class SourceFiles : ISourceFiles
    {
        /// <summary>
        /// Dictionary of Class files loaded by the .ioc Program file
        /// </summary>
        public Dictionary<string, SourceFile> ClassFiles { get; set; } = new Dictionary<string, SourceFile>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Dictionary of Rule files loaded by the .ioc Program file
        /// </summary>
        public Dictionary<string, SourceFile> RuleFiles { get; set; } = new Dictionary<string, SourceFile>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// The file containing Bindings loaded by the .ioc Program file
        /// </summary>
        public SourceFile BindingFile { get; set; } = default!;

        public SourceFile ProjectFile { get; set; } = default!;
        public SourceFile DataFile { get; set; } = default!;
        public SourceFile OPS5File { get; set; } = default!;
    }
}
