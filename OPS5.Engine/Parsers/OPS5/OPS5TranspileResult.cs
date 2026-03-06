using System.Collections.Generic;

namespace OPS5.Engine.Parsers.OPS5
{
    /// <summary>
    /// Result of transpiling an OPS5 file into OPS5 engine text-format strings.
    /// Each property contains generated syntax that can be fed to
    /// the existing internal parsers.
    /// </summary>
    public class OPS5TranspileResult
    {
        /// <summary>Generated .iocc class definition text.</summary>
        public string ClassesText { get; set; } = "";

        /// <summary>Generated .iocd data initialization text.</summary>
        public string DataText { get; set; } = "";

        /// <summary>Generated .iocr rule definition text.</summary>
        public string RulesText { get; set; } = "";

        /// <summary>Diagnostic warnings/errors encountered during transpilation.</summary>
        public List<string> Diagnostics { get; set; } = new List<string>();
    }
}
