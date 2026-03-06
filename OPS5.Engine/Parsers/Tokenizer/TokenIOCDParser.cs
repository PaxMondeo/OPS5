using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Models;
using System.Collections.Generic;

namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// Token-based parser for .iocd data initialization files.
    /// </summary>
    internal class TokenIOCDParser : TokenParserBase, IIOCDParser
    {
        public TokenIOCDParser(IOPS5Logger logger, ISourceFiles sourceFiles)
            : base(logger, sourceFiles) { }

        public IOCDFileModel ParseIOCDFile(string file, string fileName)
        {
            var model = new IOCDFileModel();
            var stream = LexAndSetup(file, fileName);

            while (!stream.IsAtEnd)
            {
                try
                {
                    switch (stream.Current.Type)
                    {
                        case TokenType.KW_Make:
                            ParseMakeAction(stream, model, fileName);
                            break;
                        case TokenType.KW_CSVLoad:
                            ParseParenAction(stream, model, fileName);
                            break;
                        case TokenType.KW_XMLLoad:
                            ParseParenAction(stream, model, fileName);
                            break;
                        case TokenType.KW_ReadTable:
                        case TokenType.KW_ReadTableChanges:
                            ParseSimpleAction(stream, model, fileName);
                            break;
                        case TokenType.KW_Exec_SQL:
                            ParseSimpleAction(stream, model, fileName);
                            break;
                        default:
                            ReportError(stream, $"Unexpected token '{stream.Current.Value}'", "data file");
                            stream.SkipPast(TokenType.Semicolon);
                            break;
                    }
                }
                catch (ParseException ex)
                {
                    Logger.WriteError(ex.Message, fileName);
                    stream.SkipPast(TokenType.Semicolon);
                }
            }

            return model;
        }

        /// <summary>
        /// Parse Make ClassName (...); - collects all tokens into atoms list
        /// to match the format expected by the existing DataActionModel consumer.
        /// </summary>
        private void ParseMakeAction(TokenStream stream, IOCDFileModel model, string fileName)
        {
            var atoms = CollectAtomsUntilSemicolon(stream);
            var action = new DataActionModel(atoms.Count > 0 ? atoms[0] : "MAKE", fileName)
            {
                Atoms = atoms
            };
            model.Actions.Add(action);
        }

        /// <summary>
        /// Parse CSVLoad/XMLLoad ClassName (...);
        /// </summary>
        private void ParseParenAction(TokenStream stream, IOCDFileModel model, string fileName)
        {
            var atoms = CollectAtomsUntilSemicolon(stream);
            var action = new DataActionModel(atoms.Count > 0 ? atoms[0] : "", fileName)
            {
                Atoms = atoms
            };
            model.Actions.Add(action);
        }

        /// <summary>
        /// Parse ReadTable/ReadTableChanges/Exec_SQL - simple commands ending in ;
        /// </summary>
        private void ParseSimpleAction(TokenStream stream, IOCDFileModel model, string fileName)
        {
            var atoms = CollectAtomsUntilSemicolon(stream);
            var action = new DataActionModel(atoms.Count > 0 ? atoms[0] : "", fileName)
            {
                Atoms = atoms
            };
            model.Actions.Add(action);
        }

        /// <summary>
        /// Collect all token values until a semicolon is reached.
        /// This produces a List of strings matching what the old parser's
        /// Utils.ParseCommand() would return.
        /// </summary>
        private List<string> CollectAtomsUntilSemicolon(TokenStream stream)
        {
            var atoms = new List<string>();
            int parenDepth = 0;

            while (!stream.IsAtEnd && !(stream.Check(TokenType.Semicolon) && parenDepth == 0))
            {
                var token = stream.Current;
                switch (token.Type)
                {
                    case TokenType.LeftParen:
                        parenDepth++;
                        stream.Advance();
                        break;
                    case TokenType.RightParen:
                        parenDepth--;
                        if (parenDepth < 0) parenDepth = 0;
                        stream.Advance();
                        break;
                    case TokenType.Comma:
                        stream.Advance();
                        break;
                    case TokenType.LeftBracket:
                        // Collect bracket content as a single string "[a,b,c]"
                        // to match ProcessElements' expected format for VECTOR
                        atoms.Add(CollectBracketContentAsString(stream));
                        break;
                    case TokenType.Variable:
                        atoms.Add($"<{token.Value}>");
                        stream.Advance();
                        break;
                    case TokenType.FormattedString:
                        atoms.Add("FORMATTED");
                        atoms.Add(token.Value);
                        stream.Advance();
                        break;
                    default:
                        atoms.Add(token.Value);
                        stream.Advance();
                        break;
                }
            }

            stream.TryConsume(TokenType.Semicolon);
            return atoms;
        }

        /// <summary>
        /// Collect bracket content [a, b, c] into a single string "[a,b,c]"
        /// so that ProcessElements and AddVector can parse it correctly.
        /// </summary>
        private string CollectBracketContentAsString(TokenStream stream)
        {
            var items = new List<string>();
            stream.TryConsume(TokenType.LeftBracket);
            while (!stream.Check(TokenType.RightBracket) && !stream.IsAtEnd)
            {
                if (stream.Check(TokenType.Comma))
                {
                    stream.Advance();
                    continue;
                }
                items.Add(stream.Current.Value);
                stream.Advance();
            }
            stream.TryConsume(TokenType.RightBracket);
            return "[" + string.Join(",", items) + "]";
        }
    }
}
