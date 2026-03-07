namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// All token types recognized by the OPS5 lexer.
    /// </summary>
    public enum TokenType
    {
        // === Literals ===
        StringLiteral,         // "quoted string" (Value = content without quotes)
        IntegerLiteral,        // 42, -5
        DecimalLiteral,        // 3.14, -2.5
        Identifier,            // unquoted names: class names, attribute names, values

        // === Variables ===
        Variable,              // <varName> (Value = "varName" without angle brackets)

        // === Operators ===
        Equals,                // =
        NotEquals,             // != or <>
        LessThan,              // <
        GreaterThan,           // >
        LessOrEqual,           // <=
        GreaterOrEqual,        // >=
        Arrow,                 // -->
        Plus,                  // +
        Minus,                 // -
        Star,                  // *
        Slash,                 // /
        Backslash,             // \

        // === Punctuation ===
        LeftParen,             // (
        RightParen,            // )
        LeftBrace,             // {
        RightBrace,            // }
        DoubleLeftAngle,       // <<
        DoubleRightAngle,      // >>

        // === OPS5 ===
        Caret,                 // ^ (attribute prefix in OPS5)

        // === Special ===
        EOF,                   // End of input
        Error                  // Unrecognized character
    }
}
