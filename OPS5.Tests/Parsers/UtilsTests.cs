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
