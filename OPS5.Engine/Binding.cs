using OPS5.Engine.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace OPS5.Engine
{
    /// <summary>
    /// A Binding is used in a Beta node to bind an attribute of a Object in a Token to a variable
    /// </summary>
    public class Binding 
    {
        /// <summary>
        /// The position in the Token of the Object to be bound
        /// </summary>
        public int ObjectIndex { get; }
        /// <summary>
        /// The Attribute of the Object to be bound
        /// </summary>
        public string Attribute { get;  }
        /// <summary>
        /// True if this binding is used in a Bind statement
        /// </summary>
        public bool isBind { get;  }
        /// <summary>
        /// True if this requires a computation
        /// </summary>
        public bool isComputation { get; }
        /// <summary>
        /// The computation to be performed, if one is required
        /// </summary>
        public string[]? Computation { get; }
        /// <summary>
        /// The type of computation to be performed
        /// </summary>
        public string? ComputeType { get; }
        /// <summary>
        /// The source text that represents the binding if from a BIND statement
        /// </summary>
        public string? Text { get; set; }
        /// <summary>
        /// Create a Binding for the Attribute of the Object at Index in the Token
        /// </summary>
        /// <param name="index"></param>
        /// <param name="attribute"></param>
        public Binding(int index, string attribute)
        {
            ObjectIndex = index;
            Attribute = attribute;
            isBind = false;
            isComputation = false;
            ComputeType = "";
            Text = "";
        }

        /// <summary>
        ///Create a Binding for a Bind statement, we bind an Attribute to another variable
        /// </summary>
        /// <param name="var"></param>
        public Binding(string var)
        {
            Attribute = var.ToUpper();
            isBind = true;
            isComputation = false;
        }

        /// <summary>
        /// Create a Binding for a Computation, passed as an array of computation steps
        /// </summary>
        /// <param name="computation"></param>
        public Binding(List<string> computation, string computeType)
        {
            //For a Bind statement, we can bind to another variable with an optional computation
            isBind = true;
            isComputation = true;
            Computation = computation.ToArray();
            if (Computation.Length > 1 && Computation[0] == "CALC")
                Computation = Computation.Skip(1).ToArray();
            for (int x = 0; x < Computation.Length; x++)
                if (Computation[x].StartsWith("<"))
                    Computation[x] = Computation[x].ToUpper();
            ComputeType = computeType.ToUpper();
            Attribute = "";
        }
    }
}
