using OPS5.Engine.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// Base class for all token-based parsers providing common
    /// lexing, error handling, and comment processing.
    /// </summary>
    internal abstract class TokenParserBase
    {
        protected readonly IOPS5Logger Logger;
        protected readonly ISourceFiles SourceFiles;
        protected readonly List<ParseDiagnostic> Diagnostics = new();

        protected TokenParserBase(IOPS5Logger logger, ISourceFiles sourceFiles)
        {
            Logger = logger;
            SourceFiles = sourceFiles;
        }

        /// <summary>
        /// Lex the source and process file-level comments (////).
        /// Returns a TokenStream with file comments filtered out.
        /// </summary>
        protected TokenStream LexAndSetup(string source, string fileName)
        {
            var lexer = new OPS5InternalLexer(source, fileName);
            var allTokens = lexer.Tokenize();

            // Forward any lexer diagnostics
            foreach (var diag in lexer.Diagnostics)
            {
                Diagnostics.Add(diag);
                Logger.WriteError(diag.ToString(), fileName);
            }

            // Process file comments
            foreach (var token in allTokens.Where(t => t.Type == TokenType.FileComment))
            {
                ProcessFileComment(token.Value, fileName);
            }

            // Filter out file comments from the stream the parser sees
            var parseTokens = allTokens
                .Where(t => t.Type != TokenType.FileComment)
                .ToList();

            return new TokenStream(parseTokens, fileName);
        }

        private void ProcessFileComment(string comment, string fileName)
        {
            string uFileName = fileName.ToUpper();
            if (string.IsNullOrEmpty(comment))
                return;

            if (uFileName.EndsWith(".IOC"))
                SourceFiles.ProjectFile.Comment += comment;
            else if (uFileName.EndsWith(".IOCC"))
            {
                if (SourceFiles.ClassFiles.ContainsKey(uFileName))
                    SourceFiles.ClassFiles[uFileName].Comment += comment;
            }
            else if (uFileName.EndsWith(".IOCR"))
            {
                if (SourceFiles.RuleFiles.ContainsKey(uFileName))
                    SourceFiles.RuleFiles[uFileName].Comment += comment;
            }
            else if (uFileName.EndsWith(".IOCB"))
                SourceFiles.BindingFile.Comment += comment;
            else if (uFileName.EndsWith(".IOCD"))
                SourceFiles.DataFile.Comment += comment;
        }

        /// <summary>
        /// Report an error diagnostic and log it.
        /// </summary>
        protected void ReportError(TokenStream stream, string message, string? context = null)
        {
            var current = stream.Current;
            var diag = new ParseDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = message,
                FileName = stream.FileName,
                Line = current.Line,
                Column = current.Column,
                Context = context,
                Found = current.Type == TokenType.EOF ? "<end of file>" : current.Value
            };
            Diagnostics.Add(diag);
            Logger.WriteError(diag.ToString(), stream.FileName);
        }

        /// <summary>
        /// Try to consume a token, report error if not found but don't throw.
        /// </summary>
        protected LexToken ExpectOrReport(TokenStream stream, TokenType expected, string? context = null)
        {
            if (stream.Check(expected))
                return stream.Advance();

            ReportError(stream, $"Expected {expected}, found {stream.Current.Type}({stream.Current.Value})", context);
            return new LexToken(expected, "", stream.Current.Line, stream.Current.Column, stream.Current.Offset, 0);
        }

        /// <summary>
        /// Consume a value token: could be a string literal, identifier, variable,
        /// number, keyword used as a value, or NIL.
        /// Returns the token value as a string.
        /// </summary>
        protected string ConsumeValue(TokenStream stream)
        {
            var current = stream.Current;
            switch (current.Type)
            {
                case TokenType.StringLiteral:
                case TokenType.FormattedString:
                case TokenType.Identifier:
                case TokenType.IntegerLiteral:
                case TokenType.DecimalLiteral:
                    stream.Advance();
                    return current.Value;

                case TokenType.Variable:
                    stream.Advance();
                    return $"<{current.Value}>";

                case TokenType.KW_NIL:
                    stream.Advance();
                    return "NIL";

                default:
                    // Keywords used as values (e.g., type names)
                    if (IsKeyword(current.Type))
                    {
                        stream.Advance();
                        return current.Value;
                    }
                    stream.Advance();
                    return current.Value;
            }
        }

        protected static bool IsKeyword(TokenType type) =>
            type >= TokenType.KW_Project && type <= TokenType.KW_FORMATTED;

        protected static bool IsValueToken(TokenType type) =>
            type == TokenType.StringLiteral ||
            type == TokenType.FormattedString ||
            type == TokenType.Identifier ||
            type == TokenType.Variable ||
            type == TokenType.IntegerLiteral ||
            type == TokenType.DecimalLiteral ||
            type == TokenType.KW_NIL ||
            IsKeyword(type);
    }
}
