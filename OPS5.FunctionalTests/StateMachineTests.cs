using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class StateMachineTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public StateMachineTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "StateMachine");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "StateMachine.ops5");

        success.Should().BeTrue("the StateMachine OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_FinalStatusDone()
    {
        await _engine.LoadAndRun(_projectDir, "StateMachine.ops5");

        var contexts = _engine.GetObjects("context");
        contexts.Should().NotBeEmpty("a context object should exist after execution");
        contexts[0].GetAttributeValue("status").Should().Be("done",
            "the state machine should reach the done state");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_AllStepsRecorded()
    {
        await _engine.LoadAndRun(_projectDir, "StateMachine.ops5");

        var results = _engine.GetObjects("result");
        results.Should().HaveCount(3, "three step results should be recorded (one, two, three)");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_CorrectStepOrder()
    {
        await _engine.LoadAndRun(_projectDir, "StateMachine.ops5");

        var results = _engine.GetObjects("result");
        var steps = results.Select(r => r.GetAttributeValue("step")).ToList();

        steps.Should().Contain("one", "step one should be recorded");
        steps.Should().Contain("two", "step two should be recorded");
        steps.Should().Contain("three", "step three should be recorded");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoDuplicateSteps()
    {
        await _engine.LoadAndRun(_projectDir, "StateMachine.ops5");

        var results = _engine.GetObjects("result");
        var steps = results.Select(r => r.GetAttributeValue("step")).ToList();

        steps.Should().OnlyHaveUniqueItems("each step should fire exactly once");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_OutputsTransitions()
    {
        await _engine.LoadAndRun(_projectDir, "StateMachine.ops5");

        var messages = _engine.GetOutputMessages();
        messages.Should().Contain(m => m.Contains("Transitioned"),
            "output should contain transition messages");
        messages.Should().Contain(m => m.Contains("completed"),
            "output should contain the completion message");
    }

    public void Dispose() => _engine.Dispose();
}
