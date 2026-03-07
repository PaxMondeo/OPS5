using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class HelloWorldTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public HelloWorldTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "HelloWorld");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "HelloWorld.ops5");

        success.Should().BeTrue("the HelloWorld OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_ClassDefined()
    {
        await _engine.LoadAndRun(_projectDir, "HelloWorld.ops5");

        _engine.Classes.ClassExists("planet").Should().BeTrue("planet class should be defined");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_PlanetsInMemory()
    {
        await _engine.LoadAndRun(_projectDir, "HelloWorld.ops5");

        var planets = _engine.GetObjects("planet");
        planets.Should().HaveCount(2, "two planets (Earth and Mars) were created");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_OutputsGreetings()
    {
        await _engine.LoadAndRun(_projectDir, "HelloWorld.ops5");

        var messages = _engine.GetOutputMessages();
        messages.Where(m => m.Contains("Hello")).Should().HaveCount(2,
            "each planet should produce a Hello greeting");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_RulesFired()
    {
        await _engine.LoadAndRun(_projectDir, "HelloWorld.ops5");

        _engine.Engine.LastRunRuleFirings.Should().Be(2,
            "the hello rule should fire once for each of the two planets");
    }

    public void Dispose() => _engine.Dispose();
}
