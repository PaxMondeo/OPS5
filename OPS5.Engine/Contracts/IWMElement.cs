using AttributeLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OPS5.Engine.Contracts
{
    public interface IWMElement
    {
        string ClassName { get; set; }
        int ID { get; set;  }
        int TimeTag { get; set; }

        bool AddToken(int id);
        void ProcessAttributes(AttributesCollection attributes);
        void ProcessElements(string[] elements);
        void Copy(IWMElement source);
        string? AttributeValue(string attr);
        void AddAlphaNode(int node);
        AttributesCollection GetAttributes();
        AttributesCollection GetUserAttributes();
        string? GetAttributeValue(string attribute);
        List<string?> GetAttributeValues();
        List<string?> GetUserAttributeValues();
        void SetAttributeValue(string attribute, string value);
        string GetAttributeType(string attributeName);
        bool IsPersistent();
        bool PersistIndividualObjects();
        bool HasAttribute(string attributeName);
        bool HasChildClass(string attributeName);
    }
}
