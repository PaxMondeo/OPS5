using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class ComputeTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public ComputeTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "ComputeTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "ComputeTest.ops5");

        success.Should().BeTrue("the ComputeTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NestedCompute()
    {
        await _engine.LoadAndRun(_projectDir, "ComputeTest.ops5");

        var resultValue = _engine.GetFirstObjectAttribute("result", "COMPUTED");
        resultValue.Should().Be("25", "(+ (* 10 2) 5) should equal 25");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_InputConsumed()
    {
        await _engine.LoadAndRun(_projectDir, "ComputeTest.ops5");

        var inputs = _engine.GetObjects("input");
        inputs.Should().BeEmpty("the input WME should be removed after compute");
    }

    public void Dispose() => _engine.Dispose();
}
