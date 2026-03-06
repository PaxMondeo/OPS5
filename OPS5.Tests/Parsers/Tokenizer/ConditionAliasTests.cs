using FluentAssertions;
using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Parsers.Tokenizer;
using NSubstitute;
using Xunit;

namespace OPS5.Engine.Tests.Parsers.Tokenizer;

public class ConditionAliasTests
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly ISourceFiles _sourceFiles = Substitute.For<ISourceFiles>();
    private readonly IWMClasses _WMClasses = Substitute.For<IWMClasses>();
    private readonly TokenIOCRParser _sut;

    public ConditionAliasTests()
    {
        _sourceFiles.ProjectFile.Returns(new SourceFile("test.iocr", "", "", "", false, false));
        _sourceFiles.RuleFiles.Returns(new Dictionary<string, SourceFile>());
        _sut = new TokenIOCRParser(_logger, _sourceFiles, _WMClasses);
    }

    private Models.IOCRFileModel Parse(string ruleText)
    {
        return _sut.ParseIOCRFile(ruleText, "test.iocr");
    }

    #region LHS Alias Detection

    [Fact]
    public void ParseCondition_WithAlias_StoresAliasOnConditionModel()
    {
        var rule = @"
            Rule TestRule(
                Order myOrder (Status Pending);
            -->
                Modify myOrder (Status Done);
            );";

        var result = Parse(rule);

        result.Rules.Should().HaveCount(1);
        var cond = result.Rules[0].Conditions[0];
        cond.Alias.Should().Be("myOrder");
    }

    [Fact]
    public void ParseCondition_WithAlias_PopulatesConditionAliasesDict()
    {
        var rule = @"
            Rule TestRule(
                Order myOrder (Status Pending);
            -->
                Modify myOrder (Status Done);
            );";

        var result = Parse(rule);

        result.Rules[0].ConditionAliases.Should().ContainKey("myOrder");
        result.Rules[0].ConditionAliases["myOrder"].Should().Be(1);
    }

    [Fact]
    public void ParseCondition_WithoutAlias_HasNullAlias()
    {
        var rule = @"
            Rule TestRule(
                Order (Status Pending);
            -->
                Modify 1 (Status Done);
            );";

        var result = Parse(rule);

        result.Rules[0].Conditions[0].Alias.Should().BeNull();
        result.Rules[0].ConditionAliases.Should().BeEmpty();
    }

    [Fact]
    public void ParseCondition_MultipleConditions_SomeWithAliases()
    {
        var rule = @"
            Rule TestRule(
                Order myOrder (Status Pending);
                Customer (Name Adelaide);
                Product myProd (Type Widget);
            -->
                Modify myOrder (Status Done);
                Modify myProd (Sold True);
            );";

        var result = Parse(rule);

        var conditions = result.Rules[0].Conditions;
        conditions.Should().HaveCount(3);
        conditions[0].Alias.Should().Be("myOrder");
        conditions[1].Alias.Should().BeNull();
        conditions[2].Alias.Should().Be("myProd");

        var aliases = result.Rules[0].ConditionAliases;
        aliases.Should().HaveCount(2);
        aliases["myOrder"].Should().Be(1);
        aliases["myProd"].Should().Be(3);
    }

    [Fact]
    public void ParseCondition_DuplicateAlias_LogsError()
    {
        var rule = @"
            Rule TestRule(
                Order myAlias (Status Pending);
                Customer myAlias (Name Adelaide);
            -->
                Modify myAlias (Status Done);
            );";

        Parse(rule);

        _logger.Received().WriteError(
            Arg.Is<string>(s => s.Contains("Duplicate") && s.Contains("myAlias")),
            Arg.Any<string>());
    }

    #endregion

    #region RHS Alias Resolution

    [Fact]
    public void ModifyWithAlias_ResolvesToCorrectConditionIndex()
    {
        var rule = @"
            Rule TestRule(
                Order myOrder (Status Pending);
                Customer (Name Adelaide);
            -->
                Modify myOrder (Status Done);
            );";

        var result = Parse(rule);

        var modifyAction = result.Rules[0].Actions[0];
        modifyAction.Command.Should().Be("MODIFY");
        // The alias should have been resolved to numeric "1"
        modifyAction.Actions[1].Should().Be("1");
    }

    [Fact]
    public void RemoveWithAlias_ResolvesToCorrectConditionIndex()
    {
        var rule = @"
            Rule TestRule(
                Order myOrder (Status Pending);
                Customer (Name Adelaide);
            -->
                Remove myOrder;
            );";

        var result = Parse(rule);

        var removeAction = result.Rules[0].Actions[0];
        removeAction.Command.Should().Be("REMOVE");
        removeAction.Atoms[1].Should().Be("1");
    }

    [Fact]
    public void ModifyWithAlias_SecondCondition_ResolvesToTwo()
    {
        var rule = @"
            Rule TestRule(
                Order (Status Pending);
                Customer myCust (Name Adelaide);
            -->
                Modify myCust (Active True);
            );";

        var result = Parse(rule);

        var modifyAction = result.Rules[0].Actions[0];
        modifyAction.Actions[1].Should().Be("2");
    }

    [Fact]
    public void MixedAliasAndNumericReferences_BothWork()
    {
        var rule = @"
            Rule TestRule(
                Order myOrder (Status Pending);
                Customer (Name Adelaide);
            -->
                Modify myOrder (Status Done);
                Modify 2 (Active True);
            );";

        var result = Parse(rule);

        var actions = result.Rules[0].Actions;
        actions.Should().HaveCount(2);
        actions[0].Actions[1].Should().Be("1");  // alias resolved
        actions[1].Actions[1].Should().Be("2");  // numeric kept
    }

    [Fact]
    public void ModifyWithUnknownAlias_LogsError()
    {
        var rule = @"
            Rule TestRule(
                Order (Status Pending);
            -->
                Modify bogusAlias (Status Done);
            );";

        Parse(rule);

        _logger.Received().WriteError(
            Arg.Is<string>(s => s.Contains("Unknown") && s.Contains("bogusAlias")),
            Arg.Any<string>());
    }

    [Fact]
    public void RemoveWithUnknownAlias_LogsError()
    {
        var rule = @"
            Rule TestRule(
                Order (Status Pending);
            -->
                Remove bogusAlias;
            );";

        Parse(rule);

        _logger.Received().WriteError(
            Arg.Is<string>(s => s.Contains("Unknown") && s.Contains("bogusAlias")),
            Arg.Any<string>());
    }

    [Fact]
    public void AliasResolution_IsCaseInsensitive()
    {
        var rule = @"
            Rule TestRule(
                Order MyOrder (Status Pending);
            -->
                Modify myorder (Status Done);
            );";

        var result = Parse(rule);

        var modifyAction = result.Rules[0].Actions[0];
        modifyAction.Actions[1].Should().Be("1");
    }

    [Fact]
    public void NumericReferences_StillWorkWithNoAliases()
    {
        var rule = @"
            Rule TestRule(
                Order (Status Pending);
                Customer (Name Adelaide);
            -->
                Modify 1 (Status Done);
                Remove 2;
            );";

        var result = Parse(rule);

        var actions = result.Rules[0].Actions;
        actions.Should().HaveCount(2);
        actions[0].Actions[1].Should().Be("1");
        actions[1].Atoms[1].Should().Be("2");
    }

    #endregion
}
