using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OPS5.Engine
{
    internal class AlphaMemory : IAlphaMemory
    {
        private IOPS5Logger _logger;
        private IAlphaNodeFactory _alphaNodeFactory;
        private IWorkingMemory _workingMemory;
        private int _nextAlphaId;

        /// <summary>
        /// Dictionary of Alpha nodes currently in existence
        /// </summary>
        private Dictionary<int, IAlphaNode> _alphaMemory { get; set; } = new Dictionary<int, IAlphaNode>();
        /// <summary>
        /// Dictionary with hash of Alpha nodes for fast lookup
        /// </summary>
        private Dictionary<string, IAlphaNode> _alphaHash { get; set; } = new Dictionary<string, IAlphaNode>();

        public AlphaMemory(IOPS5Logger logger, IAlphaNodeFactory alphaNodeFactory, IWorkingMemory workingMemory)
        {
            _logger = logger;
            _alphaNodeFactory = alphaNodeFactory;
            _workingMemory = workingMemory;
            Reset();
        }

        public IAlphaNode Reset()
        {
            _alphaMemory = new Dictionary<int, IAlphaNode>();
            _alphaHash = new Dictionary<string, IAlphaNode>(StringComparer.OrdinalIgnoreCase);
            _nextAlphaId = 0;
            IAlphaNode alphaRoot = _alphaNodeFactory.AlphaRoot();
            alphaRoot.ID = ++_nextAlphaId;
            string hash = "Root";
            _alphaMemory.TryAdd(alphaRoot.ID, alphaRoot);
            _alphaHash.TryAdd(hash, alphaRoot);
            return alphaRoot;
        }

        /// <summary>
        /// Searches for a matching Alpha node. If found returns it, else creates a new node and returns that
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="attribute"></param>
        /// <param name="op"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IAlphaNode BuildShareAlpha(IAlphaNode parent, ConditionTest test)
        {
            //An Alpha Node represents a condition, and contains a list of Objects that match it
            //A constant condition is an attribute/value pair
            //A variable condition is an attribute/variable pair
            IAlphaNode alphaNode;
            string parentID = parent.ID.ToString();

            string hash = parentID + test.Attribute + test.Operator + test.Value;

            if (_alphaHash.ContainsKey(hash))
            {
                alphaNode = _alphaHash[hash];
                _logger.WriteInfo($"Shared Alpha node {alphaNode.ID}\t{alphaNode.Attribute} {alphaNode.Op} {alphaNode.Value}", 2);
            }
            else
            {
                alphaNode = AddAlphaNode(parent, test, new List<string>());
                _logger.WriteInfo($"Created Alpha node {alphaNode.ID}\t{alphaNode.Attribute} {alphaNode.Op} {alphaNode.Value}", 2);
            }
            return alphaNode;
        }

        private IAlphaNode AddAlphaNode(IAlphaNode parent, ConditionTest test, List<string> distinctAttributes)
        {
            IAlphaNode alphaNode = _alphaNodeFactory.NewAlphaNode(parent, test, new List<string>());

            alphaNode.ID = ++_nextAlphaId;
            string hash = alphaNode.Parent.ID.ToString() + alphaNode.Attribute + alphaNode.Op + alphaNode.Value;
            _alphaMemory.TryAdd(alphaNode.ID, alphaNode);
            _alphaHash.TryAdd(hash, alphaNode);
            return alphaNode;
        }

        public void PrintAlphaMemory()
        {
            int maxAttr = 0;
            foreach (int key in _alphaMemory.Keys)
            {
                AlphaNode node = (AlphaNode)_alphaMemory[key];
                if (node.Attribute is string attr && attr.Length > maxAttr)
                    maxAttr = node.Attribute.Length;
            }
            maxAttr++;
            if (maxAttr < 10)
                Console.WriteLine("Alpha Memory\nNode      Condition                                 Parent    Alpha Children Beta Children  Objects");
            else if (maxAttr < 20)
                Console.WriteLine("Alpha Memory\nNode      Condition                                           Parent    Alpha Children Beta Children  Objects");
            else
                Console.WriteLine("Alpha Memory\nNode      Condition                                                     Parent    Alpha Children Beta Children  Objects");
            foreach (int key in _alphaMemory.Keys)
            {
                AlphaNode node = (AlphaNode)_alphaMemory[key];
                string message = "";
                if(maxAttr < 10)
                   message = $"{key,-10}{node.Attribute,-10}{node.Op,-2}{node.Value,-30}";
                else if (maxAttr < 20)
                    message = $"{key,-10}{node.Attribute,-20}{node.Op,-2}{node.Value,-30}";
                else
                    message = $"{key,-10}{node.Attribute,-30}{node.Op,-2}{node.Value,-30}";

                if (node.Parent == null)
                {
                    message += $"-         ";
                }
                else
                {
                    message += $"{node.Parent.ID,-10}";
                }

                if (node.AlphaChildren.Count == 0)
                    message += "-              ";
                else
                {
                    string ac = "";
                    foreach (AlphaNode c in node.AlphaChildren)
                    {
                        ac += $"{c.ID} ";
                    }
                    message += $"{ac,-15}";
                }

                if (node.BetaChildren.Count == 0)
                    message += "-              ";
                else
                {
                    string bc = "";
                    foreach (BetaNode c in node.BetaChildren)
                    {
                        bc += $"{c.ID} ";
                    }
                    message += $"{bc,-15}";
                }
                message += $"{node.ObjectCount()}";
                Console.WriteLine(message);
            }
        }

        public void ExamineAlpha(int nodeID)
        {
            if (nodeID > _alphaMemory.Count)
            {
                Console.WriteLine($"Only {_alphaMemory.Count} Alpha nodes");
            }
            else
            {
                IAlphaNode thisNode = _alphaMemory[nodeID];
                Console.WriteLine($"\nAlpha node {nodeID}\t{thisNode.Attribute} {thisNode.Op} {thisNode.Value}");
                if (thisNode.IsClassTest)
                    Console.WriteLine("Class Test");
                Console.Write($"Alpha Children:\t");
                if (thisNode.AlphaChildren.Count == 0)
                    Console.Write("-");
                foreach (AlphaNode child in thisNode.AlphaChildren)
                {
                    Console.Write($"{child.ID}\t");
                }

                Console.Write("\nBeta Children:\t");
                if (thisNode.BetaChildren.Count == 0)
                    Console.Write("-");
                foreach (BetaNode child in thisNode.BetaChildren)
                {
                    Console.Write($"{child.ID}\t");
                }

                Console.WriteLine("\n\nObjects:\nID        Class               Attributes");
                foreach (int objectID in thisNode.ListObjects())
                {
                    IWMElement iObject = _workingMemory.GetWME(objectID);
                    string message = $"{objectID,-10}{iObject.ClassName,-20}";
                    foreach (KeyValuePair<string, string?> attr in iObject.GetAttributes())
                    {
                        if(attr.Key != "ID" && attr.Value is string val)
                        {
                            message += $"{attr.Key} {val}\t";
                        }
                    }
                    Console.WriteLine(message);
                }
            }
        }

    }
}
