using OPS5.Engine.Models;
using System.Collections.Generic;

namespace OPS5.Engine.Parsers.OPS5
{
    /// <summary>
    /// Result of parsing OPS5 source code directly into engine models.
    /// </summary>
    internal class OPS5ParseResult
    {
        public ClassFileModel Classes { get; set; } = new ClassFileModel();
        public DataFileModel Data { get; set; } = new DataFileModel();
        public RuleFileModel Rules { get; set; } = new RuleFileModel();
        public List<DefaultModel> Defaults { get; set; } = new();
        public List<VectorAttributeModel> VectorAttributes { get; set; } = new();
    }
}
