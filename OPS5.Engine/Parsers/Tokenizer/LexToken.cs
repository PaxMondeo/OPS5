namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// Represents a lexical token produced by the OPS5 internal lexer.
    /// Named 'LexToken' to avoid collision with the RETE engine's Token type.
    /// </summary>
    public readonly record struct LexToken
    {
        /// <summary>The classified type of this token.</summary>
        public TokenType Type { get; init; }

        /// <summary>
        /// The token value. For string literals: content without quotes.
        /// For variables: content without angle brackets. For keywords: original casing.
        /// </summary>
        public string Value { get; init; }

        /// <summary>1-based line number in the source file.</summary>
        public int Line { get; init; }

        /// <summary>1-based column number in the source file.</summary>
        public int Column { get; init; }

        /// <summary>0-based absolute offset from start of source.</summary>
        public int Offset { get; init; }

        /// <summary>Length of the token in source characters.</summary>
        public int Length { get; init; }

        public LexToken(TokenType type, string value, int line, int column, int offset, int length)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
            Offset = offset;
            Length = length;
        }

        public override string ToString() => $"{Type}({Value}) at {Line}:{Column}";

        public static LexToken Eof(int line, int column, int offset) =>
            new(TokenType.EOF, "", line, column, offset, 0);
    }
}
