using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    internal class TokenFactory : ITokenFactory
    {
        IServiceProvider _serviceProvider;
        IObjectIDs _objectIDs;
        public TokenFactory(IServiceProvider serviceProvider, IObjectIDs objectIDs)
        {
            _serviceProvider = serviceProvider;
            _objectIDs = objectIDs;
        }

        public IToken NewToken(int owner)
        {
            var t = _serviceProvider.GetService(typeof(IToken));
            if (t == null)
                throw new Exception("Unable to instantiate new Token");
            IToken token = (IToken)t;
            token.ID = _objectIDs.NextTokenID();
            token.Owner = owner;
            return token;
        }
    }
    /// <summary>
    /// A Token contains a set of Objects that match a set of consecutive LHS conditions in a rule
    /// </summary>
    internal class Token : IToken
    {
        private IOPS5Logger _logger;
        private IWorkingMemory _workingMemory;
        private IConfig _config;

        public int ID { get; set; }
        /// <summary>
        /// List of Objects that make up the Token
        /// </summary>
        public List<int> ObjectIDs { get; set; } = new List<int>();
        /// <summary>
        /// Records the system Time Tag last time the Token was updated
        /// </summary>
        public List<int> Recency { get; set; } = new List<int>();
        /// <summary>
        /// Identifies the node that "owns" the Token
        /// </summary>
        public int Owner { get; set; }
        /// <summary>
        /// Dictionary of variable and bound values for the Objects in this Token
        /// </summary>
        public ConcurrentDictionary<string, string> Variables { get; set; } = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, string> _tempVariables  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Indicates that this Token has been used in a previous recognise-act cycle
        /// </summary>
        public bool Fired { get; set; }





        /// <summary>
        /// Creates a new Token
        /// </summary>
        /// <param name="owner"></param>
        public Token(IOPS5Logger logger, IWorkingMemory workingMemory, IConfig config)
        {
            _logger = logger;
            _workingMemory = workingMemory;
            _config = config;
            Fired = false;
        }

        public void AddObject(int objectID)
        {
            lock (ObjectIDs)
            {
                ObjectIDs.Add(objectID);
            }
            UpdateObjects();
            _logger.WriteInfo($"Added object #{objectID} to token owned by {Owner}", 2);
        }

        public void Copy(IToken toToken)
        {
            //Just copies the objects and vars, but doesn't update the objects, in case it isn't used
            foreach (int objectID in ObjectIDs)
            {
                lock (toToken.ObjectIDs)
                {
                    toToken.AddObject(objectID);
                }
            }
            foreach (KeyValuePair<string, string> var in Variables)
            {
                toToken.Variables.TryAdd(var.Key, var.Value);
            }
        }

        internal void Append(IToken newToken)
        {
            foreach (int objectID in newToken.ObjectIDs)
            {
                lock (ObjectIDs)
                {
                    ObjectIDs.Add(objectID);
                }
            }
            UpdateObjects();
            foreach (KeyValuePair<string, string> pair in newToken.Variables)
            {
                if (!Variables.ContainsKey(pair.Key))
                {
                    Variables.TryAdd(pair.Key, pair.Value);
                }
            }
        }

        public void UpdateObjects()
        {
            Recency.Clear();
            //We are going to use this token, so update the Objects
            foreach (int objectID in ObjectIDs)
            {
                Recency.Add(_workingMemory.GetWME(objectID).TimeTag);
                bool done = false;
                //avoid concurrency errors
                while (!done)
                {
                    done = _workingMemory.GetWME(objectID).AddToken(ID);
                }
            }
        }

        public bool Compare(IToken newToken)
        {
            bool res = true;
            if (newToken.ObjectIDs.Count == ObjectIDs.Count)
            {
                for(int x = 0; x < ObjectIDs.Count; x++)
                    if(ObjectIDs[x] != newToken.ObjectIDs[x])
                        res = false;
            }
            else
            {
                res = false;
            }
            return res;
        }

        public string GetObjectKey()
        {
            return string.Join(",", ObjectIDs);
        }

        public void NewVariable(string var, string val)
        {
            if (!Variables.ContainsKey(var))
                Variables.TryAdd(var, val);
        }

        public void NewTempVariable(string var, string val)
        {
            if (!_tempVariables.ContainsKey(var))
                _tempVariables.Add(var, val);
        }


        public int ObjectCount()
        {
            return ObjectIDs.Count;
        }

        public void UpdateVariable(string var, string val)
        {
            string variable = var.ToUpper();
            lock (Variables)
            {
                Variables.AddOrUpdate(variable, val, (key, oldValue) => val);
            }
        }

        public void UpdateTempVariable(string var, string val)
        {
            string variable = var.ToUpper();
            _tempVariables[variable] = val;
        }

        public Dictionary<string, string> GetVariables()
        {
            Dictionary<string, string> vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> pair in Variables)
                vars.Add(pair.Key, pair.Value);

            return vars;
        }

        public void SetVariables(Dictionary<string, string> vars)
        {
            lock (Variables)
            {
                Variables.Clear();
                foreach (KeyValuePair<string, string> pair in vars)
                    Variables.TryAdd(pair.Key, pair.Value);
            }
        }

        public void Remove()
        {
            throw new NotImplementedException("Token.Remove");
        }


        public string TryGetVariableValue(string variable)
        {
            string val = variable;  //in case it isn't a var - we call this to check if it is as well
            string var = variable.ToUpper();

            if (_tempVariables.ContainsKey(var))
                val = _tempVariables[var];
            else if (Variables.ContainsKey(var))
                val = Variables[var];

            if (_config.Ops5 && val.StartsWith("|"))
                val = val.Replace("|", "");

            val = Utilities.Formatting.CheckForDateTime(val);

            return val;
        }

        public bool HasVar(string variable)
        {
            return Variables.ContainsKey(variable.ToUpper()) || _tempVariables.ContainsKey(variable.ToUpper());
        }

        public void CommitVariables()
        {
            foreach(KeyValuePair<string, string> tempVar in _tempVariables)
            {
                Variables.AddOrUpdate(tempVar.Key, tempVar.Value, (key, existingVal) => { return tempVar.Value;  });
            }
            _tempVariables.Clear();
        }

        public void RollBackVariables()
        {
            _tempVariables.Clear();
        }

        public string GetRecency()
        {
            string recency = "";

            foreach (int rec in Recency)
                recency += rec.ToString() + ",";
            if (recency.Length > 0)
                recency = recency.Substring(0, recency.Length - 1);
            return recency;
        }
    }
}
