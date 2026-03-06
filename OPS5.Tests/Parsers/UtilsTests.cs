using FluentAssertions;
using OPS5.Engine.Contracts;
using NSubstitute;
using Xunit;
using ParserUtils = OPS5.Engine.Parsers.Utils;

namespace OPS5.Engine.Tests.Parsers;

public class UtilsTests
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly ISourceFiles _sourceFiles = Substitute.For<ISourceFiles>();
    private readonly ParserUtils _sut;

    public UtilsTests()
    {
        _sut = new ParserUtils(_logger, _sourceFiles);
    }

    #region ParseCommand

    [Fact]
    public void ParseCommand_SimpleWord_ReturnsSingleToken()
    {
        var result = _sut.ParseCommand(" Foo");

        result.Should().ContainSingle().Which.Should().Be("Foo");
    }

    [Fact]
    public void ParseCommand_TwoWordsWithOperator_ReturnsThreeTokens()
    {
        var result = _sut.ParseCommand(" x = 5");

        result.Should().HaveCount(3);
        result[0].Should().Be("x");
        result[1].Should().Be("=");
        result[2].Should().Be("5");
    }

    [Fact]
    public void ParseCommand_QuotedString_ReturnsUnquotedValue()
    {
        var result = _sut.ParseCommand(" x = \"hello world\"");

        result.Should().Contain("hello world");
    }

    [Fact]
    public void ParseCommand_EscapedQuote_PreservesQuoteInOutput()
    {
        var result = _sut.ParseCommand(" x = \"say \\\"hi\\\"\"");

        // Escaped quotes become literal quotes via quote roundtrip
        result.Should().Contain(s => s.Contains("\""));
    }

    [Fact]
    public void ParseCommand_FormattedString_ReturnsFormattedMarkerAndContent()
    {
        var result = _sut.ParseCommand(" x = $\"Hello {name}\"");

        result.Should().Contain("FORMATTED");
        result.Should().Contain(s => s.Contains("Hello"));
    }

    [Fact]
    public void ParseCommand_ConjunctionBraces_ReturnsConjunctionMarkerAndContent()
    {
        var result = _sut.ParseCommand(" x = {a, b}");

        result.Should().Contain("CONJUNCTION");
        result.Should().Contain(s => s.Contains("a,"));
    }

    [Fact]
    public void ParseCommand_DisjunctionAngleBrackets_ReturnsDisjunctionMarkerAndContent()
    {
        var result = _sut.ParseCommand(" x = <<a, b>>");

        result.Should().Contain("DISJUNCTION");
        result.Should().Contain(s => s.Contains("a,"));
    }

    [Fact]
    public void ParseCommand_CalcExpression_ReturnsCalcTypeAndContent()
    {
        var result = _sut.ParseCommand(" x = Calc(1 + 2)");

        result.Should().Contain("CALC");
        result.Should().Contain(s => s.Contains("1"));
    }

    [Fact]
    public void ParseCommand_AddYearsExpression_ReturnsAddYearsTypeAndContent()
    {
        var result = _sut.ParseCommand(" x = AddYears(01-01-2024, 2)");

        result.Should().Contain("ADDYEARS");
    }

    [Fact]
    public void ParseCommand_WhereClause_ReturnsWhereMarkerAndContent()
    {
        var result = _sut.ParseCommand(" x Where(y > 5)");

        result.Should().Contain("WHERE");
        result.Should().Contain(s => s.Contains("y"));
    }

    [Fact]
    public void ParseCommand_VectorBracketSyntax_ReturnsVectorMarkerAndContent()
    {
        var result = _sut.ParseCommand(" x = [1, 2, 3]");

        result.Should().Contain("VECTOR");
    }

    [Fact]
    public void ParseCommand_VectorKeywordSyntax_ReturnsVectorMarkerAndContent()
    {
        var result = _sut.ParseCommand(" x = Vector(1, 2, 3)");

        result.Should().Contain("VECTOR");
    }

    [Fact]
    public void ParseCommand_SplitExpression_ReturnsSplitMarkerAndContent()
    {
        var result = _sut.ParseCommand(" x = Split(a, b)");

        result.Should().Contain("SPLIT");
    }

    [Fact]
    public void ParseCommand_NegativeNumber_CombinesMinusWithDigit()
    {
        // Negative number combining requires whitespace before the minus sign
        // that isn't consumed by another operator pattern (e.g., "=\s")
        var result = _sut.ParseCommand(" x -5");

        result.Should().Contain("-5");
    }

    [Fact]
    public void ParseCommand_EmptyString_ReturnsEmptyList()
    {
        var result = _sut.ParseCommand("");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseCommand_MultipleQuotedStrings_ExtractsAllCorrectly()
    {
        var result = _sut.ParseCommand(" x = \"hello\" y = \"world\"");

        result.Should().Contain("hello");
        result.Should().Contain("world");
    }

    #endregion

    #region RemoveComments

    [Fact]
    public void RemoveComments_SingleLineComment_RemovesComment()
    {
        var result = _sut.RemoveComments("code //comment\nmore code");

        result.Should().Be("code \nmore code");
    }

    [Fact]
    public void RemoveComments_MultipleLines_RemovesAllComments()
    {
        var result = _sut.RemoveComments("line1 //comment1\nline2 //comment2\nline3");

        result.Should().Be("line1 \nline2 \nline3");
    }

    [Fact]
    public void RemoveComments_NoComments_ReturnsOriginal()
    {
        var input = "code without comments";

        var result = _sut.RemoveComments(input);

        result.Should().Be(input);
    }

    #endregion

    #region CountSemicolons

    [Fact]
    public void CountSemicolons_MultipleSemicolonsFollowedByWhitespace_ReturnsCorrectCount()
    {
        var result = _sut.CountSemicolons("a; b; c;");

        result.Should().Be(3);
    }

    [Fact]
    public void CountSemicolons_TrailingSemicolonAtEndOfString_IsCounted()
    {
        var result = _sut.CountSemicolons("a;");

        result.Should().Be(1);
    }

    [Fact]
    public void CountSemicolons_NoSemicolons_ReturnsZero()
    {
        var result = _sut.CountSemicolons("no semicolons here");

        result.Should().Be(0);
    }

    #endregion

    #region CountEndParentheses

    [Fact]
    public void CountEndParentheses_MultipleClosingParens_ReturnsCount()
    {
        var result = _sut.CountEndParentheses("a) b) c)");

        result.Should().Be(3);
    }

    [Fact]
    public void CountEndParentheses_DoubleParensCollapsedToSingle_CountsAsOne()
    {
        // )) becomes ) before counting, so counts as 1 not 2
        var result = _sut.CountEndParentheses("a))");

        result.Should().Be(1);
    }

    #endregion

    #region ParseTime

    [Fact]
    public void ParseTime_ValidTime_ReturnsTrue()
    {
        _sut.ParseTime("14:30:00").Should().BeTrue();
    }

    [Fact]
    public void ParseTime_InvalidTime_ReturnsFalse()
    {
        _sut.ParseTime("not-a-time").Should().BeFalse();
    }

    [Fact]
    public void ParseTime_EmptyString_ReturnsFalse()
    {
        _sut.ParseTime("").Should().BeFalse();
    }

    #endregion

    #region ParseDate

    [Fact]
    public void ParseDate_ValidDate_ReturnsTrue()
    {
        _sut.ParseDate("2024-01-15").Should().BeTrue();
    }

    [Fact]
    public void ParseDate_InvalidDate_ReturnsFalse()
    {
        _sut.ParseDate("not-a-date").Should().BeFalse();
    }

    #endregion

    #region ParseDay

    [Theory]
    [InlineData("MON")]
    [InlineData("TUE")]
    [InlineData("WED")]
    [InlineData("THU")]
    [InlineData("FRI")]
    [InlineData("SAT")]
    [InlineData("SUN")]
    public void ParseDay_ValidDays_ReturnTrue(string day)
    {
        _sut.ParseDay(day).Should().BeTrue();
    }

    [Fact]
    public void ParseDay_CaseInsensitive_ReturnTrue()
    {
        _sut.ParseDay("mon").Should().BeTrue();
    }

    [Fact]
    public void ParseDay_InvalidDay_ReturnFalse()
    {
        _sut.ParseDay("XYZ").Should().BeFalse();
    }

    #endregion

    #region UpToSemi

    [Fact]
    public void UpToSemi_LineWithSemicolon_ReturnsTruncatedLine()
    {
        var result = _sut.UpToSemi("foo bar;");

        result.Should().Be("foo bar");
    }

    [Fact]
    public void UpToSemi_NoSemicolon_ReturnsOriginalAndLogsError()
    {
        var result = _sut.UpToSemi("foo bar");

        result.Should().Be("foo bar");
        _logger.Received(1).WriteError(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region UpToCmdEnd

    [Fact]
    public void UpToCmdEnd_LineWithClosingParenSemicolon_ReturnsUpToClosingParen()
    {
        var result = _sut.UpToCmdEnd("foo(bar);");

        result.Should().Be("foo(bar)");
    }

    [Fact]
    public void UpToCmdEnd_NoMatch_LogsError()
    {
        _sut.UpToCmdEnd("foo bar");

        _logger.Received(1).WriteError(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region TrimChars

    [Fact]
    public void TrimChars_ContainsExpectedWhitespaceCharacters()
    {
        _sut.TrimChars.Should().Contain(' ');
        _sut.TrimChars.Should().Contain('\t');
        _sut.TrimChars.Should().Contain('\n');
        _sut.TrimChars.Should().Contain('\r');
    }

    #endregion
}
