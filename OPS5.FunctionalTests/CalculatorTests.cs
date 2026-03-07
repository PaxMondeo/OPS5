using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class CalculatorTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public CalculatorTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "Calculator");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "Calculator.ops5");

        success.Should().BeTrue("the Calculator OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_AdditionResult()
    {
        await _engine.LoadAndRun(_projectDir, "Calculator.ops5");

        GetResultValue("add").Should().Be("15", "10 + 5 = 15");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_SubtractionResult()
    {
        await _engine.LoadAndRun(_projectDir, "Calculator.ops5");

        GetResultValue("subtract").Should().Be("12", "20 - 8 = 12");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_MultiplicationResult()
    {
        await _engine.LoadAndRun(_projectDir, "Calculator.ops5");

        GetResultValue("multiply").Should().Be("42", "6 * 7 = 42");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_DivisionResult()
    {
        await _engine.LoadAndRun(_projectDir, "Calculator.ops5");

        GetResultValue("divide").Should().Be("5", "20 / 4 = 5");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_InputsConsumed()
    {
        await _engine.LoadAndRun(_projectDir, "Calculator.ops5");

        _engine.GetObjects("input").Should().BeEmpty(
            "all input objects should be removed after computation");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_AllResultsCreated()
    {
        await _engine.LoadAndRun(_projectDir, "Calculator.ops5");

        _engine.GetObjects("result").Should().HaveCount(4,
            "one result should be created for each of the four operations");
    }

    private string? GetResultValue(string operation)
    {
        var results = _engine.GetObjects("result");
        var match = results.FirstOrDefault(r => r.GetAttributeValue("operation") == operation);
        return match?.GetAttributeValue("value");
    }

    public void Dispose() => _engine.Dispose();
}
