using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OPS5.Engine.Calculators
{
    internal class CheckFactory : ICheckFactory
    {
        private ICheck _check;
        public CheckFactory(ICheck check)
        {
            _check = check;
        }

        public ICheck GetCheck(string check)
        {
            _check.SetProperties(check);
            return _check;
        }
    }
    internal class Check : ICheck
    {
        private readonly IUtils _parserUtils;
        private readonly IOPS5Logger _logger;

        private char[] TrimChars { get; } = new char[] { ' ', '\t', '\n' };
        private char[] TrimPar { get; } = new char[] { '(', ')' };
        private List<object> Checks = new List<object>();

        public Check(IOPS5Logger logger, IUtils parserUtils)
        {
            _logger = logger;
            _parserUtils = parserUtils;
        }

        public void SetProperties(string check)
        {
            check = check.Trim(TrimChars);
            string pattern = "\\(.*?\\)";

            bool unDone = true;
            while (unDone)
            {
                Match match = Regex.Match(check, pattern);
                if (match.Success)
                {
                    if (match.Index > 0)
                        Checks.Add(check.Substring(0, match.Index).Trim(TrimChars));
                    Checks.Add(match.Value.Trim(TrimChars).Trim(TrimPar));
                    check = check.Substring(match.Index + match.Length).Trim(TrimChars);
                }
                else
                {
                    unDone = false;
                    check = check.Trim(TrimChars);
                    if (check.Length > 0 && check != ";")
                        Checks.Add(check);
                }
            }


            for (int p = 0; p < Checks.Count; p++)
            {
                if (Checks[p] is string thisCheck && thisCheck.Contains("||") && thisCheck.ToString() != "||")
                {
                    List<string> oChecks = new List<string>();
                    pattern = ".*?||";
                    unDone = true;
                    while (unDone)
                    {
                        Match match = Regex.Match(thisCheck, pattern);
                        if (match.Success)
                        {
                            oChecks.Add(match.Value.Substring(0, match.Length - 2).Trim(TrimChars));
                            oChecks.Add("||");
                            thisCheck = thisCheck.Substring(match.Index + match.Length).Trim(TrimChars);
                        }
                        else
                        {
                            unDone = false;
                            thisCheck = thisCheck.Trim(TrimChars);
                            if (thisCheck.Length > 0)
                                oChecks.Add(thisCheck);
                        }
                    }
                    Checks[p] = oChecks;
                }
            }

            for (int p = 0; p < Checks.Count; p++)
            {
                if(Checks[p] is string thisCheck)
                {
                    if (thisCheck.Contains("&&") && thisCheck.ToString() != "&&")
                    {
                        List<object> oChecks = new List<object>();
                        pattern = ".*?&&";
                        unDone = true;
                        while (unDone)
                        {
                            Match match = Regex.Match(thisCheck, pattern);
                            if (match.Success)
                            {
                                oChecks.Add(match.Value.Substring(0, match.Length - 2).Trim(TrimChars));
                                oChecks.Add("&&");
                                thisCheck = thisCheck.Substring(match.Index + match.Length).Trim(TrimChars);
                            }
                            else
                            {
                                unDone = false;
                                thisCheck = thisCheck.Trim(TrimChars);
                                if (thisCheck.Length > 0)
                                    oChecks.Add(thisCheck);
                            }
                        }
                        Checks[p] = oChecks;
                    }
                }
                else 
                {
                    List<object> oChecks = (List<object>)Checks[p];
                    for (int o = 0; o < oChecks.Count; o++)
                    {
                        if (oChecks[o] is string thisCheck2)
                        {
                            if (thisCheck2.Contains("&&") && thisCheck2.ToString() != "&&")
                            {
                                List<string> aChecks = new List<string>();
                                pattern = ".*?&&";
                                unDone = true;
                                while (unDone)
                                {
                                    Match match = Regex.Match(thisCheck2, pattern);
                                    if (match.Success)
                                    {
                                        aChecks.Add(match.Value.Substring(0, match.Length - 2).Trim(TrimChars));
                                        aChecks.Add("&&");
                                        thisCheck2 = thisCheck2.Substring(match.Index + match.Length).Trim(TrimChars);
                                    }
                                    else
                                    {
                                        unDone = false;
                                        thisCheck2 = thisCheck2.Trim(TrimChars);
                                        if (thisCheck2.Length > 0)
                                            aChecks.Add(thisCheck2);
                                    }
                                }
                                oChecks[o] = aChecks;
                            }
                        }
                    }
                }
            }
        }

        public bool Evaluate(string val, IToken token)
        {
            return EvalChecks(val, Checks, token);
        }

        private bool EvalChecks(string val, List<object> checks, IToken token)
        {
            bool result = true;

            try
            {
                foreach (var check in checks)
                {
                    if (check is string)
                    {
                        if (check.ToString() == "&&")
                        {
                            if (result == false)
                                return false;
                        }
                        else if (check.ToString() == "||")
                        {
                            if (result == true)
                                return true;
                        }
                        else
                        {
                            List<string> atoms = _parserUtils.ParseCommand(" " + check.ToString()); //Add space to ensure initial operator is caught
                            string value = token.TryGetVariableValue(atoms[1]);

                            switch (atoms[0])
                            {
                                case "=":
                                    if (val != value)
                                        result = false;
                                    break;
                                case "!=":
                                case "<>":
                                    if (val == value)
                                        result = false;
                                    break;
                                case ">":
                                    if (decimal.TryParse(val, out decimal dval) && decimal.TryParse(value, out decimal dvalue))
                                    {
                                        if (dval <= dvalue)
                                            result = false;
                                    }
                                    else
                                        result = false;
                                    break;
                                case "<":
                                    if (decimal.TryParse(val, out decimal dval1) && decimal.TryParse(value, out decimal dvalue1))
                                    {
                                        if (dval1 >= dvalue1)
                                            result = false;
                                    }
                                    else
                                        result = false;
                                    break;
                                case ">=":
                                    if (decimal.TryParse(val, out decimal dval2) && decimal.TryParse(value, out decimal dvalue2))
                                    {
                                        if (dval2 < dvalue2)
                                            result = false;
                                    }
                                    else
                                        result = false;
                                    break;
                                case "<=":
                                    if (decimal.TryParse(val, out decimal dval3) && decimal.TryParse(value, out decimal dvalue3))
                                    {
                                        if (dval3 > dvalue3)
                                            result = false;
                                    }
                                    else
                                        result = false;
                                    break;

                                default:
                                    result = false;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        result = EvalChecks(val, (List<object>)check, token);
                    }
                }
            }
            catch (Exception)
            {
                _logger.WriteError($"Invalid syntax in check statement, checking {val}", "Check");
                result = false;
            }

            return result;
        }


    }

}
