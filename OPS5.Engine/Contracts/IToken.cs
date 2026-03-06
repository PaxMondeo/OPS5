using System.Collections.Generic;
using System.Collections.Concurrent;

namespace OPS5.Engine.Contracts
{
    internal interface ITokenFactory
    {
        IToken NewToken(int owner);
    }
    public interface IToken
    {
        int ID { get; set; }
        List<int> ObjectIDs { get; set; }
        List<int> Recency { get; set; }
        int Owner { get; set; }
        ConcurrentDictionary<string, string> Variables { get; set; }
        bool Fired { get; set; }
        void AddObject(int objectID);
        void Copy(IToken toToken);
        void UpdateObjects();
        bool Compare(IToken newToken);
        string GetObjectKey();
        void NewVariable(string var, string val);
        void NewTempVariable(string var, string val);
        int ObjectCount();
        void UpdateVariable(string var, string val);
        void UpdateTempVariable(string var, string val);
        Dictionary<string, string> GetVariables();
        void SetVariables(Dictionary<string, string> vars);
        string TryGetVariableValue(string variable);
        bool HasVar(string variable);
        void CommitVariables();
        void RollBackVariables();
        string GetRecency();
        void Remove();
    }
}
