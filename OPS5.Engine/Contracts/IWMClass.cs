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
        string Comment { get; set; }
        bool Enabled { get; set; }
        string ClassFile { get; set; }
        void AddAttribute(string attribute, string dataType = "GENERAL");
        void AddAttributes(List<string> attributes);
        string NextObjectID();
        void TrySetObjectCount(int id);
        List<string> GetUserAttributes();
        bool AttributeExists(string attributeName);
        List<string> GetAttributes();
        string GetAttributeType(string attributeName);
        void SetDefaults(Dictionary<string, string> defaults);
        string? GetDefaultValue(string attributeName);
        void SetAttributeType(string attributeName, string dataType);
    }
}
