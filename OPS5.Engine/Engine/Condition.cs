using System;
using System.Collections.Generic;

using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    /// <summary>
    /// A Condition represents a line in the LHS of a Rule
    /// and contains a list of Tests to be performed
    /// </summary>
    public class Condition
    {
        /// <summary>
        /// The Class of Object to be matched
        /// </summary>
        public string ClassName
        {
            get
            {
                return _className;
            }
            set
            {
                _className = value.ToUpper();
            }
        }
        private string _className = string.Empty;

        /// <summary>
        /// The position in the LHS of the rule of this condition
        /// </summary>
        public int Order
        {
            get {
                return _order;
                }
            set
            {
                if (_order == 0)
                    _order = value;
            }
        }
        private int _order = 0;
        /// <summary>
        /// Indicates that this is a Negative Condition (must not match)
        /// </summary>
        public bool Negative { get; set; }
        /// <summary>
        /// The actual text for the condition from the source
        /// </summary>
        public string ConditionText
        {
            get { return _conditionText; }
            set
            {
                if (_conditionText == null)
                    _conditionText = value;
            }
        }
        private string _conditionText = string.Empty;
        /// <summary>
        /// List of COnditionTests to be performed
        /// </summary>
        public List<ConditionTest> Tests { get; set; } = new List<ConditionTest>();
        /// <summary>
        /// Indicates that the condition should only pass on the first Object that matches into the token in the Beta Node
        /// </summary>
        public bool IsAny { get; set; }
        /// <summary>
        /// Optional alias for this condition, used to reference it by name on the RHS instead of by number
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// Creates a Condition, accepts the integer position in the LHS and the text of the LHS line
        /// </summary>
        /// <param name="order"></param>
        /// <param name="line"></param>
        public Condition(int order, string className, string line, bool negative)
        {
            Order = order;
            ClassName = className;
            ConditionText = line;
            Negative = negative;
        }
    }
}
