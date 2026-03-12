using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace OPS5.Engine
{
    internal class BetaNodeFactory : IBetaNodeFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public BetaNodeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IBetaNode NewBetaNode()
        {
            var bn = _serviceProvider.GetService(typeof(IBetaNode));
            if (bn == null)
                throw new Exception("Unable to instantiate new Beta Node");
            IBetaNode newBetaNode = (IBetaNode)bn;
            return newBetaNode;
        }

    }

    /// <summary>
    /// Beta noded merge Objects from a parent Alpha node with Tokens from a parent Beta node
    /// </summary>
    internal class BetaNode : IBetaNode
    {
        private readonly IOPS5Logger _logger;
        private readonly IUtils _parserUtils;
        private readonly ICheckFactory _checkFactory;
        private IWorkingMemory _workingMemory;
        private readonly ITokenFactory _tokenFactory;
        private readonly IConfig _config;
        private ICalculator _calculator;

        /// <summary>
        /// Unique integer ID of the Beta node
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// The Alpha node that feeds Objects to this node
        /// </summary>
        public IAlphaNode AlphaParent
        {
            get
            {
                return _alphaParent;
            }
            set
            {
                if (_alphaParent == null)
                    _alphaParent = value;
            }
        }
        private IAlphaNode _alphaParent = default!;
        /// <summary>
        /// The Beta node that feeds Tokens to this node
        /// </summary>
        public IBetaNode BetaParent
        {
            get
            {
                return _betaParent;
            }
            set
            {
                if (_betaParent == null)
                    _betaParent = value;
            }
        }
        private IBetaNode _betaParent = default!;
        /// <summary>
        /// List of ConditionTests that are performed before adding an Object to a Token
        /// </summary>
        public List<ConditionTest> Tests { get; set; }
        /// <summary>
        /// Dictionary for looking up Bindings of variables to Object attributes
        /// </summary>
        public Dictionary<string, Binding> Bindings { get; set; }
        /// <summary>
        /// List of Tokens contained in this node
        /// </summary>
        public Dictionary<int, IToken> Tokens { get;  }
        private readonly Dictionary<string, byte> _tokenSignatures = new Dictionary<string, byte>();
        /// <summary>
        /// List of Beta nodes that receive Tokens from this node
        /// </summary>
        public List<IBetaNode> BetaChildren
        {
            get
            {
                return _betaChildren;
            }
            set
            {
                if (_betaChildren == null)
                    _betaChildren = value;
            }
        }
        private List<IBetaNode> _betaChildren;
        /// <summary>
        /// Indicates that this node performs a Negative test. I.e. it only passes Tokens that do not have a matching Object in the Alpha parent
        /// </summary>
        public bool Negative { get; set; }
        /// <summary>
        /// Indicates that for this Beta node, Right Activation will only cause a maximum of 1 Token to be added
        /// </summary>
        public bool IsAny { get; set; }



        public BetaNode(IOPS5Logger logger,
                        IUtils parserUtils,
                        ICheckFactory checkFactory,
                        IWorkingMemory workingMemory,
                        ITokenFactory tokenFactory,
                        IConfig config,
                        ICalculator calculator)
        {
            _logger = logger;
            _parserUtils = parserUtils;
            _checkFactory = checkFactory;
            _tokenFactory = tokenFactory;
            _config = config;
            _calculator = calculator;
            _workingMemory = workingMemory;

            //Only used for Beta Root.
            _betaChildren = new List<IBetaNode>();
            Tests = new List<ConditionTest>();
            Tokens = new Dictionary<int, IToken>();
            Bindings = new Dictionary<string, Binding>(StringComparer.OrdinalIgnoreCase);
            Negative = false;
        }

        public void SetProperties(IBetaNode betaParent, IAlphaNode alphaParent, List<ConditionTest> tests, Dictionary<string, Binding> bindings, bool negative, bool isAny)
        {
            _alphaParent = alphaParent;
            _betaParent = betaParent;
            Tests = tests;
            Bindings = bindings;
            Negative = negative;
            IsAny = isAny;

            _alphaParent.AttachChildBeta(this);
            _betaParent.AttachChildNode(this);

            //Now look at all the tokens in the parent and process them

            List<IToken> tokens = _betaParent.Tokens.Values.ToList();
            foreach (Token token in tokens)
            {
                if (Negative)
                {
                    NegativeLeftActivation(token);
                }
                else
                {
                    LeftActivation(token);
                }
            }
        }

        public void AddBindings(Dictionary<string, Binding> newBindings)
        {
            List<IToken> tokens = Tokens.Values.ToList();
            //We are sharing this Beta node, so we need to add new bindings, and then process existing tokens to add variables for the new binding(s)
            foreach(KeyValuePair<string, Binding> pair in newBindings)
            {
                string variable = pair.Key;
                if (!Bindings.ContainsKey(variable))
                {
                    Binding binding = pair.Value;
                    Bindings.Add(variable, binding);
                    int objectIndex = binding.ObjectIndex;
                    
                    foreach(IToken token in tokens)
                    {
                        int objectID = token.ObjectIDs[binding.ObjectIndex - 1];
                        var varVal = _workingMemory.GetWME(objectID).GetAttributeValue(binding.Attribute); 
                        if (varVal is string && varVal != "")
                            token.NewVariable(variable, varVal);
                    }

                }
            }
        }

        /// <summary>
        ///Left activation is adding a new token which contains one or more Objects, from the Beta node above
        ///Therefore loop through all of the Objects in the Alpha node above, and test to see if they match the existing Objects in the token.
        ///Each time one does, create a new copy of the token, adding the Object to it, and pass it down to the next Beta to store.
        ///If token is empty, parent is root, so just add Object
        /// </summary>
        /// <param name="token"></param>

        public void LeftActivation(IToken token)
        {

            //Make a copy of the token so we don't update variables in passed token, and to avoid concurrency issues
            IToken newToken = _tokenFactory.NewToken(ID);
            token.Copy(newToken);
            List<int> objectIDs = AlphaParent.ListObjects();

            if (_logger.Verbosity > 1)
            {
                string msg = $"Left Activating Beta Node {ID} with Token [";
                foreach (int objectID in newToken.ObjectIDs)
                {
                    msg += $"#{objectID} ";
                }
                msg += "]";
                _logger.WriteInfo(msg, 2);
            }

            try
            {
                        foreach (int objectID in objectIDs)
                            {
                            //Make a copy of the token so we don't update variables in passed token
                            IToken tmpToken = _tokenFactory.NewToken(ID);
                            newToken.Copy(tmpToken);
                            IWMElement iObject = _workingMemory.GetWME(objectID);
                                bool addObject = false;
                                if (tmpToken.ObjectIDs.Count == 0)
                                {
                                    BindFirstObjectVariables(tmpToken, iObject);
                                    addObject = true;
                                }
                                else if (PerformTests(tmpToken, iObject))
                                {
                                    addObject = true;
                                }
                                if (addObject)
                                {
                                    tmpToken.AddObject(objectID);
                                    tmpToken.UpdateObjects();
                                    if (!_tokenSignatures.ContainsKey(tmpToken.GetObjectKey()))
                                    {
                                        AddToken(tmpToken);
                                        if (IsAny)
                                            break;
                                    }
                                }
                            }
            }
                catch (Exception ex)
                {
                _logger.WriteError(ex.Message, "LeftActivation");
                    throw;
                }
        }

        /// <summary>
        ///Right activation is adding a new Object from the Alpha node above.
        ///Therefore loop through all of the Tokens in the Beta node above and for each one, see if the Object matches
        ///Each time it does, create a new copy of the Token, adding the Object to it, and pass it down to the next Beta to store.
        ///If the Token is empty, then the parent is the root node, so just add the Object
        /// </summary>
        /// <param name="objectID"></param>

        public void RightActivation(int objectID)
        {
            _logger.WriteInfo($"Right Activating Beta node {ID} with Object {objectID}", 2);
            //Copy Beta parent's tokens to an array to avoid locking on iteration

            List<IToken> betaTokens = BetaParent.Tokens.Values.ToList();
            List<IToken> tokens = Tokens.Values.ToList();

            try
            {
                    foreach (IToken token in betaTokens)
                    {
                        bool doToken = true;
                        if (IsAny)
                        {
                            //We only want to pair this Object up with token from the parent Beta if that token has not already been paired with a previous Object
                            foreach (IToken existing in tokens)
                            {
                                bool thisOne = true;
                                for (int x = 0; x < token.ObjectIDs.Count; x++)
                                {
                                    if (token.ObjectIDs[x] != existing.ObjectIDs[x])
                                        thisOne = false;
                                }
                                if (thisOne)
                                {
                                    doToken = false;
                                    break;
                                }
                            }
                        }
                        if (doToken)
                        {
                            IToken newToken = _tokenFactory.NewToken(ID);
                            token.Copy(newToken);

                            bool addObject = false;
                            if (newToken.ObjectIDs.Count == 0)
                            {
                                BindFirstObjectVariables(newToken, _workingMemory.GetWME(objectID));
                                addObject = true;
                            }
                            else if (PerformTests(newToken, _workingMemory.GetWME(objectID)))
                            {
                                addObject = true;
                            }
                            if (addObject)
                            {
                                newToken.AddObject(objectID);
                                newToken.UpdateObjects();
                                if (!_tokenSignatures.ContainsKey(newToken.GetObjectKey()))
                                {
                                    AddToken(newToken);
                                }

                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "RightActivation");
                throw;
            }



        }



        public void NegativeLeftActivation(IToken token)
        {
            bool addToken = true;
            try
            {
                _logger.WriteInfo($"Negative Left Activating Beta node {ID} after change", 2);

                IToken newToken = _tokenFactory.NewToken(ID);
                token.Copy(newToken);

                List<int> objects = AlphaParent.ListObjects();
                if(Tests.Count == 0)
                {
                    //If there are no tests, then that means if there is any Alpha node at all, then this negative node should not have a token
                    if (objects.Count > 0)
                        addToken = false;
                }
                else
                {
                    foreach (int objectID in objects)
                    {
                        if (PerformTests(newToken, _workingMemory.GetWME(objectID)))
                        {
                            addToken = false;
                            break;
                        }
                    }
                }
                if (addToken)
                {
                    newToken.UpdateObjects();
                    if (!_tokenSignatures.ContainsKey(newToken.GetObjectKey()))
                        AddToken(newToken);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "NegativeLeftActivation");
            }
        }

        public void NegativeRightActivation(int objectID)
        {
            _logger.WriteInfo($"Negative Right Activating Beta node {ID} after change", 2);

            List<IToken> tokens = Tokens.Values.ToList();
            foreach (IToken token in tokens)
            {
                IToken newToken = _tokenFactory.NewToken(ID);
                token.Copy(newToken);
                if (PerformTests(token, _workingMemory.GetWME(objectID)))
                {
                    //Token is no longer valid due to presence of new Object
                    List<int> objectIDs = token.ObjectIDs.ToList();
                    RemoveToken(objectIDs);
                }
            }
        }

        public void RemoveObject(int objectID)
        {
            List<IToken> tokens = Tokens.Values.ToList();
            foreach (IToken token in tokens)
            {
                if (token.ObjectIDs.Contains(objectID))
                {
                    //Remove the Object from the token and then remove token from all children
                    //Get Object pattern to identify tokens down the chasin
                    List<int> objectIDs = token.ObjectIDs.ToList();
                    int tokenID = token.ID;
                    if (Tokens.Remove(tokenID))
                    {
                        _tokenSignatures.Remove(token.GetObjectKey());
                        foreach (IBetaNode bNode in BetaChildren)
                            bNode.RemoveToken(objectIDs);
                    }
                    else
                        _logger.WriteError($"Failed to remove Token {tokenID} from Beta Node {ID}", "Remove Object");
                }
            }
        }

        internal void NegativeRemoveObject(int objectID)
        {
            _logger.WriteInfo($"Removing Object {objectID} from Negative Beta node {ID} after change", 2);

            List<IToken> tokens = BetaParent.Tokens.Values.ToList();
            List<int> objectIDs = AlphaParent.ListObjects().Where(w => w != objectID).ToList();

            foreach (IToken token in tokens)
            {
                if (!_tokenSignatures.ContainsKey(token.GetObjectKey()))
                {
                    //Check that none of the Alpha nodes' Objects match
                    IToken newToken = _tokenFactory.NewToken(ID);
                    token.Copy(newToken);
                    bool addToken = true;
                    foreach (int object2ID in objectIDs)
                    {
                        if (PerformTests(newToken, _workingMemory.GetWME(object2ID)))
                        {
                            addToken = false;
                        }
                    }
                    if (addToken)
                    {
                        AddToken(newToken);
                    }
                }
            }

        }

        private void AddToken(IToken newToken)
        {
            bool added = false;
            if(Tokens.ContainsKey(newToken.ID))
                _logger.WriteError($"Attempt to add dulicate Token {newToken.ID} to Beta Node {ID}", "AddToken");
            else
            {
                Tokens.Add(newToken.ID, newToken);
                added = true;
                if (added)
                {
                    _tokenSignatures[newToken.GetObjectKey()] = 0;
                    newToken.Owner = ID;
                    _logger.WriteInfo($"Added token to Beta Node {ID}", 2);
                    List<IBetaNode> children = BetaChildren.ToList();
                    foreach (IBetaNode child in children)
                    {
                        if (child.Negative)
                        {
                            child.NegativeLeftActivation(newToken);
                        }
                        else
                        {
                            child.LeftActivation(newToken);
                        }
                    }
                }
                else
                    _logger.WriteError($"Failed to add token {newToken.ID} to Beta Node {ID}", "AddToken");
            }

        }

        /// <summary>
        ///Removes token from this node and pointer to this token from all Objects
        ///Also removes all tokens where first Objects match all Objects of this token
        /// </summary>
        /// <param name="objectIDs"></param>
        public void RemoveToken(List<int> objectIDs)
        {
            try
            {
                  
                if (_logger.Verbosity > 1)
                {
                    string msg = "Removing token [";
                    foreach (int objectID in objectIDs)
                    {
                        msg += $"#{objectID} ";
                    }
                    msg += $"] from Beta Node {ID}";
                    _logger.WriteInfo(msg, 2);
                }

                List<int> tokens = FindMatchingTokens(objectIDs);

                foreach(int tokenID in tokens)
                {
                        if(Tokens.TryGetValue(tokenID, out IToken? removedToken) && Tokens.Remove(tokenID))
                        {
                            _tokenSignatures.Remove(removedToken.GetObjectKey());
                            List<IBetaNode> children = BetaChildren.ToList();
                            foreach (IBetaNode child in children)
                            {
                                child.RemoveToken(objectIDs);
                            }
                        }
                        else
                            _logger.WriteError($"Failed to delete Token {tokenID} from Beta Node {ID}", "RemoveToken");
                }

            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "RemoveToken");                                                                                                                                                                       
            }
        }

        /// <summary>
        ///When removing a token, each child Beta node has to remove all Tokens whose starting Objects match the Objects of the token that was deleted.
        ///Having been passed the list of Object ids to match, we search for a list of matching Tokens
        /// </summary>
        /// <param name="objectIDs"></param>
        /// <returns></returns>
        private List<int> FindMatchingTokens(List<int> objectIDs)
        {
            List<int> tokens = new List<int>();
            List<IToken> tmpTokens = Tokens.Values.ToList();
            if (objectIDs.Count == 0)
            {
                //If there are no Object IDs, then we are looking for an empty token, i.e. the Beta node is a negative beta in the first condition.
                foreach (IToken token in tmpTokens)
                {
                    if (token.ObjectCount() == 0)
                        tokens.Add(token.ID);
                }
            }
            else
            {
                try
                {
                    foreach (IToken token in tmpTokens)
                    {
                        bool keep = false;
                        for (int i = 0; i < objectIDs.Count; i++)
                        {
                            if (token.ObjectIDs.Count == i)
                            {
                                keep = true;
                                break;
                            }
                            if (objectIDs[i] != token.ObjectIDs[i])
                                keep = true;
                        }

                        if (!keep)
                            tokens.Add(token.ID);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteError(ex.Message, "FindMatchingTokens");
                }
            }


            return tokens;
        }

        private void BindFirstObjectVariables(IToken token, IWMElement wme)
        {
            //The first Object is added to an empty token, so no tests are performed
            //Subsequent Objects are tested before their variable bindings are added
            Dictionary<string, string> variables = token.GetVariables(); //get the variables already bound to this token

            foreach (KeyValuePair<string, Binding> binding in Bindings.Where(b => b.Value.ObjectIndex == 1))
            {
                //There is a binding to this Object, so bind the variable
                var varVal = wme.AttributeValue(binding.Value.Attribute);
                if (_config.Ops5 && varVal is string && varVal.StartsWith("|") && varVal.EndsWith("|"))
                    varVal = varVal.Replace("|", "");
                if (varVal is string && varVal != "")
                {
                    variables.Add(binding.Key, varVal);
                }
            }
            token.SetVariables(variables);
        }

        /// <summary>
        /// Performs the tests in the token on the object, looking for a match
        /// </summary>
        /// <param name="token"></param>
        /// <param name="wme"></param>
        /// <returns></returns>
        private bool PerformTests(IToken token, IWMElement wme)
        {
            bool res = true;
            char[] trimChars = new char[] { ' ', '\t', '\n' };
            try
            {
                if (!Negative)  //Don't try to bind variables from a negative clause - we only test
                {

                    //Before we perform the tests, we need to bind any variables to values in this Object, according to the position the Object would take in the token
                    int objectIndex = token.ObjectCount() + 1;
                    foreach (KeyValuePair<string, Binding> binding in Bindings.Where(b => b.Value.ObjectIndex == objectIndex))
                    {
                        if (!token.HasVar(binding.Key))
                        {
                            //There is a binding to this Object, so bind the variable
                            var varVal = wme.AttributeValue(binding.Value.Attribute);
                            if (_config.Ops5 && varVal is string && varVal.StartsWith("|") && varVal.EndsWith("|"))
                                varVal = varVal.Replace("|", "");
                            if (varVal is string && varVal != "")
                            {
                                token.NewVariable(binding.Key, varVal);
                            }
                            else
                            {
                                res = false; //Couldn't find attribute, so wrong Object type
                            }
                        }
                    }

                    //Now check to see if we can fulfill a bind statement
                    foreach (KeyValuePair<string, Binding> binding in Bindings.Where(b => b.Value.isBind).Where(k => !token.HasVar(k.Key)))
                    {
                        if (binding.Value.isComputation)
                        {
                            var computation = binding.Value.Computation;
                            if(computation == null)
                                _logger.WriteError($"Unknown computation {binding.Value.Computation}", "BetaNode.PerformTests");
                            else
                            {
                                //Evaluate all variables and replace them in the calculation, then pass calculation to calculator
                                string result = "0";
                                switch (binding.Value.ComputeType)
                                {
                                    case "CALC":
                                    case "COMPUTE":
                                        result = _calculator.DoCalc(computation.ToList(), token);
                                        break;
                                    case "ADDYEARS":
                                    case "ADDMONTHS":
                                    case "ADDWEEKS":
                                    case "ADDDAYS":
                                    case "ADDHOURS":
                                    case "ADDMINS":
                                    case "ADDSECS":
                                        //TODO: result = _dateTimeCalculator.AddCalc(binding.Value.ComputeType, computation.ToList());
                                        break;

                                    default:
                                        _logger.WriteError($"Unknown computation type {binding.Value.ComputeType}", "BetaNode.PerformTests");
                                        break;
                                }
                                token.NewTempVariable(binding.Key, result);

                            }
                        }
                        else
                        {
                            List<string> atoms = _parserUtils.ParseCommand(binding.Value.Attribute);
                            if (atoms[0] == "POP")
                            {
                                string val = token.TryGetVariableValue(atoms[1]);
                                if (val != atoms[1])
                                {
                                    token.UpdateTempVariable(atoms[1], "NIL");
                                    token.NewTempVariable(binding.Key, val);
                                }
                            }
                            else if (atoms[0] == "PEEK")
                            {
                                string val = token.TryGetVariableValue(atoms[1]);
                                if (val != atoms[1])
                                {
                                    token.UpdateTempVariable(atoms[1], "NIL");
                                    token.NewTempVariable(binding.Key, val);
                                }
                            }
                            else
                            {
                                string val = "";
                                foreach (string atom in atoms)
                                {
                                    if (atom.Trim(trimChars) != "+")
                                    {
                                        if (token.HasVar(atom))
                                        {
                                            string temp = token.TryGetVariableValue(atom);
                                            if (temp != "NIL")
                                                val += temp;
                                        }
                                        else
                                            val += atom;
                                    }
                                }
                                token.NewTempVariable(binding.Key, val);
                            }
                        }
                    }
                }

                //Now, provided there were no errors binding variables, proceed with tests
                if (res)
                {
                    foreach (ConditionTest test in Tests)
                    {
                        if (test.CheckTest)
                        {
                            string checkVal = token.TryGetVariableValue(test.Value);
                            ICheck check = _checkFactory.GetCheck(test.Attribute);
                            res = check.Evaluate(checkVal, token);
                        }
                        else
                        {
                            var objectVal = wme.AttributeValue(test.Attribute);
                            string objectType = wme.GetAttributeType(test.Attribute);
                            string varVal = "";
                            if (objectVal is string &&  objectVal != "") //arg1 = string value of attribute in this Object
                            {
                                //We should have all required variables bound by now
                                if (test.InTest)
                                {
                                    varVal = test.Value;
                                    varVal = token.TryGetVariableValue(test.Value);
                                    bool found = false;
                                    if (varVal != "NIL")
                                    {
                                        if (objectVal.ToUpper() == varVal.ToUpper())
                                            found = true;
                                    }

                                    if (test.Operator == "IN" && !found || test.Operator == "!IN" && found)
                                        res = false;
                                }
                                else
                                {
                                    if (test.Concatenation)
                                    {
                                        string[] atoms = test.Value.Split(' ');
                                        foreach (string atom in atoms)
                                        {
                                            string tmpVal = atom;
                                            tmpVal = token.TryGetVariableValue(tmpVal);
                                            varVal += tmpVal;
                                        }
                                    }
                                    else if (token.HasVar(test.Value))
                                    {
                                        //Already bound variable, so use saved value, and it *must* be a variable, else it is handled by Alpha node.
                                        varVal = token.TryGetVariableValue(test.Value);
                                    }
                                    else
                                    {
                                        res = false;
                                    }

                                    if (test.MatchTest)
                                        {
                                            Match match = Regex.Match(objectVal, varVal);
                                            if (!match.Success)
                                                res = false;
                                        }
                                        else if (test.ContainsTest)
                                        {
                                            res = varVal.Contains(objectVal);
                                        }
                                        else if (test.Operator == "=")
                                        {
                                            switch (objectType)
                                            {
                                                case "DATE":
                                                    if (DateTime.TryParse(objectVal, out DateTime oDate) && DateTime.TryParse(varVal, out DateTime vDate))
                                                    {
                                                        if (oDate.Date != vDate.Date)
                                                            res = false;
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                case "DATETIME":
                                                    if (DateTime.TryParse(objectVal, out DateTime oDateTime) && DateTime.TryParse(varVal, out DateTime vDateTime))
                                                    {
                                                        if (oDateTime != vDateTime)
                                                            res = false;
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                case "TIME":
                                                    if (DateTime.TryParse(objectVal, out DateTime oTime) && DateTime.TryParse(varVal, out DateTime vTime))
                                                    {
                                                        if (TimeSpan.Compare(oTime.TimeOfDay, vTime.TimeOfDay) != 0)
                                                            res = false;
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                default:
                                                    if (objectVal.ToUpper() != varVal.ToUpper())
                                                    {
                                                        res = false;
                                                    }
                                                    break;
                                            }
                                        }
                                        else if (test.Operator == "<>" || test.Operator == "!=")
                                        {
                                            switch (objectType)
                                            {
                                                case "DATE":
                                                    if (DateTime.TryParse(objectVal, out DateTime oDate) && DateTime.TryParse(varVal, out DateTime vDate))
                                                    {
                                                        if (oDate.Date == vDate.Date)
                                                            res = false;
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                case "DATETIME":
                                                    if (DateTime.TryParse(objectVal, out DateTime oDateTime) && DateTime.TryParse(varVal, out DateTime vDateTime))
                                                    {
                                                        if (oDateTime == vDateTime)
                                                            res = false;
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                case "TIME":
                                                    if (DateTime.TryParse(objectVal, out DateTime oTime) && DateTime.TryParse(varVal, out DateTime vTime))
                                                    {
                                                        if (TimeSpan.Compare(oTime.TimeOfDay, vTime.TimeOfDay) == 0)
                                                            res = false;
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                default:
                                                    if (objectVal.ToUpper() == varVal.ToUpper())
                                                    {
                                                        res = false;
                                                    }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            switch (objectType)
                                            {
                                                case "DATE":
                                                    if (DateTime.TryParse(objectVal, out DateTime oDate) && DateTime.TryParse(varVal, out DateTime vDate))
                                                    {
                                                        switch (test.Operator)
                                                        {
                                                            case "<":
                                                                if (oDate.Date >= vDate.Date)
                                                                {
                                                                    res = false;
                                                                }
                                                                break;
                                                            case ">":
                                                                if (oDate.Date <= vDate.Date)
                                                                {
                                                                    res = false;
                                                                }
                                                                break;
                                                            case ">=":
                                                                if (oDate.Date < vDate.Date)
                                                                {
                                                                    res = false;
                                                                }
                                                                break;
                                                            case "<=":
                                                                if (oDate.Date > vDate.Date)
                                                                {
                                                                    res = false;
                                                                }
                                                                break;
                                                            default:
                                                                res = false;
                                                                break;
                                                        }
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                case "DATETIME":
                                                    if (DateTime.TryParse(objectVal, out DateTime d1) && DateTime.TryParse(varVal, out DateTime d2))
                                                    {
                                                        switch (test.Operator)
                                                        {
                                                            case "<":
                                                                if (d1 >= d2)
                                                                    res = false;
                                                                break;

                                                            case ">":
                                                                if (d1 <= d2)
                                                                    res = 
                                                                        false;
                                                                break;
                                                            case ">=":
                                                                if (d1 < d2)
                                                                    res = false;
                                                                break;

                                                            case "<=":
                                                                if (d1 > d2)
                                                                    res = false;
                                                                break;

                                                            default:
                                                                res = false;
                                                                break;
                                                        }
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                case "TIME":
                                                    if (DateTime.TryParse(objectVal, out DateTime oTime) && DateTime.TryParse(varVal, out DateTime vTime))
                                                    {
                                                        switch (test.Operator)
                                                        {
                                                            case "<":
                                                                if (TimeSpan.Compare(oTime.TimeOfDay, vTime.TimeOfDay) >= 0)
                                                                    res = false;
                                                                break;

                                                            case ">":
                                                                if (TimeSpan.Compare(oTime.TimeOfDay, vTime.TimeOfDay) <= 0)
                                                                    res = false;
                                                                break;

                                                            case ">=":
                                                                if (TimeSpan.Compare(oTime.TimeOfDay, vTime.TimeOfDay) < 0)
                                                                    res = false;
                                                                break;

                                                            case "<=":
                                                                if (TimeSpan.Compare(oTime.TimeOfDay, vTime.TimeOfDay) > 0)
                                                                    res = false;
                                                                break;

                                                            default:
                                                                res = false;
                                                                break;
                                                        }
                                                    }
                                                    else
                                                        res = false;
                                                    break;

                                                 case   "NUMBER":
                                                    if (double.TryParse(objectVal, out double v1) && double.TryParse(varVal, out double v2))
                                                    {
                                                        switch (test.Operator)
                                                        {
                                                            case "<":
                                                                if (v1 >= v2)
                                                                {
                                                                    res = false;
                                                                }
                                                                break;
                                                            case ">":
                                                                if (v1 <= v2)
                                                                {
                                                                    res = false;
                                                                }
                                                                break;
                                                            case ">=":
                                                                if (v1 < v2)
                                                                {
                                                                    res = false;
                                                                }
                                                                break;
                                                            case "<=":
                                                                if (v1 > v2)
                                                                {
                                                                    res = false;
                                                                }
                                                                break;
                                                            default:
                                                                res = false;
                                                                break;
                                                        }
                                                    }
                                                    break;

                                                default:
                                                    if (double.TryParse(objectVal, out double dObj) && double.TryParse(varVal, out double dVar))
                                                    {
                                                        switch (test.Operator)
                                                        {
                                                            case "<":
                                                                if (dObj >= dVar)
                                                                    res = false;
                                                                break;
                                                            case ">":
                                                                if (dObj <= dVar)
                                                                    res = false;
                                                                break;
                                                            case ">=":
                                                                if (dObj < dVar)
                                                                    res = false;
                                                                break;
                                                            case "<=":
                                                                if (dObj > dVar)
                                                                    res = false;
                                                                break;
                                                            default:
                                                                res = false;
                                                                break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (objectVal.ToUpper() == varVal.ToUpper())
                                                        {
                                                            res = false;
                                                        }
                                                    }
                                                    break;
                                            }
                                        }
                                }
                            }
                            else
                            {
                                res = false;
                            }
                        }
                        if (!res)
                            break;
                    }
                }
                if (res)
                    token.CommitVariables();
                else
                    token.RollBackVariables();
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "PerformTests");
                res = false;
            }
            return res;
        }
        public void AttachChildNode(IBetaNode node)
        {
            BetaChildren.Add(node);
        }

        public int TokenCount()
        {
            return Tokens.Count;
        }

        public bool HasTokens()
        {
            return (Tokens.Count > 0);
        }


    }

}
