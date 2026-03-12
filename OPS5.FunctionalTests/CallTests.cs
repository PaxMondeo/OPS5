using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class CallTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public CallTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "CallTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "CallTest.ops5");

        success.Should().BeTrue("the CallTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_ProducesOutput()
    {
        await _engine.LoadAndRun(_projectDir, "CallTest.ops5");

        var output = _engine.GetOutputMessages();
        output.Should().NotBeEmpty("hostname should produce output captured by the engine");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_RuleFires()
    {
        await _engine.LoadAndRun(_projectDir, "CallTest.ops5");

        _engine.Engine.LastRunRuleFirings.Should().Be(1,
            "the call-hostname rule should fire exactly once");
    }

    public void Dispose() => _engine.Dispose();
}
