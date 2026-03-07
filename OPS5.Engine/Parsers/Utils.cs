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

            List<string> formatted = new List<string>();
            List<string> quoted = new List<string>();
            List<string> conjuncted = new List<string>();
            List<string> disjuncted = new List<string>();
            List<CalcInfo> calcs = new List<CalcInfo>();
            List<string> wheres = new List<string>();
            List<string> vectors = new List<string>();
            List<string> vectorAppends = new List<string>();
            List<string> vectorRemoves = new List<string>();
            List<string> splits = new List<string>();

            try
            {
                //Replace escaped quotes
                line = line.Replace("\\\"", "[OPS5Quote]");

                //Extract constructs and replace with numbered placeholders
                ExtractAndReplace(ref line, "\\$\".*?\"", formatted, "Formatted",
                    m => m.Value.Replace("\"", "").Replace("$", ""));

                ExtractAndReplace(ref line, "\".*?\"", quoted, "Quoted",
                    m => m.Value.Replace("\"", ""));

                //Calcs require custom handling (CalcInfo objects with type and content)
                ExtractCalcs(ref line, calcs);

                ExtractAndReplace(ref line, "(?i)Where\\s*\\(.*?\\)", wheres, "Where",
                    m => StripKeywordPrefix(m, "(?i)Where\\s*\\("));

                //Vectors: two patterns feed the same list (bracket syntax and keyword syntax)
                ExtractAndReplace(ref line, "(?i)\\[.*?\\]", vectors, "Vector",
                    m => m.Value);
                ExtractAndReplace(ref line, "(?i)Vector\\s*\\(.*?\\)", vectors, "Vector",
                    m => StripKeywordPrefix(m, "(?i)Vector\\s*\\("));

                ExtractAndReplace(ref line, "(?i)Vector.Append\\s*\\(.*?\\)", vectorAppends, "Vector.Append",
                    m => StripKeywordPrefix(m, "(?i)Vector.Append\\s*\\("));

                ExtractAndReplace(ref line, "(?i)Vector.Remove\\s*\\(.*?\\)", vectorRemoves, "Vector.Remove",
                    m => StripKeywordPrefix(m, "(?i)Vector.Remove\\s*\\("));

                ExtractAndReplace(ref line, "(?i)Split\\s*\\(.*?\\)", splits, "Split",
                    m => StripKeywordPrefix(m, "(?i)Split\\s*\\("));

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
                    else if (values[x].StartsWith("Formatted") && values[x].Count() > 9)
                    {
                        if (int.TryParse(values[x].Substring(9), out int y))
                        {
                            command.Add("FORMATTED");
                            command.Add(formatted[y]);
                        }
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
                    else if (values[x].StartsWith("Where") && values[x].Count() > 5)
                    {
                        if (int.TryParse(values[x].Substring(5), out int y))
                        {
                            command.Add("WHERE");
                            string where = wheres[y];
                            command.Add(where);
                        }
                        else
                            command.Add(values[x]);
                    }
                    else if (values[x].StartsWith("Vector.Append") && values[x].Count() > 13)
                    {
                        if (int.TryParse(values[x].Substring(13), out int y))
                        {
                            command.Add("VECTOR.APPEND");
                            string vector = vectorAppends[y];
                            if (vector.Contains("Quoted"))
                            {
                                for (int z = 0; z < quoted.Count; z++)
                                {
                                    if (vector.Contains($"Quoted{z}"))
                                    {
                                        vector = vector.Replace($"Quoted{z}", quoted[z]);
                                        break;
                                    }
                                }
                            }
                            command.Add(vector);
                        }
                        else
                            command.Add(values[x]);
                    }
                    else if (values[x].StartsWith("Vector.Remove") && values[x].Count() > 13)
                    {
                        if (int.TryParse(values[x].Substring(13), out int y))
                        {
                            command.Add("VECTOR.REMOVE");
                            string vector = vectorRemoves[y];
                            if (vector.Contains("Quoted"))
                            {
                                for (int z = 0; z < quoted.Count; z++)
                                {
                                    if (vector.Contains($"Quoted{z}"))
                                    {
                                        vector = vector.Replace($"Quoted{z}", quoted[z]);
                                        break;
                                    }
                                }
                            }
                            command.Add(vector);
                        }
                        else
                            command.Add(values[x]);
                    }
                    else if (values[x].StartsWith("Vector") && values[x].Count() > 6)
                    {
                        if (int.TryParse(values[x].Substring(6), out int y))
                        {
                            command.Add("VECTOR");
                            string vector = vectors[y];
                            if (vector.Contains("Quoted"))
                            {
                                for (int z = 0; z < quoted.Count; z++)
                                {
                                    if (vector.Contains($"Quoted{z}"))
                                    {
                                        vector = vector.Replace($"Quoted{z}", quoted[z]);
                                        break;
                                    }
                                }
                            }
                            command.Add(vector);
                        }
                        else
                            command.Add(values[x]);
                    }
                    else if (values[x].StartsWith("Split") && values[x].Count() > 5)
                    {
                        if (int.TryParse(values[x].Substring(5), out int y))
                        {
                            command.Add("SPLIT");
                            string split = splits[y];
                            if (split.Contains("Quoted"))
                            {
                                for (int z = 0; z < quoted.Count; z++)
                                {
                                    if (split.Contains($"Quoted{z}"))
                                    {
                                        split = split.Replace($"Quoted{z}", quoted[z]);
                                        break;
                                    }
                                }
                            }
                            command.Add(split);
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


        public void ParseComment(string comment, string fileName)
        {
            string fileComment = comment.Replace("////", "");
            string uFileName = fileName.ToUpper();
            if (fileComment != "")
            {
                if (uFileName.EndsWith(".OPS5"))
                    _sourceFiles.OPS5File.Comment += fileComment;
                else if (_sourceFiles.ClassFiles.ContainsKey(uFileName))
                    _sourceFiles.ClassFiles[uFileName].Comment += fileComment;
                else if (_sourceFiles.RuleFiles.ContainsKey(uFileName))
                    _sourceFiles.RuleFiles[uFileName].Comment += fileComment;
            }
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
        /// Extracts Calc/Add* expressions which require custom handling (CalcInfo objects
        /// with separate type and content fields, and index/length offsets).
        /// </summary>
        private static void ExtractCalcs(ref string line, List<CalcInfo> calcs)
        {
            List<int> indices = new List<int>();
            List<int> lengths = new List<int>();

            string pattern = @"(?i)( Calc| AddYears| AddMonths| AddWeeks| AddDays| AddHours| AddMins| AddSecs)\s*\(.*?\)";
            Match match = Regex.Match(line, pattern);
            while (match.Success)
            {
                CalcInfo calc = new CalcInfo();
                indices.Add(match.Index + 1);
                lengths.Add(match.Length - 1);
                calc.Calc = match.Value;
                string innerPattern = @"(?i)(Calc|AddYears|AddMonths|AddWeeks|AddDays|AddHours|AddMins|AddSecs)\s*\(";
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

        /// <summary>
        /// Strips a keyword prefix (e.g., "Where(", "Split(") from a match
        /// and removes the trailing parenthesis.
        /// </summary>
        private static string StripKeywordPrefix(Match match, string keywordPattern)
        {
            Match inner = Regex.Match(match.Value, keywordPattern);
            return match.Value.Substring(inner.Length).Replace(")", "");
        }

        public string UpToSemi(string line)
        {
            string result = line;

            string pattern = ";";
            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                result = line.Substring(0, match.Index);
            }
            else
                _logger.WriteError($"Expected ;  Found {line}", "Parser");

            return result;
        }

        public string UpToCmdEnd(string line)
        {
            string result = line;

            string pattern = "\\)\\s*;";
            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                result = line.Substring(0, match.Index + 1);
            }
            else
                _logger.WriteError($"Expected ;  Found {line}", "Parser");

            return result;
        }

        public int CountSemicolons(string file)
        {
            int count;

            MatchCollection matches = Regex.Matches(file, @";\s");
            count = matches.Count;
            matches = Regex.Matches(file, @";$");
            count += matches.Count;

            return count;
        }

        public int CountEndParentheses(string file)
        {
            int count;

            MatchCollection matches = Regex.Matches(file.Replace("))", ")"), @"\)");
            count = matches.Count;

            return count;
        }

        public bool ParseTime(string time)
        {
            return TimeSpan.TryParse(time, out TimeSpan _);
        }

        public bool ParseDate(string date)
        {
            return DateTime.TryParse(date, out DateTime _);
        }

        public bool ParseDay(string day)
        {
            bool dayOK = true;

            switch (day.ToUpper())
            {
                case "MON":
                case "TUE":
                case "WED":
                case "THU":
                case "FRI":
                case "SAT":
                case "SUN":

                    break;

                default:
                    dayOK = false;
                    break;
            }

            return dayOK;
        }



    }
}
