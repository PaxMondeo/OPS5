using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using System;
using System.Collections.Generic;

namespace OPS5.Engine.Calculators
{
    internal abstract class Calculator
    {
        protected static IUtils Utils { get; set; } = default!;

        public Calculator()
        {
        }
        public string DoCalc(string commands, IToken thisToken)
        {
            try
            {
                List<string> calculation = Utils.ParseCommand(commands);
                return DoCalc(calculation, thisToken);
            }
            catch (Exception)
            {
                throw new Exception($"Invalid calculation '{commands}'");
            }
        }

        public string DoCalc(List<string> commands, IToken thisToken)
        {
            bool calcOK = true;
            List<string> calculation = new List<string>();
            foreach (string item in commands)
            {
                string value = thisToken.TryGetVariableValue(item);
                if (double.TryParse(value, out double val))
                    calculation.Add(val.ToString());
                else
                {
                    if (ValidCommand(value))
                        calculation.Add(value.ToUpper());
                    else
                        calcOK = false;
                }
            }
            if (calcOK)
                return Calc(calculation);
            else
                throw new Exception("Invalid calculation");
        }

        public abstract bool ValidCommand(string value);

        public abstract string Calc(List<string> calculation);


    }
}
