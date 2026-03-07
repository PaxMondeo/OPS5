using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class TowerOfHanoiTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public TowerOfHanoiTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "TowerOfHanoi");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "TowerOfHanoi.ops5");

        success.Should().BeTrue("the TowerOfHanoi OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_AllDisksOnPeg3()
    {
        await _engine.LoadAndRun(_projectDir, "TowerOfHanoi.ops5");

        var disks = _engine.GetObjects("disk");
        disks.Should().HaveCount(3, "three disks should exist in working memory");
        disks.Should().AllSatisfy(d =>
            d.GetAttributeValue("peg").Should().Be("3",
                "all disks should be on peg 3 after solving the puzzle"));
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoGoalsRemaining()
    {
        await _engine.LoadAndRun(_projectDir, "TowerOfHanoi.ops5");

        _engine.GetObjects("goal").Should().BeEmpty(
            "all goals should be removed after the puzzle is solved");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_CorrectMoveCount()
    {
        await _engine.LoadAndRun(_projectDir, "TowerOfHanoi.ops5");

        var messages = _engine.GetOutputMessages();
        var moveMessages = messages.Where(m => m.Contains("Moving disk")).ToList();
        moveMessages.Should().HaveCount(7,
            "3 disks require 2^3 - 1 = 7 moves to solve Tower of Hanoi");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_OutputContainsMoves()
    {
        await _engine.LoadAndRun(_projectDir, "TowerOfHanoi.ops5");

        var messages = _engine.GetOutputMessages();
        messages.Should().Contain(m => m.Contains("Moving disk"),
            "output should contain move descriptions");
    }

    public void Dispose() => _engine.Dispose();
}
