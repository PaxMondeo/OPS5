using FluentAssertions;
using OPS5.Engine.Contracts;
using NSubstitute;
using System.IO;
using Xunit;

namespace OPS5.Engine.Tests.Engine;

public class FileHandleManagerTests : IDisposable
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly FileHandleManager _manager;
    private readonly string _testDir;

    public FileHandleManagerTests()
    {
        _manager = new FileHandleManager(_logger);
        _testDir = Path.Combine(Path.GetTempPath(), "OPS5_FileHandleManagerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        _manager.CloseAll();
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    private string TestFile(string name) => Path.Combine(_testDir, name);

    [Fact]
    public void OpenFile_Out_CreatesWritableStream()
    {
        string path = TestFile("out.txt");
        _manager.OpenFile("output", path, "OUT");

        _manager.IsOpen("output").Should().BeTrue();
        var writer = _manager.GetWriter("output");
        writer.Should().NotBeNull();
        writer!.Write("hello");
        writer.Flush();

        _manager.CloseFile("output");
        File.ReadAllText(path).Should().Be("hello");
    }

    [Fact]
    public void OpenFile_In_CreatesReadableStream()
    {
        string path = TestFile("in.txt");
        File.WriteAllText(path, "test data");

        _manager.OpenFile("input", path, "IN");

        _manager.IsOpen("input").Should().BeTrue();
        var reader = _manager.GetReader("input");
        reader.Should().NotBeNull();
        reader!.ReadToEnd().Should().Be("test data");
    }

    [Fact]
    public void OpenFile_Append_CreatesAppendableStream()
    {
        string path = TestFile("append.txt");
        File.WriteAllText(path, "line1\n");

        _manager.OpenFile("log", path, "APPEND");

        var writer = _manager.GetWriter("log");
        writer.Should().NotBeNull();
        writer!.Write("line2");
        writer.Flush();

        _manager.CloseFile("log");
        File.ReadAllText(path).Should().Be("line1\nline2");
    }

    [Fact]
    public void CloseFile_DisposesStream()
    {
        string path = TestFile("close.txt");
        _manager.OpenFile("output", path, "OUT");
        _manager.IsOpen("output").Should().BeTrue();

        _manager.CloseFile("output");

        _manager.IsOpen("output").Should().BeFalse();
        _manager.GetWriter("output").Should().BeNull();
    }

    [Fact]
    public void CloseAll_DisposesAllStreams()
    {
        string path1 = TestFile("all1.txt");
        string path2 = TestFile("all2.txt");
        _manager.OpenFile("f1", path1, "OUT");
        _manager.OpenFile("f2", path2, "OUT");

        _manager.CloseAll();

        _manager.IsOpen("f1").Should().BeFalse();
        _manager.IsOpen("f2").Should().BeFalse();
    }

    [Fact]
    public void GetWriter_NonExistent_ReturnsNull()
    {
        _manager.GetWriter("nosuchfile").Should().BeNull();
    }

    [Fact]
    public void GetReader_OnWriteHandle_ReturnsNull()
    {
        string path = TestFile("writeonly.txt");
        _manager.OpenFile("output", path, "OUT");

        _manager.GetReader("output").Should().BeNull();
    }

    [Fact]
    public void GetWriter_OnReadHandle_ReturnsNull()
    {
        string path = TestFile("readonly.txt");
        File.WriteAllText(path, "data");
        _manager.OpenFile("input", path, "IN");

        _manager.GetWriter("input").Should().BeNull();
    }

    [Fact]
    public void IsOpen_CaseInsensitive()
    {
        string path = TestFile("case.txt");
        _manager.OpenFile("MyFile", path, "OUT");

        _manager.IsOpen("myfile").Should().BeTrue();
        _manager.IsOpen("MYFILE").Should().BeTrue();
    }

    [Fact]
    public void OpenFile_InvalidMode_LogsError()
    {
        string path = TestFile("badmode.txt");
        _manager.OpenFile("output", path, "BOGUS");

        _manager.IsOpen("output").Should().BeFalse();
        _logger.Received().WriteError(
            Arg.Is<string>(s => s.Contains("Unknown mode")),
            Arg.Any<string>());
    }

    [Fact]
    public void OpenFile_Reopens_ClosesExisting()
    {
        string path1 = TestFile("reopen1.txt");
        string path2 = TestFile("reopen2.txt");

        _manager.OpenFile("output", path1, "OUT");
        var writer1 = _manager.GetWriter("output");
        writer1!.Write("first");

        // Reopening should close the first handle
        _manager.OpenFile("output", path2, "OUT");
        var writer2 = _manager.GetWriter("output");
        writer2!.Write("second");
        writer2.Flush();

        _manager.CloseFile("output");

        // First file should have been flushed and closed
        File.ReadAllText(path1).Should().Be("first");
        File.ReadAllText(path2).Should().Be("second");
    }
}
