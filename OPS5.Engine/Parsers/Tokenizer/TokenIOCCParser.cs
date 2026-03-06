using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Models;
using System;
using System.Collections.Generic;

namespace OPS5.Engine.Parsers.Tokenizer
{
    /// <summary>
    /// Token-based parser for .iocc class definition files.
    /// </summary>
    internal class TokenIOCCParser : TokenParserBase, IIOCCParser
    {
        public TokenIOCCParser(IOPS5Logger logger, ISourceFiles sourceFiles)
            : base(logger, sourceFiles) { }

        public IOCCFileModel ParseIOCCFile(string file, string fileName)
        {
            var model = new IOCCFileModel();
            string uFileName = fileName.ToUpper();

            if (!SourceFiles.ClassFiles.ContainsKey(uFileName))
                SourceFiles.ClassFiles.Add(uFileName,
                    new SourceFile(fileName, SourceFiles.ProjectFile.FilePath, "", file, true, true));

            var stream = LexAndSetup(file, fileName);

            while (!stream.IsAtEnd)
            {
                if (stream.Check(TokenType.KW_Class))
                {
                    try
                    {
                        ParseClass(stream, model, fileName);
                    }
                    catch (ParseException ex)
                    {
                        Logger.WriteError(ex.Message, fileName);
                        stream.SkipPast(TokenType.Semicolon);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError(ex.Message, fileName);
                        stream.SkipPast(TokenType.Semicolon);
                    }
                }
                else
                {
                    ReportError(stream, "Expected 'Class' keyword", "class file");
                    stream.SkipUntil(TokenType.KW_Class, TokenType.Semicolon);
                    stream.TryConsume(TokenType.Semicolon);
                }
            }

            return model;
        }

        private void ParseClass(TokenStream stream, IOCCFileModel model, string fileName)
        {
            stream.Expect(TokenType.KW_Class);

            // Reconstruct original line for model
            int startOffset = stream.Current.Offset;

            // Class name
            var nameToken = stream.Advance();
            string className = nameToken.Value;
            string baseClass = "";
            bool isBase = true;

            // Check for inheritance: Child : Parent
            if (stream.TryConsume(TokenType.Colon) != null)
            {
                var baseToken = stream.Advance();
                baseClass = baseToken.Value.ToUpper();
                className = className.ToUpper();
                isBase = false;
            }

            stream.Expect(TokenType.LeftParen);

            // Parse comma-separated attribute list
            var attrs = new List<string>();
            while (!stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
            {
                // Each attribute is an identifier, possibly followed by a type keyword
                var attrParts = new List<string>();

                // Collect the attribute name (could be dotted like Coords.X)
                string attrName = ConsumeValue(stream);
                attrParts.Add(attrName);

                // Check if followed by a type keyword or bracket (nested class)
                if (!stream.Check(TokenType.Comma) && !stream.Check(TokenType.RightParen) && !stream.IsAtEnd)
                {
                    // Could be a type like TEXT, NUMBER, DATE, etc.
                    // Or a bracket [ NestedClass ]
                    if (stream.Check(TokenType.LeftBracket))
                    {
                        stream.Advance(); // [
                        string nestedClass = ConsumeValue(stream);
                        stream.Expect(TokenType.RightBracket); // ]
                        attrParts.Add("[");
                        attrParts.Add(nestedClass);
                        attrParts.Add("]");
                    }
                    else if (!stream.Check(TokenType.Comma) && !stream.Check(TokenType.RightParen))
                    {
                        // Type name
                        attrParts.Add(ConsumeValue(stream));
                    }
                }

                attrs.Add(string.Join(" ", attrParts));
                stream.TryConsume(TokenType.Comma);
            }

            stream.Expect(TokenType.RightParen);
            stream.Expect(TokenType.Semicolon);

            var classModel = new ClassModel("")
            {
                ClassName = className,
                IsBase = isBase,
                BaseClass = baseClass,
                Atoms = attrs
            };

            try
            {
                classModel.ValidateAtoms();
                model.Classes.Add(classModel);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Invalid Syntax found in {fileName}: {ex.Message}", "Parser");
            }
        }
    }
}
