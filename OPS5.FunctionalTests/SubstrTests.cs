using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class SubstrTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public SubstrTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "SubstrTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "SubstrTest.ops5");

        success.Should().BeTrue("the SubstrTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_LiteralSubstr()
    {
        await _engine.LoadAndRun(_projectDir, "SubstrTest.ops5");

        GetSubstrResult("SetLiteral").Should().Be("World");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_InfSubstr()
    {
        await _engine.LoadAndRun(_projectDir, "SubstrTest.ops5");

        GetSubstrResult("SetInf").Should().Be("World");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_VariableSubstr()
    {
        await _engine.LoadAndRun(_projectDir, "SubstrTest.ops5");

        GetSubstrResult("SetVariable").Should().Be("World");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_AllResultsCreated()
    {
        await _engine.LoadAndRun(_projectDir, "SubstrTest.ops5");

        _engine.GetObjects("substr-result").Should().HaveCount(3);
    }

    private string? GetSubstrResult(string testName)
    {
        var results = _engine.GetObjects("substr-result");
        var match = results.FirstOrDefault(r => r.GetAttributeValue("test-name") == testName);
        return match?.GetAttributeValue("value");
    }

    public void Dispose() => _engine.Dispose();
}
