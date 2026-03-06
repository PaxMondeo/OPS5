using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine
{
    internal class ClassRelationship
    {
        public string ParentClass { get; set; }
        public string ChildClass { get; set; }
        public string ParentAttribute { get; set; }
        public string ChildAttribute { get; set; }

        public ClassRelationship(string parentClass, string childClass, string parentAttribute, string childAttribute)
        {
            ParentClass = parentClass;
            ChildClass = childClass;
            ParentAttribute = parentAttribute;
            ChildAttribute = childAttribute;
        }
    }
}
