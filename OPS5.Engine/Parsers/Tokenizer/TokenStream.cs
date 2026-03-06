using System.Collections.Generic;
using System.Linq;

namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// Provides a navigable stream over a list of tokens with
    /// lookahead, matching, and error-context capabilities.
    /// </summary>
    public class TokenStream
    {
        private readonly IReadOnlyList<LexToken> _tokens;
        private int _position;
        private readonly LexToken _eof;

        public string FileName { get; }

        public TokenStream(IReadOnlyList<LexToken> tokens, string fileName)
        {
            _tokens = tokens;
            _position = 0;
            FileName = fileName;
            _eof = tokens.Count > 0 && tokens[^1].Type == TokenType.EOF
                ? tokens[^1]
                : LexToken.Eof(0, 0, 0);
        }

        /// <summary>Current token without advancing.</summary>
        public LexToken Current => _position < _tokens.Count
            ? _tokens[_position]
            : _eof;

        /// <summary>Look ahead by N tokens (0 = current).</summary>
        public LexToken Peek(int ahead = 0)
        {
            int idx = _position + ahead;
            return idx < _tokens.Count ? _tokens[idx] : _eof;
        }

        /// <summary>Advance and return the consumed token.</summary>
        public LexToken Advance()
        {
            if (_position < _tokens.Count)
            {
                var token = _tokens[_position];
                _position++;
                return token;
            }
            return _eof;
        }

        /// <summary>
        /// If current token matches the expected type, advance and return it.
        /// Otherwise return null without advancing.
        /// </summary>
        public LexToken? TryConsume(TokenType expected)
        {
            if (Current.Type == expected)
                return Advance();
            return null;
        }

        /// <summary>
        /// Consume the expected token type, or throw ParseException.
        /// </summary>
        public LexToken Expect(TokenType expected)
        {
            if (Current.Type == expected)
                return Advance();

            throw new ParseException(new ParseDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"Expected {expected}",
                FileName = FileName,
                Line = Current.Line,
                Column = Current.Column,
                Expected = expected.ToString(),
                Found = Current.Type == TokenType.EOF ? "<end of file>" : $"{Current.Type}({Current.Value})"
            });
        }

        /// <summary>
        /// Consume the expected token type. If not found, record diagnostic
        /// and return a synthetic token without throwing.
        /// </summary>
        public LexToken ExpectOrDefault(TokenType expected, List<ParseDiagnostic> diagnostics)
        {
            if (Current.Type == expected)
                return Advance();

            diagnostics.Add(new ParseDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"Expected {expected}",
                FileName = FileName,
                Line = Current.Line,
                Column = Current.Column,
                Expected = expected.ToString(),
                Found = Current.Type == TokenType.EOF ? "<end of file>" : $"{Current.Type}({Current.Value})"
            });

            return new LexToken(expected, "", Current.Line, Current.Column, Current.Offset, 0);
        }

        /// <summary>Check if current token is of the given type.</summary>
        public bool Check(TokenType type) => Current.Type == type;

        /// <summary>Check if current token is any of the given types.</summary>
        public bool CheckAny(params TokenType[] types)
        {
            var currentType = Current.Type;
            for (int i = 0; i < types.Length; i++)
            {
                if (currentType == types[i])
                    return true;
            }
            return false;
        }

        /// <summary>True if we have reached the end of the token stream.</summary>
        public bool IsAtEnd => Current.Type == TokenType.EOF;

        /// <summary>Save current position for backtracking.</summary>
        public int SavePosition() => _position;

        /// <summary>Restore to a previously saved position.</summary>
        public void RestorePosition(int saved) => _position = saved;

        /// <summary>Skip tokens until one of the given types is found (for error recovery).</summary>
        public void SkipUntil(params TokenType[] types)
        {
            while (!IsAtEnd && !types.Contains(Current.Type))
            {
                Advance();
            }
        }

        /// <summary>
        /// Skip tokens until one of the given types is found, then consume that token too.
        /// </summary>
        public void SkipPast(params TokenType[] types)
        {
            SkipUntil(types);
            if (!IsAtEnd && types.Contains(Current.Type))
                Advance();
        }

        /// <summary>Number of tokens remaining (including current, excluding EOF).</summary>
        public int Remaining
        {
            get
            {
                int count = 0;
                for (int i = _position; i < _tokens.Count; i++)
                {
                    if (_tokens[i].Type != TokenType.EOF) count++;
                }
                return count;
            }
        }
    }
}
