using System.Collections.Generic;
using OPS5.Engine.Contracts;

namespace OPS5.Engine.Models
{
    internal class FindPathInfo : IFindPathInfo
    {
        public string StartVar { get; set; } = "";
        public string EndVar { get; set; } = "";
        public string EdgeClass { get; set; } = "";
        public string FromAttr { get; set; } = "";
        public string FromVar { get; set; } = "";
        public string ToAttr { get; set; } = "";
        public string ToVar { get; set; } = "";
        public string DistAttr { get; set; } = "";
        public string DistVar { get; set; } = "";
        public int FirstObject { get; set; }
        public Dictionary<string, string> VariableBindings { get; set; } = new();
        public List<string> FromVals { get; set; } = new();
        public List<string> ToVals { get; set; } = new();
        public Condition PathCondition { get; set; } = default!;

        public IFindPathInfo Clone()
        {
            return new FindPathInfo
            {
                StartVar = StartVar,
                EndVar = EndVar,
                EdgeClass = EdgeClass,
                FromAttr = FromAttr,
                FromVar = FromVar,
                ToAttr = ToAttr,
                ToVar = ToVar,
                DistAttr = DistAttr,
                DistVar = DistVar,
                FirstObject = FirstObject,
                VariableBindings = new Dictionary<string, string>(VariableBindings),
                FromVals = new List<string>(FromVals),
                ToVals = new List<string>(ToVals),
                PathCondition = PathCondition
            };
        }
    }
}
