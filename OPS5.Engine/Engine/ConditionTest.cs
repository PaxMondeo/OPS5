using System;
using System.Collections.Generic;
using System.Linq;
using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    /// <summary>
    /// A ConditionTest is a test to be performed as part of a LHS Condition in a Rule
    /// </summary>
    public class ConditionTest
    {
        /// <summary>
        /// The Attribute of the Object to be tested
        /// </summary>
        public string Attribute { get; }
        /// <summary>
        /// The Operator to be used in evaluating the Attribute
        /// </summary>
        public string Operator { get;  }
        /// <summary>
        /// The value to be tested for
        /// </summary>
        public string Value { get;  }
        /// <summary>
        /// Indicates that the Object Attribute value must be distinct (only the first matching Object will be accepted)
        /// </summary>
        public bool DistinctAttribute { get; set; } = false;
        public bool Concatenation { get; set; } = false;
        public bool VectorLength { get; set; } = false;
        public bool InTest { get; set; } = false;
        public bool CheckTest { get; set; } = false;
        public bool MatchTest { get; set; } = false;
        public bool ContainsTest { get; set; } = false;
        public string InVar { get; set; } = "";

        //If >=0, indicates that the value is to be matched against item in the attribute's vector at that position.
        public int VectorVar { get; set; } = -1;

        /// <summary>
        /// Creates a new ConditionTest
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="op"></param>
        /// <param name="val"></param>
        public ConditionTest(string attr, string op, string val)
        {
            Attribute = attr;
            Operator = op;
            Value = val;
            if (op == "IN")
                InTest = true;
            if (op == "MATCHES")
                MatchTest = true;
            if (op == "CONTAINS")
                ContainsTest = true;
        }
    }
}
