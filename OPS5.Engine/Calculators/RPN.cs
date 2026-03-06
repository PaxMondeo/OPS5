using System;
using System.Collections.Generic;
using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;

namespace OPS5.Engine.Calculators
{
    /// <summary>
    /// Reverse Polish Notation calculator class
    /// </summary>
    internal class RPN : Calculator , ICalculator 
    {
        private IOPS5Logger _logger;
        /// <summary>
        /// X Register, which is used to enter data to and retreive results from the calculator
        /// </summary>
        public double xReg { get; set; }
        private Stack<double> stack;
        /// <summary>
        /// Constructor, initialises the calculator
        /// </summary>
        public RPN(IOPS5Logger logger, IUtils utils)
        {
            _logger = logger;
            Utils = utils;
            xReg = 0.0;
            stack = new Stack<double>();
        }

        public string CalcType()
        {
            return "RPN";
        }

        /// <summary>
        /// Pushes the contents of the X register on to the stack
        /// </summary>
        public void Enter()
        {
            stack.Push(xReg);
        }

        private double Pop()
        {
            if (stack.Count == 0)
                return 0.0;
            else
                return stack.Pop();
        }

        /// <summary>
        /// Adds the contents of the X register to the last value on the stack, popping that value off the stack. 
        /// Leaves the result in the X register
        /// </summary>
        public void Add()
        {

            xReg = Pop() + xReg;
        }

        /// <summary>
        /// Subtracts the contents of the X register from the last value on the stack, popping that value off the stack. 
        /// Leaves the result in the X register
        /// </summary>
        public void Subtract()
        {
            xReg = Pop() - xReg;
        }

        /// <summary>
        /// Multiplies the contents of the X register by the last value on the stack, popping that value off the stack. 
        /// Leaves the result in the X register
        /// </summary>
        public void Multiply()
        {
            xReg = Pop() * xReg;
        }

        /// <summary>
        /// Divides the last value on the stack by the contents of the X register, popping that value off the stack. 
        /// Leaves the result in the X register
        /// </summary>
        public void Divide()
        {
            if(xReg != 0)
                xReg = Pop() / xReg;
        }

        /// <summary>
        /// Inverts the value in the X register 
        /// </summary>
        public void Invert()
        {
            if (xReg != 0)
                xReg = 1.0 / xReg;
        }

        /// <summary>
        /// Finds the square of the value in the X reg, placing the result in the X reg.
        /// </summary>
        public void Sq()
        {
            xReg = xReg * xReg;
        }

        /// <summary>
        /// Finds the square root of the value in the X reg, placing result in X reg.
        /// </summary>
        public void Sqrt()
        {
            xReg = Math.Sqrt(xReg);
        }

        //Pops the last value from the stack and raises it to the power of the value of the X reg, placing the result in the X reg.
        public void YtoX()
        {
            xReg = Math.Pow(Pop(), xReg);
        }

        /// <summary>
        /// Raises e to the power of the value of the X reg, placing the result in the X reg.
        /// </summary>
        public void EtoX()
        {
            xReg = Math.Exp(xReg);
        }

        /// <summary>
        /// Raises 10 to the power of the value of the X reg, placing the result in the X reg.
        /// </summary>
        public void TentoX()
        {
            xReg = Math.Pow(10, xReg);
        }

        /// <summary>
        /// Finds the Absolute value of the X Register, placing result in X reg.
        /// </summary>
        public void Abs()
        {
            xReg = Math.Abs(xReg);
        }

        /// <summary>
        /// Finds the Arc Cosine of the value in the X reg, placing the result in the X reg.
        /// </summary>
        public void Acos()
        {
            xReg = Math.Acos(xReg);
        }

        /// <summary>
        /// Finds the Arc Sine of the value in the X reg, placing the result in the X reg.
        /// </summary>
        public void Asin()
        {
            xReg = Math.Asin(xReg);
        }

        /// <summary>
        /// Finds the Arc Tangent of the value in the X reg, placving the result in the X reg.
        /// </summary>
        public void Atan()
        {
            xReg = Math.Atan(xReg);
        }

        /// <summary>
        /// Finds the Cosine of the value in the X reg, placving the result in the X reg.
        /// </summary>
        public void Cos()
        {
            xReg = Math.Cos(xReg);
        }

        /// <summary>
        /// Finds the base 10 logarithm of the value in the X reg, placing the result in the X reg.
        /// </summary>
        public void Log()
        {
            xReg = Math.Log10(xReg);
        }

        /// <summary>
        /// Finds the natural logarithm of the value in the X reg, placing the result in the X reg.
        /// </summary>
        public void Ln()
        {
            xReg = Math.Log(xReg);
        }

        /// <summary>
        /// Pops the last value of the stack and compares it with the value in the X reg placing the result in the X reg.
        /// </summary>
        public void Max()
        {
            xReg = Math.Max(xReg, Pop());
        }

        /// <summary>
        /// Pops the last value of the stack and compares it with the value in the X reg placing the result in the X reg.
        /// </summary>
        public void Min()
        {
            xReg = Math.Min(xReg, Pop());
        }

        /// <summary>
        /// Removes the fractional part of the value in the X reg.
        /// </summary>
        public void Trunc()
        {
            xReg = Math.Truncate(xReg);
        }

        /// <summary>
        /// Finds the Sine of the value in the X reg, placving the result in the X reg.
        /// </summary>
        public void Sin()
        {
            xReg = Math.Sin(xReg);
        }

        /// <summary>
        /// Finds the Tangent of the value in the X reg, placving the result in the X reg.
        /// </summary>
        public void Tan()
        {
            xReg = Math.Tan(xReg);
        }

        /// <summary>
        /// Exchanges the value in the X reg with the last bvalue on the stack.
        /// </summary>
        public void XY()
        {
            double temp = xReg;
            xReg = Pop();
            stack.Push(temp);
        }

        /// <summary>
        /// Replaces the value in the X reg with the last value popped from the stack
        /// </summary>
        public void Rdn()
        {
            xReg = Pop();
        }

        /// <summary>
        /// Clears the X reg
        /// </summary>
        public void ClrX()
        {
            xReg = 0.0;
        }

        /// <summary>
        /// Clears the X reg and the stack
        /// </summary>
        public void ClrAll()
        {
            xReg = 0.0;
            stack.Clear();
        }

        /// <summary>
        /// Rounds Y to X decmimal places
        /// </summary>
        public void Rnd()
        {
            xReg = (double)Math.Round((decimal)Pop(), (int)xReg);
        }

        /// <summary>
        /// Accepts and RPN calculation as a string commands and returns a double result, leaving the result in the X reg.
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public override string Calc(List<string> commands)
        {
            foreach (string cmd in commands)
            {
                if (cmd.Contains(","))
                {
                    //This should be a vector
                    string[] atoms = cmd.Split(',');
                }
                else
                {
                    switch (cmd.ToUpper())
                    {
                        case "^":
                            Enter();
                            break;

                        case "+":
                            Add();
                            break;

                        case "-":
                            Subtract();
                            break;

                        case "*":
                            Multiply();
                            break;

                        case "/":
                            Divide();
                            break;

                        case "1/X":
                            Invert();
                            break;

                        case "SQRT":
                            Sqrt();
                            break;

                        case "^*":
                            Sq();
                            break;

                        case "V":
                            Rdn();
                            break;

                        case "X<>Y":
                            XY();
                            break;

                        case "Y^X":
                            YtoX();
                            break;

                        case "E^X":
                            EtoX();
                            break;

                        case "10^X":
                            TentoX();
                            break;

                        case "LOG":
                            Log();
                            break;

                        case "LN":
                            Ln();
                            break;

                        case "ABS":
                            Abs();
                            break;

                        case "ACOS":
                            Acos();
                            break;

                        case "ASIN":
                            Asin();
                            break;

                        case "ATAN":
                            Atan();
                            break;

                        case "COS":
                            Cos();
                            break;

                        case "SIN":
                            Sin();
                            break;

                        case "TAN":
                            Tan();
                            break;

                        case "MAX":
                            Max();
                            break;

                        case "MIN":
                            Min();
                            break;

                        case "INT":
                            Trunc();
                            break;

                        case "CLRX":
                            ClrX();
                            break;

                        case "CLRA":
                            ClrAll();
                            break;

                        case "POP":
                            //Do nothing - just return xReg
                            break;

                        case "RND":
                            Rnd();
                            break;

                        default:
                            double temp;
                            if (double.TryParse(cmd, out temp))
                            {
                                stack.Push(xReg);
                                xReg = temp;
                            }
                            else
                            {
                                _logger.WriteError($"Invalid number {cmd}", "Calc");
                                return "ERR";
                            }
                            break;
                    }
                }
            }


            return xReg.ToString();
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
                case "^":
                case "1/X":
                case "SQRT":
                case "^*":
                case "V":
                case "X<>Y":
                case "Y^X":
                case "E^X":
                case "10^X":
                case "LOG":
                case "LN":
                case "ABS":
                case "ACOS":
                case "ASIN":
                case "ATAN":
                case "COS":
                case "SIN":
                case "TAN":
                case "MAX":
                case "MIN":
                case "INT":
                case "CLRX":
                case "CLRA":
                case "POP":
                case "RND":
                    result = true;
                    break;

                default:
                    _logger.WriteError($"Calculation included invalid command {cmd}", "RPN");
                    break;
            }

            return result;
        }
    }
}
