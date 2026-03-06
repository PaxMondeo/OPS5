using FluentAssertions;
using OPS5.Engine.Parsers.Tokenizer;
using Xunit;

namespace OPS5.Engine.Tests.Parsers.Tokenizer
{
    public class LexerTests
    {
        private List<LexToken> Lex(string input) =>
            new OPS5InternalLexer(input, "test.ioc").Tokenize();

        // === String Literals ===

        [Fact]
        public void Tokenize_StringLiteral()
        {
            var tokens = Lex("\"hello world\"");
            tokens[0].Type.Should().Be(TokenType.StringLiteral);
            tokens[0].Value.Should().Be("hello world");
        }

        [Fact]
        public void Tokenize_EscapedQuoteInString()
        {
            var tokens = Lex("\"say \\\"hi\\\"\"");
            tokens[0].Type.Should().Be(TokenType.StringLiteral);
            tokens[0].Value.Should().Be("say \"hi\"");
        }

        [Fact]
        public void Tokenize_FormattedString()
        {
            var tokens = Lex("$\"value is {<x>}\"");
            tokens[0].Type.Should().Be(TokenType.FormattedString);
            tokens[0].Value.Should().Be("value is {<x>}");
        }

        // === Variables ===

        [Fact]
        public void Tokenize_Variable()
        {
            var tokens = Lex("<myVar>");
            tokens[0].Type.Should().Be(TokenType.Variable);
            tokens[0].Value.Should().Be("myVar");
        }

        [Fact]
        public void Tokenize_VariableWithDigits()
        {
            var tokens = Lex("<var1>");
            tokens[0].Type.Should().Be(TokenType.Variable);
            tokens[0].Value.Should().Be("var1");
        }

        // === Numbers ===

        [Fact]
        public void Tokenize_Integer()
        {
            var tokens = Lex("42");
            tokens[0].Type.Should().Be(TokenType.IntegerLiteral);
            tokens[0].Value.Should().Be("42");
        }

        [Fact]
        public void Tokenize_Decimal()
        {
            var tokens = Lex("3.14");
            tokens[0].Type.Should().Be(TokenType.DecimalLiteral);
            tokens[0].Value.Should().Be("3.14");
        }

        [Fact]
        public void Tokenize_NegativeNumber_AfterEquals()
        {
            var tokens = Lex("= -5");
            tokens[0].Type.Should().Be(TokenType.Equals);
            tokens[1].Type.Should().Be(TokenType.IntegerLiteral);
            tokens[1].Value.Should().Be("-5");
        }

        [Fact]
        public void Tokenize_Minus_AfterIdentifier()
        {
            var tokens = Lex("x - 5");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[1].Type.Should().Be(TokenType.Minus);
            tokens[2].Type.Should().Be(TokenType.IntegerLiteral);
        }

        // === Operators ===

        [Fact]
        public void Tokenize_Arrow()
        {
            var tokens = Lex("-->");
            tokens[0].Type.Should().Be(TokenType.Arrow);
        }

        [Fact]
        public void Tokenize_FatArrow()
        {
            var tokens = Lex("=>");
            tokens[0].Type.Should().Be(TokenType.FatArrow);
        }

        [Fact]
        public void Tokenize_LessOrEqual()
        {
            var tokens = Lex("<=");
            tokens[0].Type.Should().Be(TokenType.LessOrEqual);
        }

        [Fact]
        public void Tokenize_GreaterOrEqual()
        {
            var tokens = Lex(">=");
            tokens[0].Type.Should().Be(TokenType.GreaterOrEqual);
        }

        [Fact]
        public void Tokenize_NotEquals_BangEqual()
        {
            var tokens = Lex("!=");
            tokens[0].Type.Should().Be(TokenType.NotEquals);
        }

        [Fact]
        public void Tokenize_NotEquals_DiamondOperator()
        {
            var tokens = Lex("<>");
            tokens[0].Type.Should().Be(TokenType.NotEquals);
        }

        [Fact]
        public void Tokenize_Bang()
        {
            var tokens = Lex("! ClassName");
            tokens[0].Type.Should().Be(TokenType.Bang);
            tokens[1].Type.Should().Be(TokenType.Identifier);
        }

        [Fact]
        public void Tokenize_NotIN()
        {
            var tokens = Lex("!IN");
            tokens[0].Type.Should().Be(TokenType.KW_NotIN);
        }

        // === Punctuation ===

        [Fact]
        public void Tokenize_Punctuation()
        {
            var tokens = Lex("( ) ; , : { } [ ]");
            tokens[0].Type.Should().Be(TokenType.LeftParen);
            tokens[1].Type.Should().Be(TokenType.RightParen);
            tokens[2].Type.Should().Be(TokenType.Semicolon);
            tokens[3].Type.Should().Be(TokenType.Comma);
            tokens[4].Type.Should().Be(TokenType.Colon);
            tokens[5].Type.Should().Be(TokenType.LeftBrace);
            tokens[6].Type.Should().Be(TokenType.RightBrace);
            tokens[7].Type.Should().Be(TokenType.LeftBracket);
            tokens[8].Type.Should().Be(TokenType.RightBracket);
        }

        [Fact]
        public void Tokenize_DoubleAngles()
        {
            var tokens = Lex("<< >>");
            tokens[0].Type.Should().Be(TokenType.DoubleLeftAngle);
            tokens[1].Type.Should().Be(TokenType.DoubleRightAngle);
        }

        // === Keywords ===

        [Fact]
        public void Tokenize_Keywords_CaseInsensitive()
        {
            var tokens = Lex("rule RULE Rule");
            tokens[0].Type.Should().Be(TokenType.KW_Rule);
            tokens[1].Type.Should().Be(TokenType.KW_Rule);
            tokens[2].Type.Should().Be(TokenType.KW_Rule);
        }

        [Fact]
        public void Tokenize_KeywordsPreserveOriginalCase()
        {
            var tokens = Lex("Rule");
            tokens[0].Value.Should().Be("Rule");
        }

        [Fact]
        public void Tokenize_AllMajorKeywords()
        {
            var tokens = Lex("Class Make Modify Remove Set Write Calc IN Matches Contains");
            tokens[0].Type.Should().Be(TokenType.KW_Class);
            tokens[1].Type.Should().Be(TokenType.KW_Make);
            tokens[2].Type.Should().Be(TokenType.KW_Modify);
            tokens[3].Type.Should().Be(TokenType.KW_Remove);
            tokens[4].Type.Should().Be(TokenType.KW_Set);
            tokens[5].Type.Should().Be(TokenType.KW_Write);
            tokens[6].Type.Should().Be(TokenType.KW_Calc);
            tokens[7].Type.Should().Be(TokenType.KW_IN);
            tokens[8].Type.Should().Be(TokenType.KW_Matches);
            tokens[9].Type.Should().Be(TokenType.KW_Contains);
        }

        [Fact]
        public void Tokenize_Identifier_NotKeyword()
        {
            var tokens = Lex("Planet");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("Planet");
        }

        // === Compound Keywords ===

        [Fact]
        public void Tokenize_VectorAppend()
        {
            var tokens = Lex("Vector.Append");
            tokens[0].Type.Should().Be(TokenType.KW_VectorAppend);
        }

        [Fact]
        public void Tokenize_VectorRemove()
        {
            var tokens = Lex("Vector.Remove");
            tokens[0].Type.Should().Be(TokenType.KW_VectorRemove);
        }

        [Fact]
        public void Tokenize_MatrixMake()
        {
            var tokens = Lex("Matrix.Make");
            tokens[0].Type.Should().Be(TokenType.KW_Matrix_Make);
        }

        // === Comments ===

        [Fact]
        public void Tokenize_LineComment_IsSkipped()
        {
            var tokens = Lex("x // this is a comment\ny");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("x");
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("y");
        }

        [Fact]
        public void Tokenize_FileComment_IsPreserved()
        {
            var tokens = Lex("//// This is a file comment");
            tokens[0].Type.Should().Be(TokenType.FileComment);
            tokens[0].Value.Should().Be("This is a file comment");
        }

        // === Line/Column Tracking ===

        [Fact]
        public void Tokenize_TracksLineNumbers()
        {
            var tokens = Lex("x\ny\nz");
            tokens[0].Line.Should().Be(1);
            tokens[1].Line.Should().Be(2);
            tokens[2].Line.Should().Be(3);
        }

        [Fact]
        public void Tokenize_TracksColumns()
        {
            var tokens = Lex("abc def");
            tokens[0].Column.Should().Be(1);
            tokens[1].Column.Should().Be(5);
        }

        // === Full Syntax Examples ===

        [Fact]
        public void Tokenize_ClassDefinition()
        {
            var tokens = Lex("Class Planet ( Name, Size );");
            tokens[0].Type.Should().Be(TokenType.KW_Class);
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("Planet");
            tokens[2].Type.Should().Be(TokenType.LeftParen);
            tokens[3].Type.Should().Be(TokenType.Identifier);
            tokens[3].Value.Should().Be("Name");
            tokens[4].Type.Should().Be(TokenType.Comma);
            tokens[5].Type.Should().Be(TokenType.Identifier);
            tokens[5].Value.Should().Be("Size");
            tokens[6].Type.Should().Be(TokenType.RightParen);
            tokens[7].Type.Should().Be(TokenType.Semicolon);
        }

        [Fact]
        public void Tokenize_RuleCondition()
        {
            var tokens = Lex("Input (A <a>, B <b>, Operation Add);");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("Input");
            tokens[1].Type.Should().Be(TokenType.LeftParen);
            tokens[2].Type.Should().Be(TokenType.Identifier);
            tokens[2].Value.Should().Be("A");
            tokens[3].Type.Should().Be(TokenType.Variable);
            tokens[3].Value.Should().Be("a");
            tokens[4].Type.Should().Be(TokenType.Comma);
            tokens[5].Type.Should().Be(TokenType.Identifier);
            tokens[5].Value.Should().Be("B");
            tokens[6].Type.Should().Be(TokenType.Variable);
            tokens[6].Value.Should().Be("b");
        }

        [Fact]
        public void Tokenize_CalcExpression()
        {
            var tokens = Lex("Calc(<a> <b> +)");
            tokens[0].Type.Should().Be(TokenType.KW_Calc);
            tokens[1].Type.Should().Be(TokenType.LeftParen);
            tokens[2].Type.Should().Be(TokenType.Variable);
            tokens[3].Type.Should().Be(TokenType.Variable);
            tokens[4].Type.Should().Be(TokenType.Plus);
            tokens[5].Type.Should().Be(TokenType.RightParen);
        }

        [Fact]
        public void Tokenize_NegativeConditionPrefix()
        {
            var tokens = Lex("! Journey (Origin <from>);");
            tokens[0].Type.Should().Be(TokenType.Bang);
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("Journey");
            tokens[2].Type.Should().Be(TokenType.LeftParen);
            tokens[3].Type.Should().Be(TokenType.Identifier);
            tokens[3].Value.Should().Be("Origin");
            tokens[4].Type.Should().Be(TokenType.Variable);
            tokens[4].Value.Should().Be("from");
        }

        [Fact]
        public void Tokenize_WriteWithConcatenatedString()
        {
            var tokens = Lex("Write (\"Hello \" <name> \" from \" <place>);");
            tokens[0].Type.Should().Be(TokenType.KW_Write);
            tokens[1].Type.Should().Be(TokenType.LeftParen);
            tokens[2].Type.Should().Be(TokenType.StringLiteral);
            tokens[2].Value.Should().Be("Hello ");
            tokens[3].Type.Should().Be(TokenType.Variable);
            tokens[3].Value.Should().Be("name");
            tokens[4].Type.Should().Be(TokenType.StringLiteral);
            tokens[4].Value.Should().Be(" from ");
            tokens[5].Type.Should().Be(TokenType.Variable);
        }

        [Fact]
        public void Tokenize_DatabaseBinding()
        {
            var tokens = Lex("Database MyDB \"Server=localhost;\" ;");
            tokens[0].Type.Should().Be(TokenType.KW_Database);
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("MyDB");
            tokens[2].Type.Should().Be(TokenType.StringLiteral);
            tokens[3].Type.Should().Be(TokenType.Semicolon);
        }

        [Fact]
        public void Tokenize_NILKeyword()
        {
            var tokens = Lex("NIL");
            tokens[0].Type.Should().Be(TokenType.KW_NIL);
        }

        [Fact]
        public void Tokenize_AlwaysEndsWithEOF()
        {
            var tokens = Lex("");
            tokens.Should().HaveCount(1);
            tokens[0].Type.Should().Be(TokenType.EOF);
        }

        [Fact]
        public void Tokenize_IdentifierWithUnderscore()
        {
            var tokens = Lex("my_var");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[0].Value.Should().Be("my_var");
        }

        [Fact]
        public void Tokenize_IdentifierWithHyphen()
        {
            var tokens = Lex("Exec_SP");
            tokens[0].Type.Should().Be(TokenType.KW_Exec_SP);
        }

        [Fact]
        public void Tokenize_InheritanceSyntax()
        {
            var tokens = Lex("Class Child : Parent (Attr);");
            tokens[0].Type.Should().Be(TokenType.KW_Class);
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("Child");
            tokens[2].Type.Should().Be(TokenType.Colon);
            tokens[3].Type.Should().Be(TokenType.Identifier);
            tokens[3].Value.Should().Be("Parent");
        }

        [Fact]
        public void Tokenize_MultilineInput()
        {
            var input = "Rule Test(\n  Input (A <a>);\n-->\n  Write (\"ok\");\n);";
            var tokens = Lex(input);
            tokens[0].Type.Should().Be(TokenType.KW_Rule);
            // Find the arrow
            var arrow = tokens.First(t => t.Type == TokenType.Arrow);
            arrow.Line.Should().Be(3);
        }

        [Fact]
        public void Tokenize_LessThan_InComparison()
        {
            var tokens = Lex("X < 5");
            tokens[0].Type.Should().Be(TokenType.Identifier);
            tokens[1].Type.Should().Be(TokenType.LessThan);
            tokens[2].Type.Should().Be(TokenType.IntegerLiteral);
        }

        [Fact]
        public void Tokenize_Conjunction_Braces()
        {
            var tokens = Lex("{ <n> > 1 }");
            tokens[0].Type.Should().Be(TokenType.LeftBrace);
            tokens[1].Type.Should().Be(TokenType.Variable);
            tokens[2].Type.Should().Be(TokenType.GreaterThan);
            tokens[3].Type.Should().Be(TokenType.IntegerLiteral);
            tokens[4].Type.Should().Be(TokenType.RightBrace);
        }

        [Fact]
        public void Tokenize_Disjunction_DoubleAngles()
        {
            var tokens = Lex("<< Red, Blue, Green >>");
            tokens[0].Type.Should().Be(TokenType.DoubleLeftAngle);
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[2].Type.Should().Be(TokenType.Comma);
            tokens[3].Type.Should().Be(TokenType.Identifier);
            tokens[4].Type.Should().Be(TokenType.Comma);
            tokens[5].Type.Should().Be(TokenType.Identifier);
            tokens[6].Type.Should().Be(TokenType.DoubleRightAngle);
        }

        // === Timer keywords ===

        [Fact]
        public void Tokenize_AddTimerKeyword()
        {
            var tokens = Lex("AddTimer DailyMorning Time 08:00 Repeats Daily;");
            tokens[0].Type.Should().Be(TokenType.KW_AddTimer);
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("DailyMorning");
        }

        [Fact]
        public void Tokenize_RemoveTimerKeyword()
        {
            var tokens = Lex("RemoveTimer MyTimer;");
            tokens[0].Type.Should().Be(TokenType.KW_RemoveTimer);
            tokens[1].Type.Should().Be(TokenType.Identifier);
            tokens[1].Value.Should().Be("MyTimer");
            tokens[2].Type.Should().Be(TokenType.Semicolon);
        }

        [Fact]
        public void Tokenize_TimezoneKeyword()
        {
            var tokens = Lex("Timezone");
            tokens[0].Type.Should().Be(TokenType.KW_Timezone);
        }

        [Theory]
        [InlineData("Daily", TokenType.KW_Daily)]
        [InlineData("Weekly", TokenType.KW_Weekly)]
        [InlineData("Monthly", TokenType.KW_Monthly)]
        [InlineData("Yearly", TokenType.KW_Yearly)]
        [InlineData("Hourly", TokenType.KW_Hourly)]
        public void Tokenize_RepeatIntervalKeywords(string keyword, TokenType expected)
        {
            var tokens = Lex(keyword);
            tokens[0].Type.Should().Be(expected);
        }

        [Fact]
        public void Tokenize_FullTimerBinding()
        {
            var tokens = Lex("AddTimer WeeklySync Time 09:00 Repeats Weekly Timezone \"Australia/Adelaide\";");
            tokens[0].Type.Should().Be(TokenType.KW_AddTimer);
            tokens[1].Type.Should().Be(TokenType.Identifier); // WeeklySync
            tokens[2].Type.Should().Be(TokenType.KW_TIME);    // Time
            // 09:00 tokenizes as integer 09, colon, integer 00
            var timezoneIdx = tokens.FindIndex(t => t.Type == TokenType.KW_Timezone);
            timezoneIdx.Should().BeGreaterThan(0);
            tokens[timezoneIdx + 1].Type.Should().Be(TokenType.StringLiteral);
            tokens[timezoneIdx + 1].Value.Should().Be("Australia/Adelaide");
        }

        [Fact]
        public void Tokenize_AcceptKeywords()
        {
            var tokens = Lex("Accept AcceptLine");
            tokens[0].Type.Should().Be(TokenType.KW_Accept);
            tokens[1].Type.Should().Be(TokenType.KW_AcceptLine);
        }

        [Fact]
        public void Tokenize_FileIOKeywords()
        {
            var tokens = Lex("OpenFile CloseFile Out Append");
            tokens[0].Type.Should().Be(TokenType.KW_OpenFile);
            tokens[1].Type.Should().Be(TokenType.KW_CloseFile);
            tokens[2].Type.Should().Be(TokenType.KW_Out);
            tokens[3].Type.Should().Be(TokenType.KW_Append);
        }

        [Fact]
        public void Tokenize_TabTo_RecognisedAsKeyword()
        {
            var tokens = Lex("TabTo");
            tokens[0].Type.Should().Be(TokenType.KW_TabTo);
            tokens[0].Value.Should().Be("TabTo");
        }

        [Fact]
        public void Tokenize_Substr_RecognisedAsKeyword()
        {
            var tokens = Lex("Substr");
            tokens[0].Type.Should().Be(TokenType.KW_Substr);
            tokens[0].Value.Should().Be("Substr");
        }
    }
}
