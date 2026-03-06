using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface IClassRelationships
    {
        void CreateRelationship(string parentClass, string childClass, string? parentAttribute = null, string? childAttribute = null);
        string? GetParent(string childClass);
        bool HasParent(string childClass);
        List<string> GetChildren(string parentClass);
        bool HasChild(string parentClass, string childClass);
        string? GetChildClass(string parentClass, string parentAttribute);
        void PrintRelationships();
    }
}
