using FluentAssertions;
using OPS5.Engine.Calculators;
using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using NSubstitute;
using Xunit;

namespace OPS5.Engine.Tests.Calculators;

public class CheckTests
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly IUtils _utils = Substitute.For<IUtils>();
    private readonly IToken _token = Substitute.For<IToken>();

    /// <summary>
    /// Creates a Check with the given expression and configures the mock IUtils.ParseCommand
    /// to return the operator and value as tokens.
    /// </summary>
    private Check CreateCheck(string checkExpression, string op, string value)
    {
        // Check.Evaluate calls _parserUtils.ParseCommand(" " + check) which should return [op, value]
        _utils.ParseCommand(Arg.Any<string>()).Returns(new List<string> { op, value });

        // Token.TryGetVariableValue returns the value as-is (no variable resolution)
        _token.TryGetVariableValue(value).Returns(value);

        var check = new Check(_logger, _utils);
        check.SetProperties(checkExpression);
        return check;
    }

    #region Equality Operator

    [Fact]
    public void Evaluate_EqualsOperator_MatchingValues_ReturnsTrue()
    {
        var check = CreateCheck("(= 5)", "=", "5");

        check.Evaluate("5", _token).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_EqualsOperator_NonMatchingValues_ReturnsFalse()
    {
        var check = CreateCheck("(= 5)", "=", "5");

        check.Evaluate("3", _token).Should().BeFalse();
    }

    #endregion

    #region Inequality Operators

    [Fact]
    public void Evaluate_NotEqualsOperator_DifferentValues_ReturnsTrue()
    {
        var check = CreateCheck("(!= 5)", "!=", "5");

        check.Evaluate("3", _token).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NotEqualsOperator_SameValues_ReturnsFalse()
    {
        var check = CreateCheck("(!= 5)", "!=", "5");

        check.Evaluate("5", _token).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_DiamondNotEquals_DifferentValues_ReturnsTrue()
    {
        var check = CreateCheck("(<> 5)", "<>", "5");

        check.Evaluate("3", _token).Should().BeTrue();
    }

    #endregion

    #region Numeric Comparisons

    [Fact]
    public void Evaluate_GreaterThan_LargerValue_ReturnsTrue()
    {
        var check = CreateCheck("(> 3)", ">", "3");

        check.Evaluate("5", _token).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GreaterThan_SmallerValue_ReturnsFalse()
    {
        var check = CreateCheck("(> 3)", ">", "3");

        check.Evaluate("1", _token).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_LessThan_SmallerValue_ReturnsTrue()
    {
        var check = CreateCheck("(< 10)", "<", "10");

        check.Evaluate("5", _token).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GreaterOrEqual_EqualValue_ReturnsTrue()
    {
        var check = CreateCheck("(>= 5)", ">=", "5");

        check.Evaluate("5", _token).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LessOrEqual_EqualValue_ReturnsTrue()
    {
        var check = CreateCheck("(<= 5)", "<=", "5");

        check.Evaluate("5", _token).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_GreaterThan_NonNumericValues_ReturnsFalse()
    {
        var check = CreateCheck("(> abc)", ">", "abc");
        _token.TryGetVariableValue("abc").Returns("abc");

        check.Evaluate("xyz", _token).Should().BeFalse();
    }

    #endregion

    #region Variable Resolution

    [Fact]
    public void Evaluate_ResolvesVariableFromToken()
    {
        _utils.ParseCommand(Arg.Any<string>()).Returns(new List<string> { "=", "<X>" });
        _token.TryGetVariableValue("<X>").Returns("5");

        var check = new Check(_logger, _utils);
        check.SetProperties("(= <X>)");

        check.Evaluate("5", _token).Should().BeTrue();
    }

    #endregion

    #region Default Operator

    [Fact]
    public void Evaluate_UnknownOperator_ReturnsFalse()
    {
        _utils.ParseCommand(Arg.Any<string>()).Returns(new List<string> { "BADOP", "5" });
        _token.TryGetVariableValue("5").Returns("5");

        var check = new Check(_logger, _utils);
        check.SetProperties("(BADOP 5)");

        check.Evaluate("5", _token).Should().BeFalse();
    }

    #endregion
}
