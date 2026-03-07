using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class GenatomTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public GenatomTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "GenatomTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "GenatomTest.ops5");

        success.Should().BeTrue("the GenatomTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_TwoResultsCreated()
    {
        await _engine.LoadAndRun(_projectDir, "GenatomTest.ops5");

        _engine.GetObjects("result").Should().HaveCount(2,
            "two results should be created, one for each genatom call");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_UniqueIds()
    {
        await _engine.LoadAndRun(_projectDir, "GenatomTest.ops5");

        var results = _engine.GetObjects("result");
        var ids = results.Select(r => r.GetAttributeValue("atom-id")).ToList();
        ids.Should().OnlyHaveUniqueItems("genatom should generate unique symbols");
    }

    public void Dispose() => _engine.Dispose();
}
