using OPS5.FunctionalTests.Infrastructure;

namespace OPS5.FunctionalTests;

public class FileIOTests : IDisposable
{
    private readonly OPS5TestEngine _engine;
    private readonly string _projectDir;
    private readonly string _inputFilePath;
    private readonly string _outputFilePath;

    public FileIOTests(ITestOutputHelper output)
    {
        _engine = new OPS5TestEngine(output);
        _projectDir = Path.Combine(OPS5TestEngine.ResolveProjectsPath(), "FileIOTest");
        _inputFilePath = Path.Combine(_projectDir, "test-input.txt");
        _outputFilePath = Path.Combine(_projectDir, "test-output.txt");

        // Create test input file
        File.WriteAllText(_inputFilePath, "TestInputValue");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_NoErrors()
    {
        var success = await _engine.LoadAndRun(_projectDir, "FileIOTest.ops5");

        success.Should().BeTrue("the FileIOTest OPS5 project should load and run without errors");
        _engine.ErrorCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_OutputFileCreated()
    {
        await _engine.LoadAndRun(_projectDir, "FileIOTest.ops5");

        File.Exists(_outputFilePath).Should().BeTrue("output file should be created by openfile/write/closefile");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_OutputFileContainsText()
    {
        await _engine.LoadAndRun(_projectDir, "FileIOTest.ops5");

        var content = File.ReadAllText(_outputFilePath).Trim();
        content.Should().Contain("Hello from OPS5", "write action should output text to the file");
    }

    [Fact]
    [Trait("Category", "OPS5")]
    public async Task LoadAndRun_InputReadCorrectly()
    {
        await _engine.LoadAndRun(_projectDir, "FileIOTest.ops5");

        var results = _engine.GetObjects("result");
        results.Should().HaveCount(1, "one result should be created from reading input file");
        results[0].GetAttributeValue("value").Should().Be("TestInputValue", "should read value from input file");
    }

    public void Dispose()
    {
        _engine.Dispose();
        // Clean up test files
        if (File.Exists(_inputFilePath)) File.Delete(_inputFilePath);
        if (File.Exists(_outputFilePath)) File.Delete(_outputFilePath);
    }
}
