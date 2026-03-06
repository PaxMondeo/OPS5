using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OPS5.Engine
{
    internal class ClassRelationships : IClassRelationships
    {
        private IOPS5Logger _logger;

        private List<ClassRelationship> _relationships = new List<ClassRelationship>();
        
        public ClassRelationships(IOPS5Logger logger)
        {
            _logger = logger;
        }
        public void CreateRelationship(string parentClass, string childClass, string? parentAttribute = null, string? childAttribute = null)
        {
            parentClass = parentClass.ToUpper();
            childClass = childClass.ToUpper();
            if(parentAttribute == null)
            {
                if (childClass.EndsWith("S"))
                    parentAttribute = childClass + "ES";
                else
                    parentAttribute = childClass + "S";
            }
            if (childAttribute == null)
                childAttribute = parentClass + "ID";
            if (!_relationships.Where(_ => _.ParentClass == parentClass && _.ChildClass == childClass).Any())
            {
                _relationships.Add(new ClassRelationship(parentClass, childClass, parentAttribute, childAttribute));
            }
        }

        public string? GetParent(string childClass)
        {
            ClassRelationship? relationship = _relationships.Where(_ => _.ChildClass == childClass.ToUpper()).FirstOrDefault();
            if (relationship == null)
                return null;
            else
                return relationship.ParentClass;
        }

        public bool HasParent(string childClass)
        {
            return _relationships.Where(_ => _.ChildClass == childClass).Any();
        }

        public bool HasChild(string parentClass, string parentAttribute)
        {
            return _relationships.Where(_ => _.ParentClass == parentClass && _.ParentAttribute == parentAttribute.ToUpper()).Any();
        }

        public string? GetChildClass(string parentClass, string parentAttribute)
        {
            ClassRelationship? relationship = _relationships.Where(_ => _.ParentClass == parentClass.ToUpper() && _.ParentAttribute == parentAttribute.ToUpper()).FirstOrDefault();
            if (relationship == null)
                return null;
            else
                return relationship.ChildClass;
        }

        public List<string> GetChildren(string parentClass)
        {
            List<string> children = _relationships.Where(_ => _.ParentClass == parentClass.ToUpper()).Select(_ => _.ChildClass).ToList();
            return children;
        }

        public void PrintRelationships()
        {
            foreach (ClassRelationship relationship in _relationships)
            {
                Console.WriteLine($"{relationship.ChildClass} has parent {relationship.ParentClass}");
            }
        }
    }
}
