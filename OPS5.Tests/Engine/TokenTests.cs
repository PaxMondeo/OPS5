using FluentAssertions;
using OPS5.Engine.Contracts;
using NSubstitute;
using Xunit;

namespace OPS5.Engine.Tests.Engine;

public class TokenTests
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly IWorkingMemory _workingMemory = Substitute.For<IWorkingMemory>();
    private readonly IConfig _config = Substitute.For<IConfig>();
    private readonly Token _sut;

    public TokenTests()
    {
        _sut = new Token(_logger, _workingMemory, _config);
    }

    private IWMElement CreateMockWME(int timeTag)
    {
        var wme = Substitute.For<IWMElement>();
        wme.TimeTag.Returns(timeTag);
        wme.AddToken(Arg.Any<int>()).Returns(true);
        return wme;
    }

    private void SetupWME(int objectId, int timeTag)
    {
        var wme = CreateMockWME(timeTag);
        _workingMemory.GetWME(objectId).Returns(wme);
    }

    #region Variable Binding

    [Fact]
    public void NewVariable_AddsVariable_CanBeRetrieved()
    {
        _sut.NewVariable("X", "hello");

        _sut.TryGetVariableValue("X").Should().Be("hello");
    }

    [Fact]
    public void NewVariable_DuplicateKey_DoesNotOverwrite()
    {
        _sut.NewVariable("X", "first");
        _sut.NewVariable("X", "second");

        _sut.TryGetVariableValue("X").Should().Be("first");
    }

    [Fact]
    public void UpdateVariable_ExistingVar_OverwritesValue()
    {
        _sut.NewVariable("X", "old");
        _sut.UpdateVariable("X", "new");

        _sut.TryGetVariableValue("X").Should().Be("new");
    }

    [Fact]
    public void UpdateVariable_CaseInsensitive_MatchesUpperCase()
    {
        _sut.UpdateVariable("x", "val");

        _sut.TryGetVariableValue("X").Should().Be("val");
    }

    [Fact]
    public void TryGetVariableValue_UnknownVar_ReturnsOriginalString()
    {
        var result = _sut.TryGetVariableValue("<unknown>");

        result.Should().Be("<unknown>");
    }

    [Fact]
    public void HasVar_ExistingVar_ReturnsTrue()
    {
        _sut.NewVariable("X", "val");

        _sut.HasVar("X").Should().BeTrue();
    }

    [Fact]
    public void HasVar_MissingVar_ReturnsFalse()
    {
        _sut.HasVar("NONEXISTENT").Should().BeFalse();
    }

    #endregion

    #region Temp Variables and Commit/Rollback

    [Fact]
    public void NewTempVariable_CommitVariables_MovesToPermanent()
    {
        _sut.NewTempVariable("Y", "temp_val");
        _sut.CommitVariables();

        _sut.Variables.Should().ContainKey("Y");
        _sut.Variables["Y"].Should().Be("temp_val");
    }

    [Fact]
    public void NewTempVariable_RollBackVariables_DiscardsTemp()
    {
        _sut.NewTempVariable("Y", "temp_val");
        _sut.RollBackVariables();

        _sut.Variables.Should().NotContainKey("Y");
        _sut.HasVar("Y").Should().BeFalse();
    }

    [Fact]
    public void TryGetVariableValue_TempVarTakesPrecedence_ReturnsTempValue()
    {
        _sut.NewVariable("X", "permanent");
        _sut.NewTempVariable("X", "temporary");

        _sut.TryGetVariableValue("X").Should().Be("temporary");
    }

    [Fact]
    public void CommitVariables_OverwritesExistingPermanent()
    {
        _sut.NewVariable("X", "old");
        _sut.NewTempVariable("X", "new");
        _sut.CommitVariables();

        _sut.Variables["X"].Should().Be("new");
    }

    [Fact]
    public void RollBackVariables_ClearsTempVariables()
    {
        _sut.NewTempVariable("TEMP_ONLY", "val");

        _sut.HasVar("TEMP_ONLY").Should().BeTrue();

        _sut.RollBackVariables();

        _sut.HasVar("TEMP_ONLY").Should().BeFalse();
    }

    #endregion

    #region Object Management

    [Fact]
    public void AddObject_SingleObject_AppearsInObjectIDs()
    {
        SetupWME(1, 100);

        _sut.AddObject(1);

        _sut.ObjectIDs.Should().Contain(1);
    }

    [Fact]
    public void AddObject_CallsWorkingMemoryGetWME()
    {
        SetupWME(42, 100);

        _sut.AddObject(42);

        _workingMemory.Received().GetWME(42);
    }

    [Fact]
    public void ObjectCount_ReturnsCorrectCount()
    {
        SetupWME(1, 100);
        SetupWME(2, 200);
        SetupWME(3, 300);

        _sut.AddObject(1);
        _sut.AddObject(2);
        _sut.AddObject(3);

        _sut.ObjectCount().Should().Be(3);
    }

    #endregion

    #region Copy

    [Fact]
    public void Copy_CopiesObjectIDsAndVariables()
    {
        SetupWME(1, 100);
        SetupWME(2, 200);
        _sut.AddObject(1);
        _sut.AddObject(2);
        _sut.NewVariable("A", "val_a");

        var target = new Token(_logger, _workingMemory, _config);
        _sut.Copy(target);

        target.ObjectIDs.Should().BeEquivalentTo(new[] { 1, 2 });
        target.Variables.Should().ContainKey("A");
        target.Variables["A"].Should().Be("val_a");
    }

    [Fact]
    public void Copy_DoesNotShareReference_ModifyingSourceDoesNotAffectTarget()
    {
        SetupWME(1, 100);
        _sut.AddObject(1);
        _sut.NewVariable("A", "original");

        var target = new Token(_logger, _workingMemory, _config);
        _sut.Copy(target);

        // Modify source
        _sut.UpdateVariable("A", "modified");

        // Target should be unchanged
        target.Variables["A"].Should().Be("original");
    }

    #endregion

    #region GetObjectKey

    [Fact]
    public void GetObjectKey_ReturnsCommaDelimitedIDs()
    {
        SetupWME(1, 100);
        SetupWME(2, 200);
        SetupWME(3, 300);
        _sut.AddObject(1);
        _sut.AddObject(2);
        _sut.AddObject(3);

        _sut.GetObjectKey().Should().Be("1,2,3");
    }

    [Fact]
    public void GetObjectKey_EmptyToken_ReturnsEmptyString()
    {
        _sut.GetObjectKey().Should().BeEmpty();
    }

    #endregion

    #region GetVariables / SetVariables

    [Fact]
    public void GetVariables_ReturnsCopy_ModifyingReturnedDictDoesNotAffectOriginal()
    {
        _sut.NewVariable("A", "val");

        var vars = _sut.GetVariables();
        vars["A"] = "changed";

        _sut.TryGetVariableValue("A").Should().Be("val");
    }

    [Fact]
    public void SetVariables_ReplacesAllVariables()
    {
        _sut.NewVariable("OLD", "old_val");

        var newVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "NEW", "new_val" }
        };
        _sut.SetVariables(newVars);

        _sut.HasVar("OLD").Should().BeFalse();
        _sut.HasVar("NEW").Should().BeTrue();
    }

    #endregion

    #region GetRecency

    [Fact]
    public void GetRecency_ReturnsCommaDelimitedTimeTags()
    {
        SetupWME(1, 10);
        SetupWME(2, 20);
        _sut.AddObject(1);
        _sut.AddObject(2);

        _sut.GetRecency().Should().Be("10,20");
    }

    [Fact]
    public void GetRecency_EmptyToken_ReturnsEmptyString()
    {
        _sut.GetRecency().Should().BeEmpty();
    }

    #endregion

    #region Fired Property

    [Fact]
    public void Fired_DefaultIsFalse()
    {
        _sut.Fired.Should().BeFalse();
    }

    [Fact]
    public void Fired_CanBeSetToTrue()
    {
        _sut.Fired = true;

        _sut.Fired.Should().BeTrue();
    }

    #endregion
}
