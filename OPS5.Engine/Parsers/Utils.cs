using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OPS5.Engine.Parsers
{
    internal class Utils : IUtils
    {
        /// <summary>
        /// character array containing white space characters
        /// </summary>
        public char[] TrimChars { get; } = new char[] { ' ', '\t', '\n', '\r' };

        private readonly IOPS5Logger _logger;
        private ISourceFiles _sourceFiles;

        public Utils(IOPS5Logger logger,
                     ISourceFiles sourceFiles)
        {
            _logger = logger;
            _sourceFiles = sourceFiles;
        }

        public List<string> ParseCommand(string line)
        {
            List<string> command = new List<string>();

            List<string> quoted = new List<string>();
            List<string> conjuncted = new List<string>();
            List<string> disjuncted = new List<string>();
            List<CalcInfo> calcs = new List<CalcInfo>();

            try
            {
                //Replace escaped quotes
                line = line.Replace("\\\"", "[OPS5Quote]");

                ExtractAndReplace(ref line, "\".*?\"", quoted, "Quoted",
                    m => m.Value.Replace("\"", ""));

                //Calcs require custom handling (CalcInfo objects with type and content)
                ExtractCalcs(ref line, calcs);

                ExtractAndReplace(ref line, "{.*?}", conjuncted, "Conjuncted",
                    m => m.Value.Replace("{", "").Replace("}", ""));

                ExtractAndReplace(ref line, "<<.*?>>", disjuncted, "Disjuncted",
                    m => m.Value.Replace("<<", "").Replace(">>", ""));

                //Tokenise the line
                string pattern = @"(?i)(\()|(\))|(<[^<^\s]*?>)|(\.*?\b[^\s^\(]+\b)|(=\s)|(\s>\s)|(\s<\s)|(\s>=\s)|(\s<=\s)|(\s<>\s*)|(\s!=\s)|([^a-z^A-Z^0-9]>[^=]\s?)|(<[^a-z^A-Z^0-9^=]\s?)|(\s\+)|(\s/)|(\s\*)|(\s-)|(\s\\)|(!IN\s)";
                Match match = Regex.Match(line, pattern);
                List<string> values = new List<string>();
                while (match.Success)
                {
                    string value = match.Value.Trim(TrimChars);
                    match = match.NextMatch();
                    if (!(value == "(" || value == ")"))
                    {
                        if (value == "-" && match.Success)
                        {
                            if (double.TryParse(match.Value, out double x))
                            {
                                value += match.Value;
                                match = match.NextMatch();
                            }
                            values.Add(value);
                        }
                        else
                            values.Add(value);
                    }
                }
                for (int x = 0; x < values.Count; x++)
                {
                    if (values[x].StartsWith("Quoted") && values[x].Count() > 6)
                    {
                        if (int.TryParse(values[x].Substring(6), out int y))
                            command.Add(quoted[y]);
                        else
                            command.Add(values[x]);
                    }
                    else if (values[x].StartsWith("Conjuncted") && values[x].Count() > 10)
                    {
                        if (int.TryParse(values[x].Substring(10), out int y))
                        {
                            command.Add("CONJUNCTION");
                            string conj = conjuncted[y];
                            if (conj.Contains("Quoted"))
                            {
                                for (int z = 0; z < quoted.Count; z++)
                                {
                                    if (conj.Contains($"Quoted{z}"))
                                    {
                                        conj = conj.Replace($"Quoted{z}", "\"" + quoted[z] + "\"");
                                        break;
                                    }
                                }
                            }

                            command.Add(conj);
                        }
                        else
                            command.Add(values[x]);
                    }
                    else if (values[x].StartsWith("Disjuncted") && values[x].Count() > 10)
                    {
                        if (int.TryParse(values[x].Substring(10), out int y))
                        {
                            command.Add("DISJUNCTION");
                            string disj = disjuncted[y];
                            if (disj.Contains("Quoted"))
                            {
                                for (int z = 0; z < quoted.Count; z++)
                                {
                                    if (disj.Contains($"Quoted{z}"))
                                    {
                                        disj = disj.Replace($"Quoted{z}", "\"" + quoted[z] + "\"");
                                        break;
                                    }
                                }
                            }

                            command.Add(disj);
                        }
                        else
                            command.Add(values[x]);
                    }
                    else if (values[x].StartsWith("Calc") && values[x].Count() > 4)
                    {
                        if (int.TryParse(values[x].Substring(4), out int y))
                        {
                            command.Add(calcs[y].CalcType.ToUpper());
                            string calc = calcs[y].Calc;
                            command.Add(calc);
                        }
                        else
                            command.Add(values[x]);
                    }
                    else
                        command.Add(values[x]);
                }

                for (int cmdX = 0; cmdX < command.Count; cmdX++)
                    command[cmdX] = command[cmdX].Replace("[OPS5Quote]", "\"");

            }
            catch (Exception ex)
            {
                _logger.WriteError($"{ex.Message} in line {line}", "Parsing Command");
            }



            return command;

        }

        public string RemoveComments(string file)
        {
            return Regex.Replace(file, "//.*", "", RegexOptions.Multiline);
        }

        /// <summary>
        /// Matches a pattern in the line, extracts values into a list, and replaces matches
        /// with numbered placeholders. Supports being called multiple times with the same
        /// list for multi-pattern extraction (e.g., vectors use bracket and keyword syntax).
        /// </summary>
        private void ExtractAndReplace(ref string line, string pattern, List<string> extracted,
            string placeholderPrefix, Func<Match, string> valueExtractor)
        {
            List<int> indices = new List<int>();
            List<int> lengths = new List<int>();
            int startCount = extracted.Count;

            Match match = Regex.Match(line, pattern);
            while (match.Success)
            {
                extracted.Add(valueExtractor(match));
                indices.Add(match.Index);
                lengths.Add(match.Length);
                match = match.NextMatch();
            }

            int addedCount = extracted.Count - startCount;
            if (addedCount > 0)
            {
                for (int x = addedCount - 1; x >= 0; x--)
                {
                    line = line.Remove(indices[x], lengths[x]);
                    line = line.Insert(indices[x], $"{placeholderPrefix}{startCount + x}");
                }
            }
        }

        /// <summary>
        /// Extracts Calc expressions which require custom handling (CalcInfo objects
        /// with separate type and content fields, and index/length offsets).
        /// </summary>
        private static void ExtractCalcs(ref string line, List<CalcInfo> calcs)
        {
            List<int> indices = new List<int>();
            List<int> lengths = new List<int>();

            string pattern = @"(?i)( Calc)\s*\(.*?\)";
            Match match = Regex.Match(line, pattern);
            while (match.Success)
            {
                CalcInfo calc = new CalcInfo();
                indices.Add(match.Index + 1);
                lengths.Add(match.Length - 1);
                calc.Calc = match.Value;
                string innerPattern = @"(?i)(Calc)\s*\(";
                Match match2 = Regex.Match(calc.Calc, innerPattern);
                calc.Calc = calc.Calc.Substring(match2.Length).Replace(")", "").Replace("(", "");
                calc.CalcType = match2.Value.Substring(0, match2.Length - 1).Trim();
                calcs.Add(calc);
                match = match.NextMatch();
            }
            if (calcs.Count > 0)
            {
                for (int x = calcs.Count - 1; x >= 0; x--)
                {
                    line = line.Remove(indices[x], lengths[x]);
                    line = line.Insert(indices[x], $"Calc{x}");
                }
            }
        }



    }
}
