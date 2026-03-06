using OPS5.Engine.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OPS5.Engine
{
    internal class BetaMemory : IBetaMemory
    {
        private readonly IOPS5Logger _logger;
        /// <summary>
        /// Dictionary of Beta nodes currently in existence
        /// </summary>
        private ConcurrentDictionary<int, IBetaNode> _betaMemory { get; set; } = new ConcurrentDictionary<int, IBetaNode>();
        private int _nextBetaId;

        private IWorkingMemory _workingMemory;
        private IBetaNodeFactory _betaNodeFactory;
        private ITokenFactory _tokenFactory;


        public BetaMemory(IBetaNodeFactory betaNodeFactory,
                          IWorkingMemory workingMemory,
                          ITokenFactory tokenFactory,
                          IOPS5Logger logger)
        {
            _betaNodeFactory = betaNodeFactory;
            _workingMemory = workingMemory;
            _tokenFactory = tokenFactory;
            _logger = logger;
            Reset();
        }

        public IBetaNode Reset()
        {
            _betaMemory = new ConcurrentDictionary<int, IBetaNode>();
            _nextBetaId = 0;
            IBetaNode betaRoot = _betaNodeFactory.NewBetaNode();
            betaRoot.ID = Interlocked.Increment(ref _nextBetaId);
            _betaMemory.TryAdd(betaRoot.ID, betaRoot);
            IToken newToken = _tokenFactory.NewToken(1);
            betaRoot.Tokens.TryAdd(newToken.ID, newToken);

            return betaRoot;
        }

        public IBetaNode AddBetaNode()
        {
            IBetaNode betaNode = _betaNodeFactory.NewBetaNode();

            betaNode.ID = Interlocked.Increment(ref _nextBetaId);
            _betaMemory.TryAdd(betaNode.ID, betaNode);
            return betaNode;
        }

        /// <summary>
        /// Searches for a matching Beta node. If found, returns it, else creates a new node and returns that
        /// </summary>
        /// <param name="betaParent"></param>
        /// <param name="alphaParent"></param>
        /// <param name="newTests"></param>
        /// <param name="ruleBindings"></param>
        /// <param name="negative"></param>
        /// <param name="isFindPath"></param>
        /// <param name="findPath"></param>
        /// <returns></returns>
        public IBetaNode BuildShareBeta(IBetaNode betaParent, IAlphaNode alphaParent, List<ConditionTest> newTests, Dictionary<string, Binding> ruleBindings, bool negative, bool isAny, bool isFindPath, IFindPathInfo? findPath)
        {
            IBetaNode betaNode = default!;
            Dictionary<string, Binding> newBindings = new Dictionary<string, Binding>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, Binding> binding in ruleBindings)
                newBindings.Add(binding.Key, binding.Value);

            foreach (IBetaNode child in betaParent.BetaChildren)
            {
                if (child.AlphaParent.ID == alphaParent.ID && child.Tests.Count == newTests.Count && child.Negative == negative && child.IsFindPath == isFindPath)
                {
                    bool share = true;
                    for (int x = 0; x < newTests.Count; x++)
                    {
                        if (newTests[x] != child.Tests[x])
                            share = false;
                    }
                    foreach (KeyValuePair<string, Binding> bind1 in newBindings)
                    {
                        bool found = false;
                        foreach (KeyValuePair<string, Binding> bind2 in child.Bindings)
                            if (bind1.Key == bind2.Key && bind1.Value == bind2.Value)
                            {
                                found = true;
                                break;
                            }
                        if (!found)
                            share = false;
                    }
                    if (share)
                    {
                        betaNode = child;
                        string neg = "";
                        if (negative)
                        {
                            neg = "Negative ";
                        }
                        betaNode.AddBindings(newBindings);
                        _logger.WriteInfo($"Shared {neg}Beta node {betaNode.ID}", 2);
                    }
                }
            }
            if (betaNode == null)
            {
                betaNode = AddBetaNode();
                betaNode.SetProperties(betaParent, alphaParent, newTests, newBindings, negative, isAny, isFindPath, findPath);
                if (_logger.Verbosity > 1)
                {
                    if (negative)
                    {
                        _logger.WriteInfo($"Created Negative Beta node {betaNode.ID}", 2);
                    }
                    else if (isFindPath)
                    {
                        _logger.WriteInfo($"Created FindPath Beta node {betaNode.ID}", 2);
                    }
                    else
                    {
                        _logger.WriteInfo($"Created Beta node {betaNode.ID}", 2);
                    }
                }
            }
            return betaNode;
        }




        public IBetaNode GetBetaNode(int id)
        {
            return _betaMemory[id];
        }

        public void PrintBetaMemory()
        {
            string message;
            Console.WriteLine("\nBeta Memory\nID        Negative  Any  Alpha Parent   Beta Parent    Children       Tests Tokens");
            foreach (KeyValuePair<int, IBetaNode> pr in _betaMemory)
            {
                string neg = "";
                string any = "";
                int key = pr.Key;
                IBetaNode node = pr.Value;
                if (node.Negative)
                {
                    neg = "True ";
                }
                if (node.IsAny)
                {
                    any = "True ";
                }
                if (node.BetaParent == null)
                {
                    message = $"{key,-3}(Root)                -              -              ";
                }
                else
                {
                    message = $"{key,-10}{neg,-10}{any,-5}{node.AlphaParent.ID,-15}{node.BetaParent.ID,-15}";
                }
                string childs = "";
                foreach (BetaNode c in node.BetaChildren)
                {
                    childs += $"{c.ID} ";
                }
                Console.WriteLine($"{message}{childs,-15}{node.Tests.Count(),-6}{node.Tokens.Count()} ");
            }
        }

        public void ExamineBeta(int nodeID)
        {
            if (nodeID > _betaMemory.Count)
            {
                Console.WriteLine($"Only {_betaMemory.Count} Beta Nodes");
            }
            else
            {
                IBetaNode thisNode = _betaMemory[nodeID];
                string neg = "";
                string n = "";
                if (thisNode.Negative)
                {
                    neg = "NEGATIVE ";
                    n = "--";
                }
                Console.WriteLine($"\n{neg}Beta node {nodeID} has {thisNode.Tests.Count()} Tests and {thisNode.Tokens.Count()} Tokens");

                if (thisNode.AlphaParent == null)
                    Console.WriteLine("Root Beta Node");
                else
                {
                    Console.WriteLine($"Alpha Parent:\t{thisNode.AlphaParent.ID}");
                    Console.WriteLine($"Beta Parent:\t{thisNode.BetaParent.ID}");
                }

                Console.Write("\nChildren:\t");
                if (thisNode.BetaChildren.Count == 0)
                    Console.WriteLine("None");
                else
                {
                    foreach (BetaNode child in thisNode.BetaChildren)
                    {
                        Console.Write($"{child.ID} ");
                    }
                    Console.WriteLine();
                }

                if(thisNode.Tests.Count == 0)
                    Console.WriteLine("Conditions:\tNone");
                else
                {
                    Console.WriteLine($"Conditions:\t{thisNode.Tests.Count}");
                    foreach (ConditionTest test in thisNode.Tests)
                    {
                        Console.WriteLine($"\t{n}Test  {test.Attribute} {test.Operator} {test.Value}");
                    }
                }

                if (thisNode.Bindings.Count == 0)
                    Console.WriteLine("Bindings:\tNone");
                else
                {
                    Console.WriteLine("Bindings:\n\tIndex Variable Name                 Object                        Attribute");
                    foreach (KeyValuePair<string, Binding> binding in thisNode.Bindings)
                    {
                        Console.WriteLine($"\t{binding.Value.ObjectIndex,-6}{binding.Key,-30}{GetClassTypeFromTokenIndex(thisNode, binding.Value.ObjectIndex),-30}{binding.Value.Attribute}");
                    }
                }
                foreach (int token in thisNode.Tokens.Keys)
                {
                    string message = "";
                    Console.WriteLine($"Token with recency {thisNode.Tokens[token].GetRecency()} containing Objects: ");
                    foreach (int objectID in thisNode.Tokens[token].ObjectIDs)
                    {
                        if (_workingMemory.WMEExists(objectID))
                        {
                            message = $"\t\tObject of Class {_workingMemory.GetWME(objectID).ClassName}";
                            foreach (KeyValuePair<string, string?> attr in _workingMemory.GetWME(objectID).GetAttributes())
                            {
                                if (attr.Value is string val)
                                {
                                    if (val.Length > 500)
                                        val = val.Substring(0, 500);
                                    message += $" {attr.Key} {val} ";
                                }
                            }
                            Console.WriteLine(message);
                        }
                        else
                            Console.WriteLine($"Working memory does not contain Object {objectID} referred to in Token {token} in Beta Node {thisNode.ID}");
                    }
                    message = "Bound Value(s): ";
                    foreach (KeyValuePair<string, string> variable in thisNode.Tokens[token].Variables)
                    {
                        string val = variable.Value;
                        if (val.Length > 500)
                            val = val.Substring(0, 500);
                        message += $"{variable.Key} {val} ";
                    }
                    Console.WriteLine(message);
                    if (thisNode.Tokens[token].Fired)
                    {
                        Console.WriteLine("Token has been fired!");
                    }
                }
            }
        }

        private string GetClassTypeFromTokenIndex(IBetaNode node, int index)
        {
            string response = "";

            if (node.TokenCount() > 0)
            {
                IToken token = node.Tokens.First().Value;
                if (token.ObjectCount() >= index)
                    response = _workingMemory.GetWME(token.ObjectIDs[index - 1]).ClassName;
            }

            return response;
        }

    }
}
