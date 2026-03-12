using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class AliasTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public AliasTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "AliasTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "AliasTest.ops5");

        success.Should().BeTrue("the AliasTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_BothBlocksMatched()
    {
        await _engine.LoadAndRun(_projectDir, "AliasTest.ops5");

        var resultValue = _engine.GetFirstObjectAttribute("result", "PAIR");
        resultValue.Should().Be("3", "sum of block A (1) and block B (2) should be 3");
    }

    public void Dispose() => _engine.Dispose();
}
