using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface IRuleFactory
    {
        IRule NewRule(string name);
    }
    public interface IRule
    {
        int ID { get; set; }
        string Name { get; set; }
        List<Condition> Conditions { get; set; }
        Dictionary<string, Binding> Bindings { get; set; }
        IBetaNode PNode { get; set; }
        int Specificity { get; set; }
        List<IRHSAction> RHS { get; }
        int ObjectCount { get; set; }
        bool PNodeHasTokens();
        List<IToken> PNodeTokens();
        void SetFile(string fileName);
        string RuleFile();
        void AddCondition(Condition condition);
        void AddAction(IRHSAction action);

        /// <summary>
        /// Number of times this rule has fired during the current or last run.
        /// </summary>
        int FireCount { get; set; }
    }
}
