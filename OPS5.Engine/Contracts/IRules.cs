using OPS5.Engine.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IRules
    {
        void Reset();
        IRule AddRule(string name);
        IRule AddRule(string name, string prodFile);
        IRule AddRule(RuleModel ruleModel);

        List<IRule> GetRules();
        List<IRule> GetRulesWithTokens();
        void PrintRules(bool full, bool all);
        void PrintConflictSet();
    }
}
