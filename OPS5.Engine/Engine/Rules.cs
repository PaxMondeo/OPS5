using OPS5.Engine.Contracts;
using OPS5.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OPS5.Engine
{
    internal class Rules : IRules
    {
        /// <summary>
        /// Dictionary of Rules that have been loaded
        /// </summary>
        private Dictionary<int, IRule> _rules { get; set; } = new Dictionary<int, IRule>();
        private IOPS5Logger _logger;
        private IWorkingMemory _workingMemory;
        private IRuleFactory _ruleFactory;
        private ISourceFiles _sourceFiles;

        private int _rulesCount = 0;

        public Rules(IOPS5Logger logger, IWorkingMemory workingMemory, IRuleFactory ruleFactory, ISourceFiles sourceFiles)
        {
            _logger = logger;
            _workingMemory = workingMemory;
            _ruleFactory = ruleFactory;
            _sourceFiles = sourceFiles;

            Reset();
        }

        public void Reset()
        {
            _rules = new Dictionary<int, IRule>();
            _rulesCount++;
        }

        public IRule AddRule(string name)
        {
            IRule rule = _ruleFactory.NewRule(name);
            _rules.Add(rule.ID, rule);
            return rule;
        }

        /// <summary>
        /// Add a Rule to RETE
        /// </summary>
        /// <param name="name">Name of Rule</param>
        /// <returns>Rule</returns>
        public IRule AddRule(string name, string prodFile)
        {
            IRule rule = AddRule(name);
            rule.SetFile(prodFile);
            return rule;
        }

        /// <summary>
        /// Add a Rule to RETE
        /// </summary>
        /// <param name="name">Name of Rule</param>
        /// <param name="enabled">Indicates that the Rule is Enabled for use or Disabled</param>
        /// <returns>Rule</returns>
        public IRule AddRule(RuleModel ruleModel)
        {
            string fu = ruleModel.FileName.ToUpper();
            if (!_sourceFiles.RuleFiles.ContainsKey(fu))
                _sourceFiles.RuleFiles.Add(fu, new SourceFile(ruleModel.FileName, _sourceFiles.ProjectFile.FilePath, "", "", true, false));
            else
                _sourceFiles.RuleFiles[fu].Loaded = true;

            IRule rule = AddRule(ruleModel.RuleName, ruleModel.FileName);
            rule.Enabled = ruleModel.Enabled;
            rule.IsFindPath = ruleModel.IsFindPath;
            return rule;
        }


        public List<IRule> GetRules()
        {
            return _rules.Values.ToList();
        }

        public List<IRule> GetEnabledRulesWithTokens()
        {
            return _rules.Values.Where(p => p.Enabled && p.PNodeHasTokens()).ToList();
        }

        public void PrintRules(bool full, bool all)
        {
            Console.WriteLine("\n\nRules:");
            foreach (Rule p in _rules.Values.Where(p => p.Enabled))
            {
                if (p.PNode == null) continue;
                if(all || p.PNode.Tokens.Count > 0)
                {
                    if (full)
                    {
                        Console.WriteLine($"Rule {p.Name}: on Beta Node {p.PNode.ID} with specificity {p.Specificity} has ");
                        foreach (var token in p.PNode.Tokens)
                        {
                            Console.WriteLine("Token containing :");
                            foreach (var objectId in token.Value.ObjectIDs)
                            {
                                var iObject = _workingMemory.GetWME(objectId);
                                Console.WriteLine($"\t{iObject.ClassName}");
                                foreach (KeyValuePair<string, string?> attr in iObject.GetAttributes())
                                {
                                    Console.WriteLine($"\t\t{attr.Key}\t\t{attr.Value}");
                                }
                            }
                        }
                    }
                    else
                        Console.WriteLine($"Rule {p.Name}: on Beta Node {p.PNode.ID} with specificity {p.Specificity} has {p.PNode.Tokens.Count} tokens");
                }
            }
            if (!full)
                Console.WriteLine("To see details of pending rules, enter Rules FULL");
            if (!all)
                Console.WriteLine("To see all rules, enter Rules ALL");
        }

        public void PrintConflictSet()
        {
            try
            {
                foreach (Rule p in _rules.Values.Where(p => p.Enabled == true))
                {
                    if (p.PNode == null) continue;
                    if (p.PNode.Tokens.Count() > 0)
                    {
                        Console.WriteLine($"Rule {p.Name} with Specificity {p.Specificity} has:");
                        foreach (Token token in p.PNode.Tokens.Values)
                        {
                            string message = $"Token with recency {token.GetRecency()} and objects ";
                            foreach (int objectID in token.ObjectIDs)
                            {
                                if (_workingMemory.WMEExists(objectID))
                                {
                                    IWMElement iObject = _workingMemory.GetWME(objectID);
                                    message += $"{iObject.ClassName} ID: {iObject.AttributeValue("ID")} Timetag {iObject.TimeTag}, ";
                                }
                                else
                                    Console.WriteLine($"ERROR - could not find Object {objectID} referred to by Token {token.ID} in PNode {p.PNode.ID}, does not exist in Working Memory");
                            }
                            Console.WriteLine($"{message}Specificity {p.Specificity}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void DisableRules(string className)
        {
            foreach (IRule rule in _rules.Values)
            {
                foreach (Condition cond in rule.Conditions)
                {
                    if (cond.ClassName == className)
                    {
                        rule.Enabled = false;
                        _logger.WriteInfo($"Rule {rule.Name} refers to class {className} and has been disabled. Please review the rule before re-enabling it.", 0);
                    }
                }
            }
        }

    }
}
