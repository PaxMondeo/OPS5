using FluentAssertions;
using OPS5.Engine.Parsers.OPS5;
using OPS5.Engine.Parsers.Tokenizer;
using Xunit;

namespace OPS5.Engine.Tests.Parsers.OPS5
{
    public class OPS5LexerTests
    {
        private List<LexToken> Lex(string input) =>
            new OPS5Lexer(input, "test.ops5").Tokenize();

        // === Comments ===

        [Fact]
        public void Tokenize_SemicolonComment_IsSkipped()
        {
            var tokens = Lex("; this is a comment\n(make block)");
            tokens[0].Type.Should().Be(TokenType.LeftParen);
            tokens[1].Value.Should().Be("make");
        }

        [Fact]
        public void Tokenize_InlineComment_StopsAtNewline()
        {
            var tokens = Lex("(make ; comment\nblock)");
            tokens[0].Type.Should().Be(TokenType.LeftParen);
            tokens[1].Value.Should().Be("make");
            tokens[2].Value.Should().Be("block");
            tokens[3].Type.Should().Be(TokenType.RightParen);
        }

        // === Caret ===

        [Fact]
        public void Tokenize_Caret()
        {
            var tokens = Lex("^name");
            tokens[0].Type.Should().Be(TokenType.Caret);
            tokens[0].Value.Should().Be("^");
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("name");
        }

        // === Pipe Strings ===

        [Fact]
        public void Tokenize_PipeString_SimpleText()
        {
            var tokens = Lex("|hello world|");
            tokens[0].Type.Should().Be(TokenType.StringLiteral);
            tokens[0].Value.Should().Be("hello world");
        }

        [Fact]
        public void Tokenize_PipeString_EmptyString()
        {
            var tokens = Lex("||");
            tokens[0].Type.Should().Be(TokenType.StringLiteral);
            tokens[0].Value.Should().Be("");
        }

        [Fact]
        public void Tokenize_PipeString_WithSpecialChars()
        {
            var tokens = Lex("|Block: <name> found!|");
            tokens[0].Type.Should().Be(TokenType.StringLiteral);
            tokens[0].Value.Should().Be("Block: <name> found!");
        }

        // === Variables ===

        [Fact]
        public void Tokenize_Variable()
        {
            var tokens = Lex("<x>");
            tokens[0].Type.Should().Be(TokenType.Variable);
            tokens[0].Value.Should().Be("x");
        }

        [Fact]
        public void Tokenize_Variable_MultiCharName()
        {
            var tokens = Lex("<my-var>");
            tokens[0].Type.Should().Be(TokenType.Variable);
            tokens[0].Value.Should().Be("my-var");
        }

        // === Operators ===

        [Fact]
        public void Tokenize_Arrow()
        {
            var tokens = Lex("-->");
            tokens[0].Type.Should().Be(TokenType.Arrow);
            tokens[0].Value.Should().Be("-->");
        }

        [Fact]
        public void Tokenize_DoubleAngleBrackets()
        {
            var tokens = Lex("<< red blue >>");
            tokens[0].Type.Should().Be(TokenType.DoubleLeftAngle);
            tokens[1].Value.Should().Be("red");
            tokens[2].Value.Should().Be("blue");
            tokens[3].Type.Should().Be(TokenType.DoubleRightAngle);
        }

        [Fact]
        public void Tokenize_ComparisonOperators()
        {
            var tokens = Lex("= <> < > <= >=");
            tokens[0].Type.Should().Be(TokenType.Equals);
            tokens[1].Type.Should().Be(TokenType.NotEquals);
            tokens[2].Type.Should().Be(TokenType.LessThan);
            tokens[3].Type.Should().Be(TokenType.GreaterThan);
            tokens[4].Type.Should().Be(TokenType.LessOrEqual);
            tokens[5].Type.Should().Be(TokenType.GreaterOrEqual);
        }

        [Fact]
        public void Tokenize_ArithmeticOperators()
        {
            var tokens = Lex("+ - * //");
            tokens[0].Type.Should().Be(TokenType.Plus);
            tokens[1].Type.Should().Be(TokenType.Minus);
            tokens[2].Type.Should().Be(TokenType.Star);
            tokens[3].Type.Should().Be(TokenType.Slash);
        }

        // === Identifiers ===

        [Fact]
        public void Tokenize_SimpleIdentifier()
        {
            var tokens = Lex("block");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("block");
        }

        [Fact]
        public void Tokenize_HyphenatedIdentifier()
        {
            var tokens = Lex("on-top-of");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("on-top-of");
        }

        [Fact]
        public void Tokenize_IdentifierWithUnderscore()
        {
            var tokens = Lex("my_class");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("my_class");
        }

        // === Numbers ===

        [Fact]
        public void Tokenize_IntegerLiteral()
        {
            var tokens = Lex("42");
            tokens[0].Type.Should().Be(TokenType.IntegerLiteral);
            tokens[0].Value.Should().Be("42");
        }

        [Fact]
        public void Tokenize_DecimalLiteral()
        {
            var tokens = Lex("3.14");
            tokens[0].Type.Should().Be(TokenType.DecimalLiteral);
            tokens[0].Value.Should().Be("3.14");
        }

        [Fact]
        public void Tokenize_NegativeNumber()
        {
            var tokens = Lex("^mass -5");
            tokens[0].Type.Should().Be(TokenType.Caret);
            tokens[1].Value.Should().Be("mass");
            tokens[2].Type.Should().Be(TokenType.IntegerLiteral);
            tokens[2].Value.Should().Be("-5");
        }

        // === Parentheses and Braces ===

        [Fact]
        public void Tokenize_Parentheses()
        {
            var tokens = Lex("(block)");
            tokens[0].Type.Should().Be(TokenType.LeftParen);
            tokens[1].Value.Should().Be("block");
            tokens[2].Type.Should().Be(TokenType.RightParen);
        }

        [Fact]
        public void Tokenize_Braces()
        {
            var tokens = Lex("{ > 3 < 10 }");
            tokens[0].Type.Should().Be(TokenType.LeftBrace);
            tokens[1].Type.Should().Be(TokenType.GreaterThan);
            tokens[2].Value.Should().Be("3");
            tokens[3].Type.Should().Be(TokenType.LessThan);
            tokens[4].Value.Should().Be("10");
            tokens[5].Type.Should().Be(TokenType.RightBrace);
        }

        // === Slash-prefixed identifiers ===

        [Fact]
        public void Tokenize_SlashFollowedByLetter_IsIdentifier()
        {
            var tokens = Lex("/s");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("/s");
        }

        [Fact]
        public void Tokenize_SlashPath_IsSingleIdentifier()
        {
            var tokens = Lex("/path/to/file");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("/path/to/file");
        }

        [Fact]
        public void Tokenize_SlashSpace_IsSlashToken()
        {
            var tokens = Lex("/ s");
            tokens[0].Type.Should().Be(TokenType.Slash);
            tokens[0].Value.Should().Be("/");
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("s");
        }

        [Fact]
        public void Tokenize_DoubleSlash_IsSlashToken()
        {
            var tokens = Lex("//");
            tokens[0].Type.Should().Be(TokenType.Slash);
            tokens[0].Value.Should().Be("//");
        }

        // === Complete OPS5 Fragments ===

        [Fact]
        public void Tokenize_Literalize()
        {
            var tokens = Lex("(literalize block name color mass)");
            tokens[0].Type.Should().Be(TokenType.LeftParen);
            tokens[1].Value.Should().Be("literalize");
            tokens[2].Value.Should().Be("block");
            tokens[3].Value.Should().Be("name");
            tokens[4].Value.Should().Be("color");
            tokens[5].Value.Should().Be("mass");
            tokens[6].Type.Should().Be(TokenType.RightParen);
        }

        [Fact]
        public void Tokenize_MakeStatement()
        {
            var tokens = Lex("(make block ^name B1 ^color red)");
            tokens[0].Type.Should().Be(TokenType.LeftParen);
            tokens[1].Value.Should().Be("make");
            tokens[2].Value.Should().Be("block");
            tokens[3].Type.Should().Be(TokenType.Caret);
            tokens[4].Value.Should().Be("name");
            tokens[5].Value.Should().Be("B1");
            tokens[6].Type.Should().Be(TokenType.Caret);
            tokens[7].Value.Should().Be("color");
            tokens[8].Value.Should().Be("red");
            tokens[9].Type.Should().Be(TokenType.RightParen);
        }

        [Fact]
        public void Tokenize_ProductionRule()
        {
            string input = @"(p find-red
                (block ^name <x> ^color red)
                -->
                (write |Found: | <x>))";
            var tokens = Lex(input);

            tokens[0].Type.Should().Be(TokenType.LeftParen);
            tokens[1].Value.Should().Be("p");
            tokens[2].Value.Should().Be("find-red");
            tokens[3].Type.Should().Be(TokenType.LeftParen);
            tokens[4].Value.Should().Be("block");
            tokens[5].Type.Should().Be(TokenType.Caret);
            tokens[6].Value.Should().Be("name");
            tokens[7].Type.Should().Be(TokenType.Variable);
            tokens[7].Value.Should().Be("x");
            // ... Arrow token
            var arrowToken = tokens.First(t => t.Type == TokenType.Arrow);
            arrowToken.Value.Should().Be("-->");
        }

        // === Position Tracking ===

        [Fact]
        public void Tokenize_TracksLineAndColumn()
        {
            var tokens = Lex("(block\n  ^name)");
            tokens[0].Line.Should().Be(1);
            tokens[0].Column.Should().Be(1);
            // "block" starts at column 2 on line 1
            tokens[1].Line.Should().Be(1);
            tokens[1].Column.Should().Be(2);
            // ^name is on line 2
            tokens[2].Line.Should().Be(2);
        }

        // === EOF ===

        [Fact]
        public void Tokenize_EmptyInput_ReturnsOnlyEof()
        {
            var tokens = Lex("");
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.EOF);
        }

        [Fact]
        public void Tokenize_OnlyComments_ReturnsOnlyEof()
        {
            var tokens = Lex("; just a comment\n; another one");
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.EOF);
        }
    }
}
