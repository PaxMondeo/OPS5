using OPS5.Engine.Parsers.Tokenizer;
using System.Collections.Generic;

namespace OPS5.Engine.Parsers.OPS5
{
    /// <summary>
    /// Lexer for OPS5 syntax. Produces LexToken instances using the shared TokenType enum.
    ///
    /// Key differences from the internal lexer:
    /// - ; starts a line comment (to end of line), NOT a statement terminator
    /// - ^ is a Caret token (attribute prefix)
    /// - |text| is a pipe-delimited string literal
    /// - Identifiers may contain hyphens (e.g., on-top-of)
    /// - No semicolon terminators; structure is S-expression based
    /// </summary>
    internal class OPS5Lexer
    {
        private readonly string _source;
        private readonly string _fileName;
        private int _pos;
        private int _line;
        private int _col;
        private readonly List<LexToken> _tokens = new();
        private readonly List<string> _diagnostics = new();

        public IReadOnlyList<string> Diagnostics => _diagnostics;

        public OPS5Lexer(string source, string fileName)
        {
            _source = source ?? "";
            _fileName = fileName;
            _pos = 0;
            _line = 1;
            _col = 1;
        }

        public List<LexToken> Tokenize()
        {
            while (_pos < _source.Length)
            {
                SkipWhitespace();
                if (_pos >= _source.Length)
                    break;

                char c = _source[_pos];

                switch (c)
                {
                    case ';':
                        SkipLineComment();
                        break;

                    case '(':
                        AddToken(TokenType.LeftParen, "(", 1);
                        break;

                    case ')':
                        AddToken(TokenType.RightParen, ")", 1);
                        break;

                    case '{':
                        AddToken(TokenType.LeftBrace, "{", 1);
                        break;

                    case '}':
                        AddToken(TokenType.RightBrace, "}", 1);
                        break;

                    case '^':
                        AddToken(TokenType.Caret, "^", 1);
                        break;

                    case '|':
                        ScanPipeString();
                        break;

                    case '<':
                        ScanAngleBracketOrVariable();
                        break;

                    case '>':
                        ScanGreaterThan();
                        break;

                    case '-':
                        ScanMinusOrArrow();
                        break;

                    case '=':
                        AddToken(TokenType.Equals, "=", 1);
                        break;

                    case '+':
                        AddToken(TokenType.Plus, "+", 1);
                        break;

                    case '*':
                        AddToken(TokenType.Star, "*", 1);
                        break;

                    case '/':
                        ScanSlash();
                        break;

                    case '\\':
                        AddToken(TokenType.Backslash, "\\", 1);
                        break;

                    default:
                        if (char.IsDigit(c))
                            ScanNumber();
                        else if (IsIdentifierStart(c))
                            ScanIdentifier();
                        else
                        {
                            _diagnostics.Add($"Unexpected character '{c}' at line {_line}, column {_col} in {_fileName}");
                            AddToken(TokenType.Error, c.ToString(), 1);
                        }
                        break;
                }
            }

            _tokens.Add(LexToken.Eof(_line, _col, _pos));
            return _tokens;
        }

        private void AddToken(TokenType type, string value, int length)
        {
            _tokens.Add(new LexToken(type, value, _line, _col, _pos, length));
            Advance(length);
        }

        private void Advance(int count)
        {
            for (int i = 0; i < count && _pos < _source.Length; i++)
            {
                if (_source[_pos] == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }
                _pos++;
            }
        }

        private char Peek(int offset = 0)
        {
            int idx = _pos + offset;
            return idx < _source.Length ? _source[idx] : '\0';
        }

        private void SkipWhitespace()
        {
            while (_pos < _source.Length && char.IsWhiteSpace(_source[_pos]))
            {
                if (_source[_pos] == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }
                _pos++;
            }
        }

        private void SkipLineComment()
        {
            // ; to end of line
            while (_pos < _source.Length && _source[_pos] != '\n')
                _pos++;
            _col = 1;
        }

        private void ScanPipeString()
        {
            // |text with spaces|
            int startLine = _line, startCol = _col, startPos = _pos;
            _pos++; _col++; // skip opening |

            int contentStart = _pos;
            while (_pos < _source.Length && _source[_pos] != '|')
            {
                if (_source[_pos] == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }
                _pos++;
            }

            string content = _source[contentStart.._pos];

            if (_pos < _source.Length)
            {
                _pos++; _col++; // skip closing |
            }
            else
            {
                _diagnostics.Add($"Unterminated pipe string starting at line {startLine}, column {startCol} in {_fileName}");
            }

            int length = _pos - startPos;
            _tokens.Add(new LexToken(TokenType.StringLiteral, content, startLine, startCol, startPos, length));
        }

        private void ScanAngleBracketOrVariable()
        {
            // Could be: <var>, <<, <=, <>, or <
            if (Peek(1) == '<')
            {
                // <<
                AddToken(TokenType.DoubleLeftAngle, "<<", 2);
            }
            else if (Peek(1) == '=')
            {
                // <=
                AddToken(TokenType.LessOrEqual, "<=", 2);
            }
            else if (Peek(1) == '>')
            {
                // <>
                AddToken(TokenType.NotEquals, "<>", 2);
            }
            else if (Peek(1) != '\0' && (char.IsLetterOrDigit(Peek(1)) || Peek(1) == '_'))
            {
                // Variable: <name>
                ScanVariable();
            }
            else
            {
                AddToken(TokenType.LessThan, "<", 1);
            }
        }

        private void ScanVariable()
        {
            int startLine = _line, startCol = _col, startPos = _pos;
            _pos++; _col++; // skip <

            int nameStart = _pos;
            while (_pos < _source.Length && _source[_pos] != '>' && _source[_pos] != '\n')
            {
                _pos++;
                _col++;
            }

            string name = _source[nameStart.._pos];

            if (_pos < _source.Length && _source[_pos] == '>')
            {
                _pos++; _col++; // skip >
            }
            else
            {
                _diagnostics.Add($"Unterminated variable '<{name}' at line {startLine}, column {startCol} in {_fileName}");
            }

            int length = _pos - startPos;
            _tokens.Add(new LexToken(TokenType.Variable, name, startLine, startCol, startPos, length));
        }

        private void ScanGreaterThan()
        {
            if (Peek(1) == '>')
            {
                AddToken(TokenType.DoubleRightAngle, ">>", 2);
            }
            else if (Peek(1) == '=')
            {
                AddToken(TokenType.GreaterOrEqual, ">=", 2);
            }
            else
            {
                AddToken(TokenType.GreaterThan, ">", 1);
            }
        }

        private void ScanMinusOrArrow()
        {
            if (Peek(1) == '-' && Peek(2) == '>')
            {
                // -->
                AddToken(TokenType.Arrow, "-->", 3);
            }
            else if (char.IsDigit(Peek(1)))
            {
                // Negative number: check if preceded by an operator/paren/caret context
                // In OPS5, a standalone - before a digit in a value position is a negative number
                if (IsNegativeNumberContext())
                    ScanNumber();
                else
                    AddToken(TokenType.Minus, "-", 1);
            }
            else
            {
                AddToken(TokenType.Minus, "-", 1);
            }
        }

        private bool IsNegativeNumberContext()
        {
            // A minus is a negative number if preceded by ^, (, {, an operator, identifier, or start of input
            // Identifier is included because in OPS5 value positions (^attr -5), negative numbers follow identifiers
            if (_tokens.Count == 0) return true;
            var lastType = _tokens[^1].Type;
            return lastType == TokenType.Caret ||
                   lastType == TokenType.Identifier ||
                   lastType == TokenType.LeftParen ||
                   lastType == TokenType.LeftBrace ||
                   lastType == TokenType.Equals ||
                   lastType == TokenType.NotEquals ||
                   lastType == TokenType.LessThan ||
                   lastType == TokenType.GreaterThan ||
                   lastType == TokenType.LessOrEqual ||
                   lastType == TokenType.GreaterOrEqual ||
                   lastType == TokenType.Plus ||
                   lastType == TokenType.Minus ||
                   lastType == TokenType.Star ||
                   lastType == TokenType.Slash;
        }

        private void ScanSlash()
        {
            if (Peek(1) == '/')
            {
                // // is OPS5 integer division operator
                AddToken(TokenType.Slash, "//", 2);
            }
            else
            {
                AddToken(TokenType.Slash, "/", 1);
            }
        }

        private void ScanNumber()
        {
            int startLine = _line, startCol = _col, startPos = _pos;
            bool hasMinus = _source[_pos] == '-';
            if (hasMinus)
            {
                _pos++;
                _col++;
            }

            bool hasDecimal = false;
            while (_pos < _source.Length && (char.IsDigit(_source[_pos]) || _source[_pos] == '.'))
            {
                if (_source[_pos] == '.')
                {
                    if (hasDecimal) break;
                    hasDecimal = true;
                }
                _pos++;
                _col++;
            }

            string value = _source[startPos.._pos];
            int length = _pos - startPos;
            var type = hasDecimal ? TokenType.DecimalLiteral : TokenType.IntegerLiteral;
            _tokens.Add(new LexToken(type, value, startLine, startCol, startPos, length));
        }

        private void ScanIdentifier()
        {
            int startLine = _line, startCol = _col, startPos = _pos;

            while (_pos < _source.Length && IsIdentifierChar(_source[_pos]))
            {
                _pos++;
                _col++;
            }

            string value = _source[startPos.._pos];
            int length = _pos - startPos;

            // OPS5 does not have keyword classification at lex time —
            // the parser handles dispatch based on identifier values
            _tokens.Add(new LexToken(TokenType.Identifier, value, startLine, startCol, startPos, length));
        }

        private static bool IsIdentifierStart(char c)
            => char.IsLetter(c) || c == '_';

        private static bool IsIdentifierChar(char c)
            => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.';
    }
}
