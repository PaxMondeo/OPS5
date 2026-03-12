using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class CBindTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public CBindTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "CBindTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "CBindTest.ops5");

        success.Should().BeTrue("the CBindTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_TimeTagCaptured()
    {
        await _engine.LoadAndRun(_projectDir, "CBindTest.ops5");

        var output = _engine.GetOutputMessages();
        output.Should().Contain(s => s.Contains("time-tag="),
            "cbind should capture and write the time-tag of the created WME");

        // The time-tag should be a positive integer
        var tagLine = output.First(s => s.Contains("time-tag="));
        var tagStr = tagLine.Split("time-tag=")[1].Trim();
        int.TryParse(tagStr, out int tag).Should().BeTrue("time-tag should be a numeric value");
        tag.Should().BeGreaterThan(0, "time-tag should be positive");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_TagStoredInControl()
    {
        await _engine.LoadAndRun(_projectDir, "CBindTest.ops5");

        // The control object should have the tag value stored
        var tagValue = _engine.GetFirstObjectAttribute("control", "TAG");
        tagValue.Should().NotBeNullOrEmpty();
        int.TryParse(tagValue, out int tag).Should().BeTrue("tag attribute should contain the numeric time-tag");
        tag.Should().BeGreaterThan(0);
    }

    public void Dispose() => _engine.Dispose();
}
