using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;

namespace OPS5.Engine.Models
{
    public class RuleFileModel
    {
        public List<RuleModel> Rules { get; set; } = new List<RuleModel>();
    }

    public class RuleModel
    {
        public string RuleName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string Comment { get; set; } = "";
        public List<ConditionModel> Conditions { get; set; } = new List<ConditionModel>();
        public List<ActionModel> Actions { get; set; } = new List<ActionModel>();
        public Dictionary<string, int> ConditionAliases { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public bool IsFindPath { get; set; }
        public IFindPathInfo FindPathInfo { get; set; } = default!;
        public ConditionModel PathCondition { get; set; } = default!;
    }

    public class ConditionModel : FileModelBase
    {
        public int Order { get; set; }
        public string ClassName { get; set; }
        public bool Negative { get; set; }
        public List<ConditionTest> Tests { get; set; } = new List<ConditionTest>();
        public bool IsAny { get; set; } = false;
        public bool IsFindPath { get; set; } = false;
        public string? Alias { get; set; }
        public ConditionModel(int order, string className, bool negative, string line, bool isFindPath) : base(line)
        {
            Order = order;
            ClassName = className;
            Negative = negative;
            IsFindPath = isFindPath;
        }

    }

    public class ActionModel : FileModelBase
    {
        public string ClassName { get; set; } = "";
        public string Command { get; set; }
        public List<object> Actions { get; set; } = new List<object>();
        public List<string> Atoms { get; set; } = new List<string>();
        //public string BindingName { get; set; }

        public ActionModel(string command, string line): base(line)
        {
            Command = command;
        }
    }
}
