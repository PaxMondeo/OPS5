using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class DefaultTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public DefaultTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "DefaultTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "DefaultTest.ops5");

        success.Should().BeTrue("the DefaultTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_DefaultValuesApplied()
    {
        await _engine.LoadAndRun(_projectDir, "DefaultTest.ops5");

        var output = _engine.GetOutputMessages();
        // Block A should have default color=red and size=10
        output.Should().Contain(s => s.Contains("A color=red") && s.Contains("size=10"),
            "block A should use default values for color and size");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_ExplicitOverridesDefault()
    {
        await _engine.LoadAndRun(_projectDir, "DefaultTest.ops5");

        var output = _engine.GetOutputMessages();
        // Block B should have explicit color=blue but default size=10
        output.Should().Contain(s => s.Contains("B color=blue") && s.Contains("size=10"),
            "block B should use explicit color but default size");
    }

    public void Dispose() => _engine.Dispose();
}
