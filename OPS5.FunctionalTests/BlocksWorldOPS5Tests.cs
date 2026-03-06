using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class BlocksWorldOPS5Tests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public BlocksWorldOPS5Tests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "BlocksWorld");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "BlocksWorld.ops5");

        success.Should().BeTrue("the BlocksWorld OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_ClassesDefined()
    {
        await _engine.LoadAndRun(_projectDir, "BlocksWorld.ops5");

        _engine.Classes.ClassExists("block").Should().BeTrue("block class should be defined");
        _engine.Classes.ClassExists("goal").Should().BeTrue("goal class should be defined");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_WorkingMemoryHasBlocks()
    {
        await _engine.LoadAndRun(_projectDir, "BlocksWorld.ops5");

        var blocks = _engine.GetObjects("block");
        blocks.Should().HaveCountGreaterOrEqualTo(3, "three blocks were created in the OPS5 data");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_FindsRedBlock()
    {
        await _engine.LoadAndRun(_projectDir, "BlocksWorld.ops5");

        var messages = _engine.GetOutputMessages();
        messages.Should().Contain(m => m.Contains("Found red block"), "should report finding the red block");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_GoalUpdated()
    {
        await _engine.LoadAndRun(_projectDir, "BlocksWorld.ops5");

        var goals = _engine.GetObjects("goal");
        goals.Should().HaveCount(1, "one goal was created");
        var goalStatus = goals[0].GetAttributeValue("status");
        goalStatus.Should().Be("found", "goal status should be updated to 'found' after finding the red block");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_OutputsDoneMessage()
    {
        await _engine.LoadAndRun(_projectDir, "BlocksWorld.ops5");

        var messages = _engine.GetOutputMessages();
        messages.Should().Contain(m => m.Contains("Done"), "should output completion message");
    }

    public void Dispose() => _engine.Dispose();
}
