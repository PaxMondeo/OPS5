using System.Collections.Generic;

namespace OPS5.Engine.Contracts.Parser
{
    internal interface IUtils
    {
        char[] TrimChars { get; }
        public List<string> ParseCommand(string line);
        public string RemoveComments(string file);
    }
}
