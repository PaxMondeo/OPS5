using System;
using System.Collections.Generic;
using System.Linq;

using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    internal class RuleFactory : IRuleFactory
    {
        IServiceProvider _serviceProvider;
        IObjectIDs _objectIDs;

        public RuleFactory(IServiceProvider serviceProvider, IObjectIDs objectIDs)
        {
            _serviceProvider = serviceProvider;
            _objectIDs = objectIDs;
        }

        public IRule NewRule(string name)
        {
            var r = _serviceProvider.GetService(typeof(IRule));
            if (r == null)
                throw new Exception("Unable to instantiate new Rule");
            IRule rule = (IRule)r;

            rule.Name = name;
            rule.ID = _objectIDs.NextRuleID();
            return rule;
        }
    }
    /// <summary>
    /// Represents a Rule
    /// </summary>
    internal class Rule : IRule
    {
        private IOPS5Logger _logger;

        private string _ruleFile = string.Empty;
        /// <summary>
        /// Unique Integer ID for the Rule
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Name of the Rule
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// List of LHS Conditions
        /// </summary>
        public List<Condition> Conditions { get; set; } = new List<Condition>();

        /// <summary>
        /// Dictionary of variable Bindings. String is Variable, bound to Attribute in condition #
        /// </summary>
        public Dictionary<string, Binding> Bindings { get; set; } = new Dictionary<string, Binding>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The Beta Node that is this Rule's "P" Node, any Objects contained in this node are part of the Conflict Set
        /// </summary>
        public IBetaNode PNode { get; set; } = default!;

        /// <summary>
        /// The Specificity of this Rule. Used in conflict resolution.
        /// </summary>
        public int Specificity { get; set; } = 0;

        /// <summary>
        /// List of RHS Actions
        /// </summary>
        public List<IRHSAction> RHS { get; private set; } = new List<IRHSAction>();

        /// <summary>
        /// Author's comment describing the Rule
        /// </summary>
        public string Comment { get; set; } = "";

        /// <summary>
        /// Indicates that the Rule is Enabled for use
        /// </summary>
        public bool Enabled { get; set; } = true;

        public int ObjectCount { get; set; } = 0; //used to track the position of each Object in the Token, as it is added

        public int FireCount { get; set; }

        public Rule(IOPS5Logger logger)
        {
            _logger = logger;
        }


        public bool PNodeHasTokens()
        {
            return PNode != null && PNode.HasTokens();
        }

        public List<IToken> PNodeTokens()
        {
            if (PNode == null) return new List<IToken>();
            return PNode.Tokens.Values.ToList();
        }

        public void SetFile(string fileName)
        {
            _ruleFile = fileName.ToUpper();
        }

        public string RuleFile()
        {
            return _ruleFile;
        }

        public void AddCondition(Condition condition)
        {
            Conditions.Add(condition);
            Specificity += condition.Tests.Count();
        }

        internal void AddBinding(string attribute, Binding binding)
        {
            Bindings.Add(attribute.ToUpper(), binding);
        }

        public void AddAction(IRHSAction action)
        {
            RHS.Add(action);
        }

        internal static bool Variable(string rule, string passed, out string var)
        {
            if (passed.StartsWith("<") && passed.EndsWith(">"))
            {
                var = "<" + passed.Substring(1, passed.Length - 2) + ">";
                var = var.ToUpper();
                return true;
            }
            else
            {
                var = "";
                return false;
            }
        }


    }
}
