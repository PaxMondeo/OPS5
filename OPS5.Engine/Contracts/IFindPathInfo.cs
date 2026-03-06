using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IFindPathInfo
    {
        string StartVar { get; set; }
        string EndVar { get; set; }
        string EdgeClass { get; set; }
        string FromAttr { get; set; }
        string FromVar { get; set; }
        string ToAttr { get; set; }
        string ToVar { get; set; }
        string DistAttr { get; set; }
        string DistVar { get; set; }
        int FirstObject { get; set; }
        Dictionary<string, string> VariableBindings { get; set; }
        List<string> FromVals { get; set; }
        List<string> ToVals { get; set; }
        Condition PathCondition { get; set; }

        IFindPathInfo Clone();
    }
}
