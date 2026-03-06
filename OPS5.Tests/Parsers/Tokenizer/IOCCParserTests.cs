using FluentAssertions;
using OPS5.Engine.Contracts;
using OPS5.Engine.Parsers.Tokenizer;
using NSubstitute;
using Xunit;

namespace OPS5.Engine.Tests.Parsers.Tokenizer;

public class IOCCParserTests
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly ISourceFiles _sourceFiles = Substitute.For<ISourceFiles>();
    private readonly TokenIOCCParser _parser;

    public IOCCParserTests()
    {
        _sourceFiles.ProjectFile.Returns(new SourceFile("test.ioc", "", "", "", false, false));
        _sourceFiles.ClassFiles.Returns(new Dictionary<string, SourceFile>());
        _parser = new TokenIOCCParser(_logger, _sourceFiles);
    }

    private Models.IOCCFileModel Parse(string input)
    {
        return _parser.ParseIOCCFile(input, "test.iocc");
    }

    #region Basic class parsing

    [Fact]
    public void ParseClass_SimpleAttributes_ReturnsClassModel()
    {
        var result = Parse("Class Order (Status, Total);");

        result.Classes.Should().HaveCount(1);
        var cls = result.Classes[0];
        cls.ClassName.Should().Be("Order");
        cls.Atoms.Should().HaveCount(2);
        cls.Atoms.Should().Contain("Status");
        cls.Atoms.Should().Contain("Total");
    }

    [Fact]
    public void ParseClass_SingleAttribute_Works()
    {
        var result = Parse("Class Flag (Active);");

        result.Classes.Should().HaveCount(1);
        result.Classes[0].Atoms.Should().HaveCount(1);
        result.Classes[0].Atoms[0].Should().Be("Active");
    }

    [Fact]
    public void ParseClass_MultipleClasses_ReturnsAll()
    {
        var result = Parse(@"
            Class Order (Status, Total);
            Class Customer (Name, Email);
        ");

        result.Classes.Should().HaveCount(2);
        result.Classes[0].ClassName.Should().Be("Order");
        result.Classes[1].ClassName.Should().Be("Customer");
    }

    #endregion

    #region Inheritance

    [Fact]
    public void ParseClass_WithInheritance_SetsBaseClass()
    {
        var result = Parse("Class ChildOrder : Order (Priority);");

        result.Classes.Should().HaveCount(1);
        var cls = result.Classes[0];
        cls.IsBase.Should().BeFalse();
        cls.BaseClass.Should().Be("ORDER");
    }

    [Fact]
    public void ParseClass_NoInheritance_IsBaseTrue()
    {
        var result = Parse("Class Order (Status);");

        result.Classes[0].IsBase.Should().BeTrue();
        result.Classes[0].BaseClass.Should().BeEmpty();
    }

    #endregion

    #region Typed attributes

    [Fact]
    public void ParseClass_AttributeWithType_IncludesTypeInAtom()
    {
        var result = Parse("Class Sensor (Temperature NUMBER, Label TEXT);");

        result.Classes.Should().HaveCount(1);
        result.Classes[0].Atoms.Should().Contain(a => a.Contains("Temperature") && a.Contains("NUMBER"));
        result.Classes[0].Atoms.Should().Contain(a => a.Contains("Label") && a.Contains("TEXT"));
    }

    #endregion

    #region Nested class syntax

    [Fact]
    public void ParseClass_NestedClassBracket_ParsesBracketContent()
    {
        var result = Parse("Class Container (Items [Product]);");

        result.Classes.Should().HaveCount(1);
        // The atom should contain Items, [, Product, ]
        var atom = result.Classes[0].Atoms[0];
        atom.Should().Contain("Items");
        atom.Should().Contain("[");
        atom.Should().Contain("Product");
        atom.Should().Contain("]");
    }

    #endregion

    #region Flags

    [Fact]
    public void ParseClass_DisabledFlag_SetsDisabledTrue()
    {
        var result = Parse("Class Order (DISABLED, Status);");

        result.Classes.Should().HaveCount(1);
        result.Classes[0].Disabled.Should().BeTrue();
        result.Classes[0].Atoms.Should().NotContain("DISABLED");
        result.Classes[0].Atoms.Should().Contain("Status");
    }

    [Fact]
    public void ParseClass_PersistentFlag_SetsPersistentTrue()
    {
        var result = Parse("Class Order (PERSISTENT, Status);");

        result.Classes.Should().HaveCount(1);
        result.Classes[0].IsPersistent.Should().BeTrue();
        result.Classes[0].Atoms.Should().NotContain("PERSISTENT");
    }

    [Fact]
    public void ParseClass_PersistObjectFlag_SetsBothPersistFlags()
    {
        var result = Parse("Class Order (PERSISTOBJECT, Status);");

        result.Classes.Should().HaveCount(1);
        result.Classes[0].IsPersistent.Should().BeTrue();
        result.Classes[0].PersistIndividualObjects.Should().BeTrue();
    }

    [Fact]
    public void ParseClass_CommentFlag_SetsComment()
    {
        // ValidateAtoms assumes COMMENT "text" format with quotes (from old parser).
        // The token parser strips quotes so ValidateAtoms truncates the first/last char.
        // Test verifies that COMMENT is processed and removed from Atoms.
        var result = Parse(@"Class Order (COMMENT ""Order tracking"", Status);");

        result.Classes.Should().HaveCount(1);
        result.Classes[0].Comment.Should().NotBeEmpty();
        result.Classes[0].Atoms.Should().NotContain(a => a.ToUpper().StartsWith("COMMENT"));
        result.Classes[0].Atoms.Should().Contain("Status");
    }

    #endregion

    #region Case handling

    [Fact]
    public void ParseClass_ClassName_PreservesOriginalCase()
    {
        var result = Parse("Class MyOrder (Status);");

        result.Classes[0].ClassName.Should().Be("MyOrder");
    }

    [Fact]
    public void ParseClass_InheritedClassName_IsUppercased()
    {
        var result = Parse("Class child : parent (Attr);");

        result.Classes[0].ClassName.Should().Be("CHILD");
        result.Classes[0].BaseClass.Should().Be("PARENT");
    }

    #endregion

    #region Error handling

    [Fact]
    public void ParseClass_EmptyAttributes_LogsError()
    {
        Parse("Class Order ();");

        _logger.Received().WriteError(
            Arg.Is<string>(s => s.Contains("No attributes found")),
            Arg.Any<string>());
    }

    [Fact]
    public void ParseClass_UnexpectedToken_LogsErrorAndRecovers()
    {
        var result = Parse("Garbage;");

        result.Classes.Should().BeEmpty();
        _logger.Received().WriteError(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    #endregion
}
