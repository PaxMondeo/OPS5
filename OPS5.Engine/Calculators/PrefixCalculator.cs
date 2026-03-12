using System;
using System.Collections.Generic;
using System.Linq;
using OPS5.Engine.Contracts;

namespace OPS5.Engine.Calculators
{
    /// <summary>
    /// Evaluates arithmetic expressions in prefix (Polish) notation,
    /// matching the natural syntax of OPS5 compute expressions.
    /// </summary>
    internal class PrefixCalculator : ICalculator
    {
        private readonly IOPS5Logger _logger;

        public PrefixCalculator(IOPS5Logger logger)
        {
            _logger = logger;
        }

        public string CalcType() => "PREFIX";

        public string Calc(List<string> tokens)
        {
            int index = 0;
            double result = Evaluate(tokens, ref index);
            return FormatResult(result);
        }

        public bool ValidCommand(string cmd)
        {
            return IsOperator(cmd);
        }

        public string DoCalc(string commands, IToken thisToken)
        {
            var tokens = commands.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            return DoCalc(tokens, thisToken);
        }

        public string DoCalc(List<string> commands, IToken thisToken)
        {
            var resolved = new List<string>();
            foreach (string item in commands)
            {
                string value = thisToken.TryGetVariableValue(item);
                if (double.TryParse(value, out double val))
                    resolved.Add(val.ToString());
                else if (IsOperator(value))
                    resolved.Add(value);
                else
                {
                    _logger.WriteError($"Invalid token in calculation: {value}", "Calc");
                    throw new Exception($"Invalid calculation: {value}");
                }
            }
            return Calc(resolved);
        }

        private double Evaluate(List<string> tokens, ref int index)
        {
            if (index >= tokens.Count)
            {
                _logger.WriteError("Unexpected end of expression", "Calc");
                return 0;
            }

            string token = tokens[index];
            index++;

            if (IsOperator(token))
            {
                double left = Evaluate(tokens, ref index);
                double right = Evaluate(tokens, ref index);

                return token switch
                {
                    "+" => left + right,
                    "-" => left - right,
                    "*" => left * right,
                    "/" => right != 0 ? left / right : 0,
                    "//" => right != 0 ? Math.Truncate(left / right) : 0,
                    "\\" => right != 0 ? Math.Truncate(left / right) : 0,
                    _ => 0
                };
            }

            if (double.TryParse(token, out double value))
                return value;

            _logger.WriteError($"Invalid token in expression: {token}", "Calc");
            return 0;
        }

        private static bool IsOperator(string token)
        {
            return token is "+" or "-" or "*" or "/" or "//" or "\\";
        }

        private static string FormatResult(double value)
        {
            if (value == Math.Truncate(value) && !double.IsInfinity(value) && !double.IsNaN(value))
                return ((long)value).ToString();
            return value.ToString();
        }
    }
}
