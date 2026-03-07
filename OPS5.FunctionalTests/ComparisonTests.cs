using OPS5.Engine.Contracts;
using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class ComparisonTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;

    public ComparisonTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "ComparisonTest");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "ComparisonTest.ops5");

        success.Should().BeTrue("the ComparisonTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_EqualityMatches()
    {
        await _engine.LoadAndRun(_projectDir, "ComparisonTest.ops5");

        var results = GetResultsForTest("eq");
        results.Should().HaveCount(2, "A and C both have value 10, matching equality test");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NotEqualMatches()
    {
        await _engine.LoadAndRun(_projectDir, "ComparisonTest.ops5");

        var results = GetResultsForTest("ne");
        results.Should().HaveCount(1, "only B has a value not equal to 10");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_LessThanMatches()
    {
        await _engine.LoadAndRun(_projectDir, "ComparisonTest.ops5");

        var results = GetResultsForTest("lt");
        results.Should().HaveCount(2, "A and C have value 10, which is less than 15");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_GreaterThanMatches()
    {
        await _engine.LoadAndRun(_projectDir, "ComparisonTest.ops5");

        var results = GetResultsForTest("gt");
        results.Should().HaveCount(1, "only B has value 20, which is greater than 15");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_LessOrEqualMatches()
    {
        await _engine.LoadAndRun(_projectDir, "ComparisonTest.ops5");

        var results = GetResultsForTest("le");
        results.Should().HaveCount(2, "A and C have value 10, which is less than or equal to 10");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_GreaterOrEqualMatches()
    {
        await _engine.LoadAndRun(_projectDir, "ComparisonTest.ops5");

        var results = GetResultsForTest("ge");
        results.Should().HaveCount(1, "only B has value 20, which is greater than or equal to 20");
    }

    private List<IWMElement> GetResultsForTest(string testName)
    {
        return _engine.GetObjects("result")
            .Where(r => r.GetAttributeValue("test-name") == testName)
            .ToList();
    }

    public void Dispose() => _engine.Dispose();
}
