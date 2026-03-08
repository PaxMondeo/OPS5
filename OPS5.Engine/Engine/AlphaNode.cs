using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    internal class AlphaNodeFactory : IAlphaNodeFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public AlphaNodeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAlphaNode NewAlphaNode(IAlphaNode parent, ConditionTest test, List<string> distinctAttributes)
        {
            var aNode = _serviceProvider.GetService(typeof(IAlphaNode));
            if (aNode != null)
            {
                IAlphaNode newAlphaNode = (IAlphaNode)aNode;
                newAlphaNode.SetProperties(parent, test, distinctAttributes);
                return newAlphaNode;
            }
            else
                throw new Exception("Could not instantiate a new Alpha Node");
        }

        public IAlphaNode AlphaRoot()
        {
            var aNode = _serviceProvider.GetService(typeof(IAlphaNode));
            if(aNode != null)
            {
                IAlphaNode rootAlpha = (IAlphaNode)aNode;
                rootAlpha.Attribute = "Root";
                rootAlpha.Op = "";
                rootAlpha.Value = "";

                return rootAlpha;
            }
            throw new Exception("Could not instantiate a root Alpha Node");
        }
    }
    /// <summary>
    /// Alpha nodes are the nodes that feed Working Memory Elements into the network
    ///Alpha node represents list of _objects that match a constant condition
    ///It can feed into another alpha node (if next condition is also constant), or into a Beta node (if next condition if variable)
    /// </summary>
    internal class AlphaNode : IAlphaNode
    {
        private IOPS5Logger _logger;
        private IWMClasses _WMClasses;
        private IWorkingMemory _workingMemory;
        private IConfig _config;

        /// <summary>
        /// Integer ID of the Alpha node
        /// </summary>
        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id == 0)
                    _id = value;
            }
        }
        private int _id;
        /// <summary>
        /// The parent Alpha node from which this node receives its _objects
        /// </summary>
        public IAlphaNode Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (_parent == null)
                    _parent = value;
            }
        }
        private IAlphaNode _parent = default!;
        /// <summary>
        /// List of Alpha nodes that receive _objects from this node
        /// </summary>
        public List<IAlphaNode> AlphaChildren { get; set; } = new List<IAlphaNode>();
        /// <summary>
        /// List of Beta nodes that receive _objects from this node
        /// </summary>
        public List<IBetaNode> BetaChildren { get; set; } = new List<IBetaNode>();
        /// <summary>
        /// The Object attribute whose value is tested to accept _objects to this node
        /// </summary>
        public string? Attribute
        {
            get
            {
                return _attribute;
            }
            set
            {
                if (_attribute == null)
                    _attribute = value;
            }
        }
        private string? _attribute = null;
        /// <summary>
        /// The operator used to test the Object attribute
        /// </summary>
        public string Op
        {
            get
            {
                return _op;
            }
            set
            {
                if (_op == null)
                    _op = value;
            }
        }
        private string _op = string.Empty;
        /// <summary>
        /// The value against which the Object's attribute is tested
        /// </summary>
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value == null)
                    _value = value.ToUpper();
            }
        }
        private string _value = string.Empty;
        /// <summary>
        /// Set to true if the test is to match the Class of the Object
        /// </summary>
        public bool IsClassTest { get; set; } = false;
        /// <summary>
        /// List of Classes that this node can match (when classes are inherited)
        /// </summary>
        public List<string> Classes { get; set; } = new List<string>();
        /// <summary>
        /// List of Objects contained by this node
        /// </summary>
        private List<int> _objects { get; set; } = new List<int>();
        /// <summary>
        /// List of distinct attribute values of _objects in this node - used by DISTINCT clause
        /// </summary>
        private List<string> _distinctAttributes = new List<string>();

        private Dictionary<string, IWMElement> _distinctObjectHash = new Dictionary<string, IWMElement>(StringComparer.OrdinalIgnoreCase);

        private bool _vectorLength { get; set; } = false;






        public AlphaNode(IOPS5Logger logger, IWMClasses WMClasses, IWorkingMemory workingMemory, IConfig config)
        {
            _logger = logger;
            _WMClasses = WMClasses;
            _workingMemory = workingMemory;
            _config = config;

        }

        public void SetProperties(IAlphaNode parent, ConditionTest test, List<string> distinctAttributes)
        {
            _distinctAttributes = new List<string>(distinctAttributes);
            _parent = parent;
            _attribute = test.Attribute;
            _op = test.Operator;
            _value = test.Value;
            _vectorLength = test.VectorLength;

            if (_attribute == "CLASS")
            {
                IsClassTest = true;
                Classes = new List<string>
                {
                    _value.ToUpper()
                };
                if (_WMClasses.IsBaseClass(_value))
                {
                    foreach (KeyValuePair<string, IWMClass> pair in _WMClasses.GetClassesBasedOn(_value))
                    {
                        Classes.Add(pair.Key);
                    }
                }
            }
            else
            {
                IsClassTest = false;
            }

            parent.AttachChildAlpha(this);
                
            //Once the node is created, if it is not the root node, then evaluate all of the parent's _objects to initialise

            foreach (int objectID in _parent.ListObjects())
            {
                AddObject(objectID);
            }
        }

        public void AddObject(int objectID)
        {
            bool addObject = false;

            try
            {
                //If there are no conditions, i.e. root node, then add Object
                if (_attribute == "Root")
                {
                    addObject = true;
                }
                else if (IsClassTest) //Test for class of Object
                {
                    addObject = (Classes.Contains(_workingMemory.GetWME(objectID).ClassName));
                }
                else if (_distinctAttributes.Count > 0)
                {
                    //Distinct Alpha node, only add Object if it is a new distinct value
                    string hashKey = "";
                    foreach (string attr in _distinctAttributes)
                    {
                        hashKey += _workingMemory.GetWME(objectID).AttributeValue(attr);
                    }
                    if (_distinctObjectHash.ContainsKey(hashKey))
                    {
                        addObject = false;
                        _logger.WriteInfo($"Alpha Node {_id} skipping non-distinct object {objectID}", 2);
                    }
                    else
                    {
                        _distinctObjectHash.Add(hashKey, _workingMemory.GetWME(objectID));
                        addObject = true;
                        _logger.WriteInfo($"Alpha Node {_id} adding distinct object {objectID}", 2);
                    }

                }
                else
                {
                    if(_attribute != null)
                    {
                        var av = _workingMemory.GetWME(objectID).AttributeValue(_attribute);
                        if(av is string value)
                        {
                            value = value.ToUpper();
                            if (value != "")
                            {
                                if (_op == "=")
                                {
                                    if (_value.StartsWith("<<"))
                                    {
                                        //Disjunction
                                        List<string> disValues = new List<string>();
                                        string val = "";
                                        for (int x = 2; x < _value.Length; x++)
                                        {
                                            switch (_value[x])
                                            {
                                                case ' ':
                                                case '>':
                                                    if (val != "")
                                                    {
                                                        disValues.Add(val.ToUpper());
                                                    }
                                                    val = "";
                                                    break;
                                                case '|':
                                                    val = _value.Substring(x + 1);
                                                    int y = val.IndexOf('|');
                                                    if (y > 0)
                                                    {
                                                        val = _value.Substring(0, y);
                                                        disValues.Add(val.ToUpper());
                                                        x += y + 1;
                                                    }
                                                    val = "";
                                                    break;
                                                default:
                                                    val += _value[x];
                                                    break;
                                            }
                                        }
                                        addObject = false;
                                        foreach (string atom in disValues)
                                        {
                                            if (atom == value)
                                            {
                                                addObject = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        addObject = (value.ToUpper() == _value.ToUpper());
                                    }
                                }
                                else if (_op == "<>" || _op == "!=")
                                {
                                    addObject = (value.ToUpper() != _value.ToUpper());
                                }
                                else if (_op == "MATCHES")
                                {
                                    Match match = Regex.Match(value, _value);
                                    addObject = match.Success;
                                }
                                else if (_op == "CONTAINS")
                                {
                                    addObject = (value.Contains(_value, StringComparison.OrdinalIgnoreCase));
                                }
                                else
                                {
                                    if (Double.TryParse(_value, out double dbl1))
                                    {
                                        if (Double.TryParse(value, out double dbl2))
                                        {
                                            switch (_op)
                                            {
                                                case ">":
                                                    addObject = (dbl1 < dbl2);
                                                    break;
                                                case "<":
                                                    addObject = (dbl1 > dbl2);
                                                    break;
                                                case ">=":
                                                    addObject = (dbl1 <= dbl2);
                                                    break;
                                                case "<=":
                                                    addObject = (dbl1 >= dbl2);
                                                    break;
                                                case "<>":
                                                case "!=":
                                                    addObject = (dbl1 != dbl2);
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            addObject = false;
                                        }
                                    }
                                }
                            }
                        }

                    }

                }

                if (addObject)
                    {
                        _logger.WriteInfo($"Object #{objectID} accepted by Alpha Node {_id} {_attribute} {_op} {_value}", 2);
                        //Update Object to add this Alpha node to its list of Alpha nodes
                        _workingMemory.GetWME(objectID).AddAlphaNode(_id);
                        //Add Object to list of _objects in this Alpha Node
                        _objects.Add(objectID);

                        foreach (AlphaNode anode in AlphaChildren)
                        {
                            anode.AddObject(objectID);
                        }

                        foreach (BetaNode bnode in BetaChildren)
                        {
                            if (bnode.Negative)
                            {
                                bnode.NegativeRightActivation(objectID);
                            }
                            else
                            {
                                bnode.RightActivation(objectID);
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "AddObject");
            }
        }


        public List<int> ListObjects()
        {
            return new List<int>(_objects);
        }

        public void RemoveObject(int objectID)
        {


            if (_objects.Contains(objectID))
            {
                foreach (AlphaNode aNode in AlphaChildren)
                    aNode.RemoveObject(objectID);

                foreach (BetaNode bnode in BetaChildren)
                {
                    if (bnode.Negative)
                        bnode.NegativeRemoveObject(objectID);
                    else
                        bnode.RemoveObject(objectID);
                }
                _objects.Remove(objectID);



                if (_distinctAttributes.Count > 0)
                {
                    //this is a distinct alpha, so check to see if that object can be replaced
                    string hash = "";
                    foreach (string attr in _distinctAttributes)
                    {
                        hash += _workingMemory.GetWME(objectID).AttributeValue(attr);
                    }
                    if (_distinctObjectHash.ContainsKey(hash))
                    {
                        _distinctObjectHash.Remove(hash);
                    }

                    foreach (int w in _parent.ListObjects())
                    {
                        hash = "";
                        foreach (string attr in _distinctAttributes)
                        {
                            hash += _workingMemory.GetWME(w).GetAttributeValue(attr);
                        }
                        if (!_distinctObjectHash.ContainsKey(hash))
                        {
                            AddObject(w);
                            break;
                        }
                    }
                }
            }
        }

        public int ObjectCount()
        {
            return _objects.Count;
        }

        public void AttachChildAlpha(IAlphaNode node)
        {
            AlphaChildren.Add(node);
        }

        public void AttachChildBeta(IBetaNode node)
        {
            BetaChildren.Add(node);
        }
    }
}
