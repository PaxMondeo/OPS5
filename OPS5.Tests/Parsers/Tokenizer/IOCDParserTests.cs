using FluentAssertions;
using OPS5.Engine.Contracts;
using OPS5.Engine.Parsers.Tokenizer;
using NSubstitute;
using Xunit;

namespace OPS5.Engine.Tests.Parsers.Tokenizer;

public class IOCDParserTests
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly ISourceFiles _sourceFiles = Substitute.For<ISourceFiles>();
    private readonly TokenIOCDParser _parser;

    public IOCDParserTests()
    {
        _sourceFiles.ProjectFile.Returns(new SourceFile("test.ioc", "", "", "", false, false));
        _parser = new TokenIOCDParser(_logger, _sourceFiles);
    }

    private Models.IOCDFileModel Parse(string input)
    {
        return _parser.ParseIOCDFile(input, "test.iocd");
    }

    #region Make action

    [Fact]
    public void ParseMake_SimpleAttributes_ReturnsAction()
    {
        var result = Parse("Make Order (Status Pending, Total 100);");

        result.Actions.Should().HaveCount(1);
        var action = result.Actions[0];
        action.Command.Should().Be("MAKE");
        action.Atoms.Should().Contain("Make");
        action.Atoms.Should().Contain("Order");
        action.Atoms.Should().Contain("Status");
        action.Atoms.Should().Contain("Pending");
        action.Atoms.Should().Contain("Total");
        action.Atoms.Should().Contain("100");
    }

    [Fact]
    public void ParseMake_WithVariable_WrapsInAngleBrackets()
    {
        var result = Parse("Make Result (Value <total>);");

        result.Actions.Should().HaveCount(1);
        result.Actions[0].Atoms.Should().Contain("<total>");
    }

    [Fact]
    public void ParseMake_MultipleActions_ReturnsAll()
    {
        var result = Parse(@"
            Make Order (Status Pending);
            Make Customer (Name Alice);
        ");

        result.Actions.Should().HaveCount(2);
        result.Actions[0].Atoms.Should().Contain("Order");
        result.Actions[1].Atoms.Should().Contain("Customer");
    }

    #endregion

    #region CSVLoad / XMLLoad

    [Fact]
    public void ParseCSVLoad_ReturnsAction()
    {
        // File paths with dots are tokenized as separate tokens (data . csv).
        // In practice, file paths are enclosed in string literals.
        var result = Parse(@"CSVLoad Product (""data.csv"");");

        result.Actions.Should().HaveCount(1);
        result.Actions[0].Command.Should().Be("CSVLOAD");
        result.Actions[0].Atoms.Should().Contain("CSVLoad");
        result.Actions[0].Atoms.Should().Contain("Product");
        result.Actions[0].Atoms.Should().Contain("data.csv");
    }

    [Fact]
    public void ParseXMLLoad_ReturnsAction()
    {
        var result = Parse("XMLLoad Config (settings.xml);");

        result.Actions.Should().HaveCount(1);
        result.Actions[0].Command.Should().Be("XMLLOAD");
        result.Actions[0].Atoms.Should().Contain("XMLLoad");
        result.Actions[0].Atoms.Should().Contain("Config");
    }

    #endregion

    #region ReadTable / ReadTableChanges

    [Fact]
    public void ParseReadTable_ReturnsAction()
    {
        var result = Parse("ReadTable Orders;");

        result.Actions.Should().HaveCount(1);
        result.Actions[0].Command.Should().Be("READTABLE");
        result.Actions[0].Atoms.Should().Contain("ReadTable");
        result.Actions[0].Atoms.Should().Contain("Orders");
    }

    [Fact]
    public void ParseReadTableChanges_ReturnsAction()
    {
        var result = Parse("ReadTableChanges Orders;");

        result.Actions.Should().HaveCount(1);
        result.Actions[0].Command.Should().Be("READTABLECHANGES");
    }

    #endregion

    #region Exec_SQL

    [Fact]
    public void ParseExecSQL_ReturnsAction()
    {
        var result = Parse("Exec_SQL;");

        result.Actions.Should().HaveCount(1);
        result.Actions[0].Command.Should().Be("EXEC_SQL");
    }

    #endregion

    #region Bracket content

    [Fact]
    public void ParseMake_WithBracketVector_CollectsAsSingleString()
    {
        var result = Parse("Make Data (Items [Red, Green, Blue]);");

        result.Actions.Should().HaveCount(1);
        result.Actions[0].Atoms.Should().Contain("[Red,Green,Blue]");
    }

    #endregion

    #region Formatted strings

    [Fact]
    public void ParseMake_WithFormattedString_AddsFormattedKeyword()
    {
        var result = Parse(@"Make Log (Message $""Order {id} created"");");

        result.Actions.Should().HaveCount(1);
        result.Actions[0].Atoms.Should().Contain("FORMATTED");
    }

    #endregion

    #region Command uppercasing

    [Fact]
    public void ParseMake_CommandIsUppercased()
    {
        var result = Parse("Make Order (Status Pending);");

        result.Actions[0].Command.Should().Be("MAKE");
    }

    #endregion

    #region Error handling

    [Fact]
    public void ParseUnknownKeyword_LogsError()
    {
        var result = Parse("Garbage Token;");

        _logger.Received().WriteError(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    #endregion
}
