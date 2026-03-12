using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class TabToTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public TabToTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "TabToTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "TabToTest.ops5");

        success.Should().BeTrue("the TabToTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_OutputContainsPadding()
    {
        await _engine.LoadAndRun(_projectDir, "TabToTest.ops5");

        var output = _engine.GetOutputMessages();
        output.Should().Contain(s => s.Contains("A") && s.Contains("B"),
            "output should contain both A and B");

        // The tabto(10) should insert spaces between A and B
        var line = output.First(s => s.Contains("A") && s.Contains("B"));
        var indexA = line.IndexOf("A");
        var indexB = line.IndexOf("B");
        (indexB - indexA).Should().BeGreaterThan(1,
            "tabto should insert padding spaces between A and B");
    }

    public void Dispose() => _engine.Dispose();
}
