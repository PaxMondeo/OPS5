using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class ConjunctionTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public ConjunctionTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "ConjunctionTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "ConjunctionTest.ops5");

        success.Should().BeTrue("the ConjunctionTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_OnlyBMatches()
    {
        await _engine.LoadAndRun(_projectDir, "ConjunctionTest.ops5");

        _engine.GetObjects("result").Should().HaveCount(1,
            "only B (value 15) satisfies the conjunction { > 10 < 20 }");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_ResultIsB()
    {
        await _engine.LoadAndRun(_projectDir, "ConjunctionTest.ops5");

        var results = _engine.GetObjects("result");
        results[0].GetAttributeValue("name").Should().Be("B",
            "B is the only item with value between 10 and 20 exclusive");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_DoesNotMatchA()
    {
        await _engine.LoadAndRun(_projectDir, "ConjunctionTest.ops5");

        var results = _engine.GetObjects("result");
        results.Should().NotContain(r => r.GetAttributeValue("name") == "A",
            "A has value 5, which is not greater than 10");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_DoesNotMatchC()
    {
        await _engine.LoadAndRun(_projectDir, "ConjunctionTest.ops5");

        var results = _engine.GetObjects("result");
        results.Should().NotContain(r => r.GetAttributeValue("name") == "C",
            "C has value 25, which is not less than 20");
    }

    public void Dispose() => _engine.Dispose();
}
