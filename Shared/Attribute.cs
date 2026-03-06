using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttributeLibrary
{
    public class Attribute
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }


        public Attribute()
        {
            
        }

        public Attribute(KeyValuePair<string, string?> attribute)
        {
            Name = attribute.Key;
            Value = attribute.Value;
        }
    }
}
