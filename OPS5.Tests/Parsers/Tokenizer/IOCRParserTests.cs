using FluentAssertions;
using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Parsers.Tokenizer;
using NSubstitute;
using Xunit;

namespace OPS5.Engine.Tests.Parsers.Tokenizer;

public class IOCRParserTests
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly ISourceFiles _sourceFiles = Substitute.For<ISourceFiles>();
    private readonly IWMClasses _WMClasses = Substitute.For<IWMClasses>();
    private readonly TokenIOCRParser _sut;

    public IOCRParserTests()
    {
        _sourceFiles.ProjectFile.Returns(new SourceFile("test.iocr", "", "", "", false, false));
        _sourceFiles.RuleFiles.Returns(new Dictionary<string, SourceFile>());
        _sut = new TokenIOCRParser(_logger, _sourceFiles, _WMClasses);
    }

    private Models.IOCRFileModel Parse(string ruleText)
    {
        return _sut.ParseIOCRFile(ruleText, "test.iocr");
    }

    // Helper to wrap a condition line in a minimal rule structure
    private string WrapRule(string lhs, string rhs = "Halt;")
    {
        return $@"
            Rule TestRule(
                {lhs}
            -->
                {rhs}
            );";
    }

    #region LHS — Basic conditions

    [Fact]
    public void ParseRule_SimpleCondition_ReturnsRuleWithCondition()
    {
        var result = Parse(WrapRule("Order (Status Pending);"));

        result.Rules.Should().HaveCount(1);
        var cond = result.Rules[0].Conditions[0];
        cond.ClassName.Should().Be("Order");
        cond.Negative.Should().BeFalse();
        // First test is always CLASS=ClassName
        cond.Tests.Should().Contain(t => t.Attribute == "CLASS" && t.Value == "Order");
        // Second test is the attribute test
        cond.Tests.Should().Contain(t => t.Attribute == "STATUS" && t.Operator == "=" && t.Value == "Pending");
    }

    [Fact]
    public void ParseRule_NegativeCondition_SetsNegativeTrue()
    {
        var result = Parse(WrapRule("! Order ();"));

        result.Rules[0].Conditions[0].Negative.Should().BeTrue();
    }

    [Fact]
    public void ParseRule_NegativeCondition_HasNoAlias()
    {
        var result = Parse(WrapRule("! Order ();"));

        result.Rules[0].Conditions[0].Alias.Should().BeNull();
    }

    [Fact]
    public void ParseRule_MultipleConditions_OrderIsCorrect()
    {
        var result = Parse(WrapRule(@"
            Order (Status Pending);
            Customer (Name Alice);
        "));

        var conditions = result.Rules[0].Conditions;
        conditions.Should().HaveCount(2);
        conditions[0].Order.Should().Be(1);
        conditions[1].Order.Should().Be(2);
        conditions[0].ClassName.Should().Be("Order");
        conditions[1].ClassName.Should().Be("Customer");
    }

    [Fact]
    public void ParseRule_ConditionWithoutParens_ParsesTests()
    {
        var result = Parse(WrapRule("Order Status Pending;"));

        var cond = result.Rules[0].Conditions[0];
        cond.ClassName.Should().Be("Order");
        cond.Tests.Should().Contain(t => t.Attribute == "STATUS" && t.Value == "Pending");
    }

    [Fact]
    public void ParseRule_EmptyParens_OnlyClassTest()
    {
        var result = Parse(WrapRule("Order ();"));

        var cond = result.Rules[0].Conditions[0];
        cond.Tests.Should().HaveCount(1);
        cond.Tests[0].Attribute.Should().Be("CLASS");
        cond.Tests[0].Value.Should().Be("Order");
    }

    [Fact]
    public void ParseRule_AnyModifier_SetsIsAny()
    {
        var result = Parse(WrapRule("Order (Status Active) ANY;"));

        result.Rules[0].Conditions[0].IsAny.Should().BeTrue();
    }

    [Fact]
    public void ParseRule_RuleName_IsPreserved()
    {
        var result = Parse(@"
            Rule MyTestRule(
                Order ();
            -->
                Halt;
            );");

        result.Rules[0].RuleName.Should().Be("MyTestRule");
    }

    #endregion

    #region LHS — Operators

    [Fact]
    public void ParseCondition_ExplicitEquals()
    {
        var result = Parse(WrapRule("Order (Status = Active);"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "STATUS");
        test.Operator.Should().Be("=");
        test.Value.Should().Be("Active");
    }

    [Fact]
    public void ParseCondition_NotEquals()
    {
        var result = Parse(WrapRule("Order (Status != Done);"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "STATUS");
        test.Operator.Should().Be("!=");
        test.Value.Should().Be("Done");
    }

    [Fact]
    public void ParseCondition_GreaterThan()
    {
        var result = Parse(WrapRule("Order (Total > 100);"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "TOTAL");
        test.Operator.Should().Be(">");
        test.Value.Should().Be("100");
    }

    [Fact]
    public void ParseCondition_LessThanOrEqual()
    {
        var result = Parse(WrapRule("Order (Count <= 50);"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "COUNT");
        test.Operator.Should().Be("<=");
        test.Value.Should().Be("50");
    }

    [Fact]
    public void ParseCondition_ImplicitEquals()
    {
        var result = Parse(WrapRule("Order (Status Pending);"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "STATUS");
        test.Operator.Should().Be("=");
        test.Value.Should().Be("Pending");
    }

    #endregion

    #region LHS — Special operators

    [Fact]
    public void ParseCondition_InTest_SetsInTestTrue()
    {
        var result = Parse(WrapRule("Order (Status IN Active);"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "STATUS" && t.Attribute != "CLASS");
        test.InTest.Should().BeTrue();
        test.Operator.Should().Be("IN");
    }

    [Fact]
    public void ParseCondition_MatchesTest_SetsMatchTestTrue()
    {
        var result = Parse(WrapRule(@"Order (Name Matches ""^A.*"");"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "NAME");
        test.MatchTest.Should().BeTrue();
        test.Operator.Should().Be("MATCHES");
    }

    [Fact]
    public void ParseCondition_ContainsTest_SetsContainsTestTrue()
    {
        var result = Parse(WrapRule("Order (Text Contains hello);"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "TEXT");
        test.ContainsTest.Should().BeTrue();
        test.Operator.Should().Be("CONTAINS");
    }

    [Fact]
    public void ParseCondition_LengthTest_SetsVectorLengthTrue()
    {
        var result = Parse(WrapRule("Order (Items Length > 0);"));

        var test = result.Rules[0].Conditions[0].Tests
            .First(t => t.Attribute == "ITEMS");
        test.VectorLength.Should().BeTrue();
    }

    [Fact]
    public void ParseCondition_ConcatTest_SetsConcatenationTrue()
    {
        var result = Parse(@"
            Rule TestRule(
                Vars v (First <f>);
                Source s (Combined <f> Concat World);
            -->
                Halt;
            );");

        var cond = result.Rules[0].Conditions[1];
        var test = cond.Tests.First(t => t.Attribute == "COMBINED");
        test.Concatenation.Should().BeTrue();
    }

    #endregion

    #region LHS — Directives

    [Fact]
    public void ParseRule_DisabledDirective_SetsEnabledFalse()
    {
        var result = Parse(@"
            Rule TestRule(
                Disabled;
                Order (Status Pending);
            -->
                Halt;
            );");

        result.Rules[0].Enabled.Should().BeFalse();
    }

    [Fact]
    public void ParseRule_CommentDirective_SetsComment()
    {
        var result = Parse(@"
            Rule TestRule(
                Comment ""test comment"";
                Order (Status Pending);
            -->
                Halt;
            );");

        result.Rules[0].Comment.Should().Contain("test comment");
    }

    [Fact]
    public void ParseRule_SetDirective_AddsTestToLastCondition()
    {
        var result = Parse(@"
            Rule TestRule(
                Order o (Total <t>);
                SET <x> = Calc(<t> 10 +);
            -->
                Halt;
            );");

        // SET adds a test to the last non-negative condition
        var cond = result.Rules[0].Conditions[0];
        cond.Tests.Should().Contain(t => t.Value.Contains("<X.TESTRULE>"));
    }

    #endregion

    #region RHS — Actions

    [Fact]
    public void ParseRHS_MakeAction_SetsCommandAndClassName()
    {
        var result = Parse(WrapRule("Order ();", "Make Result (Status Done);"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("MAKE");
        action.ClassName.Should().Be("RESULT");
    }

    [Fact]
    public void ParseRHS_ModifyWithNumeric_ResolvesReference()
    {
        var result = Parse(@"
            Rule TestRule(
                Order (Status Pending);
            -->
                Modify 1 (Status Done);
            );");

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("MODIFY");
        action.Actions[1].Should().Be("1");
    }

    [Fact]
    public void ParseRHS_RemoveWithNumeric_ResolvesReference()
    {
        var result = Parse(@"
            Rule TestRule(
                Order (Status Pending);
            -->
                Remove 1;
            );");

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("REMOVE");
        action.Atoms[1].Should().Be("1");
    }

    [Fact]
    public void ParseRHS_HaltAction_CreatesAction()
    {
        var result = Parse(WrapRule("Order ();", "Halt;"));

        result.Rules[0].Actions.Should().HaveCount(1);
        result.Rules[0].Actions[0].Command.Should().Be("HALT");
    }

    [Fact]
    public void ParseRHS_WriteAction_CreatesAction()
    {
        var result = Parse(WrapRule("Order ();", @"Write ""message"";"));

        result.Rules[0].Actions[0].Command.Should().Be("WRITE");
    }

    [Fact]
    public void ParseRHS_ReadRange_SetsClassName()
    {
        var result = Parse(WrapRule("Order ();", "ReadRange Product;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("READRANGE");
        action.ClassName.Should().Be("PRODUCT");
    }

    [Fact]
    public void ParseRHS_WriteRange_SetsClassName()
    {
        var result = Parse(WrapRule("Order ();", "WriteRange Product;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("WRITERANGE");
        action.ClassName.Should().Be("PRODUCT");
    }

    [Fact]
    public void ParseRHS_WriteCellValue_CollectsAtoms()
    {
        var result = Parse(WrapRule("Order ();",
            "WriteCellValue SHEET OutputSheet ROW 1 COL 1 VALUE 9.99;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("WRITECELLVALUE");
        action.Atoms.Should().Contain("SHEET");
        action.Atoms.Should().Contain("OutputSheet");
        action.Atoms.Should().Contain("ROW");
        action.Atoms.Should().Contain("1");
        action.Atoms.Should().Contain("COL");
        action.Atoms.Should().Contain("VALUE");
        action.Atoms.Should().Contain("9.99");
    }

    [Fact]
    public void ParseRHS_AcceptAction_CreatesAction()
    {
        var result = Parse(WrapRule("Order ();", "Accept <x>;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("ACCEPT");
        action.Atoms.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public void ParseRHS_AcceptLineAction_CreatesAction()
    {
        var result = Parse(WrapRule("Order ();", "AcceptLine <x>;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("ACCEPTLINE");
    }

    [Fact]
    public void ParseRHS_AcceptWithPrompt_IncludesPromptAtom()
    {
        var result = Parse(WrapRule("Order ();", @"Accept <x> ""Enter value:"";"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("ACCEPT");
        action.Atoms.Should().Contain("Enter value:");
    }

    [Fact]
    public void ParseRHS_OpenFileAction_CreatesAction()
    {
        var result = Parse(WrapRule("Order ();", @"OpenFile output ""results.txt"" Out;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("OPENFILE");
        action.Atoms.Should().HaveCountGreaterOrEqualTo(4);
        action.Atoms[1].Should().Be("output");  // identifiers go through default (no uppercasing)
        action.Atoms[2].Should().Be("results.txt");
        action.Atoms[3].Should().Be("OUT");
    }

    [Fact]
    public void ParseRHS_CloseFileAction_CreatesAction()
    {
        var result = Parse(WrapRule("Order ();", "CloseFile output;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("CLOSEFILE");
        action.Atoms.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public void ParseRHS_WriteToFile_ContainsToKeyword()
    {
        var result = Parse(WrapRule("Order ();", @"Write (""hello"") To output;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("WRITE");
        action.Atoms.Should().Contain("TO");
    }

    [Fact]
    public void ParseRHS_AcceptFromFile_ContainsFromKeyword()
    {
        var result = Parse(WrapRule("Order ();", "Accept <x> From input;"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("ACCEPT");
        action.Atoms.Should().Contain("FROM");
    }

    [Fact]
    public void ParseRHS_WriteTabTo_CollectsTabToAndColumn()
    {
        var result = Parse(WrapRule("Order ();", @"Write (""Name:"" TabTo 20 <name>);"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("WRITE");
        action.Atoms.Should().Contain("TABTO");
        action.Atoms.Should().Contain("20");
    }

    [Fact]
    public void ParseRHS_SetSubstr_CollectsSubstrAndArgs()
    {
        var result = Parse(WrapRule("Order ();", "Set <r> = Substr(<text> 3 5);"));

        var action = result.Rules[0].Actions[0];
        action.Command.Should().Be("SET");
        action.Atoms.Should().Contain("SUBSTR");
    }

    #endregion

    #region Error handling

    [Fact]
    public void ParseRHS_UnknownAction_LogsError()
    {
        Parse(WrapRule("Order ();", "Bogus;"));

        // Command is uppercased to "BOGUS" before the error check
        _logger.Received().WriteError(
            Arg.Is<string>(s => s.Contains("Unknown") && s.Contains("BOGUS")),
            Arg.Any<string>());
    }

    [Fact]
    public void ParseRHS_ModifyOutOfRange_LogsError()
    {
        Parse(@"
            Rule TestRule(
                Order (Status Pending);
            -->
                Modify 5 (Status Done);
            );");

        _logger.Received().WriteError(
            Arg.Is<string>(s => s.Contains("out of range")),
            Arg.Any<string>());
    }

    #endregion

    #region Variable qualification

    [Fact]
    public void ParseCondition_Variable_IsQualifiedWithRuleName()
    {
        var result = Parse(@"
            Rule TestRule(
                Order (Price <price>);
            -->
                Halt;
            );");

        var cond = result.Rules[0].Conditions[0];
        var test = cond.Tests.First(t => t.Attribute == "PRICE");
        // Variables get qualified with rule name: <price> → <PRICE.TESTRULE>
        test.Value.Should().Be("<PRICE.TESTRULE>");
    }

    #endregion
}
