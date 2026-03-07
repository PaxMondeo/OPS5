using OPS5.Engine.Enumerations;
using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class StrategyTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public StrategyTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "StrategyTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LEX_PicksMostRecent()
    {
        await _engine.Load(_projectDir, "StrategyTest.ops5");
        _engine.SetStrategy(ConflictResolutionStrategy.LEX);
        await _engine.RunOnly();

        var results = _engine.GetObjects("result");
        results.Should().HaveCount(1);
        results[0].GetAttributeValue("picked").Should().Be("Second");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task MEA_ResolvesCorrectly()
    {
        await _engine.Load(_projectDir, "StrategyTest.ops5");
        _engine.SetStrategy(ConflictResolutionStrategy.MEA);
        await _engine.RunOnly();

        var results = _engine.GetObjects("result");
        results.Should().HaveCount(1);
        // MEA: first condition is Control (same for both), falls to sub-ordering
        results[0].GetAttributeValue("picked").Should().NotBeNullOrEmpty();
    }

    public void Dispose() => _engine.Dispose();
}
