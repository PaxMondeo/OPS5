using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class DisjunctionTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public DisjunctionTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "DisjunctionTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "DisjunctionTest.ops5");

        success.Should().BeTrue("the DisjunctionTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_MatchesRedAndAmber()
    {
        await _engine.LoadAndRun(_projectDir, "DisjunctionTest.ops5");

        _engine.GetObjects("result").Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_DoesNotMatchGreen()
    {
        await _engine.LoadAndRun(_projectDir, "DisjunctionTest.ops5");

        var results = _engine.GetObjects("result");
        results.Should().NotContain(r => r.GetAttributeValue("name") == "Go");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_DoesNotMatchBlack()
    {
        await _engine.LoadAndRun(_projectDir, "DisjunctionTest.ops5");

        var results = _engine.GetObjects("result");
        results.Should().NotContain(r => r.GetAttributeValue("name") == "Off");
    }

    public void Dispose() => _engine.Dispose();
}
