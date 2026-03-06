using FluentAssertions;
using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Parsers.Tokenizer;
using NSubstitute;
using Xunit;

namespace OPS5.Engine.Tests.Engine;

public class ConflictResolutionTests
{
    private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();
    private readonly ISourceFiles _sourceFiles = Substitute.For<ISourceFiles>();

    public ConflictResolutionTests()
    {
        _sourceFiles.ProjectFile.Returns(new SourceFile("test.ioc", "", "", "", false, false));
    }

    #region Token IOCR Parser — Rule ALL Deprecation

    [Fact]
    public void RuleWithALL_EmitsDeprecationError()
    {
        _sourceFiles.RuleFiles.Returns(new Dictionary<string, SourceFile>());
        var WMClasses = Substitute.For<IWMClasses>();
        var parser = new TokenIOCRParser(_logger, _sourceFiles, WMClasses);

        string ruleFile = "Rule TestRule ALL(\n  Item (Name <n>);\n-->\n  Write (\"test\");\n);";

        parser.ParseIOCRFile(ruleFile, "test.iocr");

        _logger.Received().WriteError(
            Arg.Is<string>(s => s.Contains("ALL modifier") && s.Contains("no longer supported")),
            Arg.Any<string>());
    }

    #endregion
}
