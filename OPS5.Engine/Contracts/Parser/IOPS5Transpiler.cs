using OPS5.Engine.Parsers.OPS5;

namespace OPS5.Engine.Contracts.Parser
{
    /// <summary>
    /// Transpiles OPS5 syntax into OPS5 engine text-format strings.
    /// </summary>
    internal interface IOPS5Transpiler
    {
        OPS5TranspileResult Transpile(string ops5Text, string fileName);
    }
}
