using System.Collections.Generic;
using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;

namespace OPS5.Engine.Calculators
{
    internal class LegacyCalculator : Calculator, ICalculator
    {
        public LegacyCalculator(IUtils utils)
        {
            Utils = utils;
        }

        public string CalcType()
        {
            return "Legacy";
        }


        public override string Calc(List<string> input)
        {
            double calc = 0.0;
            string op = "";

            foreach(string cmd in input)
            {
                switch (cmd)
                {
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                    case "//":
                        op = cmd;
                        break;

                    default:
                        //Must be a constant
                        double val = 0.0;
                        double.TryParse(cmd, out val);
                        switch (op)
                        {
                            case "+":
                                calc += val;
                                break;

                            case "-":
                                calc -= val;
                                break;

                            case "*":
                                calc *= val;
                                break;

                            case "/":
                                calc /= val;
                                break;

                            case "//":
                                calc %= val;
                                break;

                            case "":
                                calc = val;
                                break;
                        }
                        break;
                }
            }


            return calc.ToString();
        }

        public override bool ValidCommand(string cmd)
        {
            bool result = false;

            switch (cmd.ToUpper())
            {
                case "+":
                case "-":
                case "/":
                case "*":
                case "//":
                case "=":
                    result = true;
                    break;
            }

            return result;
        }

    }
}
