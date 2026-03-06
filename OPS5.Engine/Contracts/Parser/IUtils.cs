using System.Collections.Generic;

namespace OPS5.Engine.Contracts.Parser
{
    internal interface IUtils
    {
        char[] TrimChars { get; }
        public List<string> ParseCommand(string line);
        public void ParseComment(string comment, string fileName);
        public string RemoveComments(string file);
        public int CountSemicolons(string file);
        int CountEndParentheses(string file);
        public string UpToCmdEnd(string line);
        public string UpToSemi(string line);
        public bool ParseTime(string time);
        public bool ParseDate(string date);
        public bool ParseDay(string day);
    }
}
