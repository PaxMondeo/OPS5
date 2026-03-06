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
        string BasedOn { get; set; }
        bool IsBaseClass { get; set; }
        string Comment { get; set; }
        bool Enabled { get; set; }
        string ClassFile { get; set; }
        bool ReadOnly { get; set; }
        void AddAttribute(string attribute, string dataType = "GENERAL");
        void AddAttributes(List<string> attributes);
        string NextObjectID();
        void TrySetObjectCount(int id);
        List<string> GetUserAttributes();
        bool AttributeExists(string attributeName);
        List<string> GetAttributes();
        bool IsPersistent { get; set; }
        bool PersistIndividualObjects { get; set; }
        bool HasClassAttribute(string attributeName);
        string? GetSubClass(string attributeName);
        bool HasComplexAttribute(string attributePrefix);
        string GetAttributeType(string attributeName);
    }
}
