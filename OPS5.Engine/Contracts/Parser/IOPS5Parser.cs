using OPS5.Engine.Parsers.OPS5;

namespace OPS5.Engine.Contracts.Parser
{
    /// <summary>
    /// Parses OPS5 S-expression syntax directly into engine model objects.
    /// </summary>
    internal interface IOPS5Parser
    {
        OPS5ParseResult Parse(string ops5Text, string fileName);
    }
}
