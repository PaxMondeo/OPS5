using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface IWMClassFactory
    {
        IWMClass NewClass(string className);
    }
    public interface IWMClass
    {
        string ClassName { get; set; }
        void AddAttribute(string attribute);
        void AddAttributes(List<string> attributes);
        string NextObjectID();
        void TrySetObjectCount(int id);
        List<string> GetUserAttributes();
        bool AttributeExists(string attributeName);
        List<string> GetAttributes();
        void SetDefaults(Dictionary<string, string> defaults);
        string? GetDefaultValue(string attributeName);
        void SetVectorAttribute(string attributeName);
        bool IsVectorAttribute(string attributeName);
    }
}
