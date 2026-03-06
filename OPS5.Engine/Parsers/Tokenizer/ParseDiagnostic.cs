using System;
using System.Text;

namespace OPS5.Engine.Parsers.Tokenizer
{
    public enum DiagnosticSeverity
    {
        Error,
        Warning,
        Info
    }

    /// <summary>
    /// Rich diagnostic information for parser errors with source location.
    /// </summary>
    public class ParseDiagnostic
    {
        public DiagnosticSeverity Severity { get; init; }
        public string Message { get; init; } = "";
        public string FileName { get; init; } = "";
        public int Line { get; init; }
        public int Column { get; init; }
        public string? Context { get; init; }
        public string? Expected { get; init; }
        public string? Found { get; init; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{Severity}: {Message}");
            if (!string.IsNullOrEmpty(FileName))
                sb.Append($" in {FileName}");
            if (Line > 0)
                sb.Append($" at line {Line}, column {Column}");
            if (!string.IsNullOrEmpty(Context))
                sb.Append($" ({Context})");
            if (!string.IsNullOrEmpty(Expected) && !string.IsNullOrEmpty(Found))
                sb.Append($" [expected {Expected}, found {Found}]");
            return sb.ToString();
        }
    }

    public class ParseException : Exception
    {
        public ParseDiagnostic Diagnostic { get; }

        public ParseException(ParseDiagnostic diagnostic)
            : base(diagnostic.ToString())
        {
            Diagnostic = diagnostic;
        }
    }
}
