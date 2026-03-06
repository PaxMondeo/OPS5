using System.IO;
using System.Text;
using OPS5.Engine.Contracts;
using OPS5.Engine.DI;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace OPS5.FunctionalTests.Infrastructure;

/// <summary>
/// Wraps a complete OPS5 engine instance for functional testing.
/// Each instance creates its own DI container for full test isolation.
/// </summary>
public class OPS5TestEngine : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ITestOutputHelper? _output;
    private StringWriter? _consoleCapture;

    public IEngine Engine { get; }
    public IWorkingMemory WorkingMemory { get; }
    public IFileProcessing FileProcessing { get; }
    public IOPS5Logger Logger { get; }
    public IWMClasses Classes { get; }
    public IRules Rules { get; }

    private readonly IConfig _config;

    public OPS5TestEngine(ITestOutputHelper? output = null)
    {
        _output = output;

        var services = new ServiceCollection();
        services.AddOPS5Services();
        _serviceProvider = services.BuildServiceProvider();

        Engine = _serviceProvider.GetRequiredService<IEngine>();
        WorkingMemory = _serviceProvider.GetRequiredService<IWorkingMemory>();
        FileProcessing = _serviceProvider.GetRequiredService<IFileProcessing>();
        Logger = _serviceProvider.GetRequiredService<IOPS5Logger>();
        Classes = _serviceProvider.GetRequiredService<IWMClasses>();
        Rules = _serviceProvider.GetRequiredService<IRules>();
        _config = _serviceProvider.GetRequiredService<IConfig>();

        // Initialise config with test settings
        _config.ReadSettings("Windows");
        Logger.SetVerbosity(-1); // Suppress all logging noise
    }

    /// <summary>
    /// Loads an OPS5 project file and runs the engine.
    /// </summary>
    /// <param name="projectDirectory">Absolute path to the directory containing the project file</param>
    /// <param name="projectFile">Name of the project file (e.g., "HelloWorld.ioc")</param>
    /// <param name="maxSteps">Maximum rule firing cycles (0 = unlimited until quiescence)</param>
    /// <returns>True if the project loaded and ran without errors</returns>
    public async Task<bool> LoadAndRun(string projectDirectory, string projectFile, int maxSteps = 0)
    {
        // Ensure trailing separator
        if (!projectDirectory.EndsWith('\\') && !projectDirectory.EndsWith('/'))
            projectDirectory += '\\';

        _config.ClientAppPath = projectDirectory;

        bool loadSuccess = await FileProcessing.ProcessFile(projectFile);
        if (!loadSuccess || Logger.ErrorCount > 0)
            return false;

        // Capture Console.Out only during Engine.Run() so we get clean Write
        // action output without logger startup noise or cross-test contamination.
        _consoleCapture = new StringWriter();
        var savedOut = Console.Out;
        Console.SetOut(_consoleCapture);
        try
        {
            // ProcessFile sets AutoRun=true when Run; is in the project file,
            // but does NOT actually execute the engine. We always run explicitly.
            if (maxSteps > 0)
                await Engine.Run(maxSteps);
            else
                await Engine.Run();
        }
        finally
        {
            Console.SetOut(savedOut);
        }

        WriteStatisticsTable();
        return Logger.ErrorCount == 0;
    }

    /// <summary>
    /// Loads a project file without running the engine.
    /// Use this when you need to set up working memory
    /// before running, then call <see cref="RunOnly"/> to execute.
    /// </summary>
    public async Task<bool> Load(string projectDirectory, string projectFile)
    {
        if (!projectDirectory.EndsWith('\\') && !projectDirectory.EndsWith('/'))
            projectDirectory += '\\';

        _config.ClientAppPath = projectDirectory;

        bool loadSuccess = await FileProcessing.ProcessFile(projectFile);
        return loadSuccess && Logger.ErrorCount == 0;
    }

    /// <summary>
    /// Runs the engine (without loading). Use after <see cref="Load"/>
    /// and any manual working memory setup.
    /// </summary>
    public async Task<bool> RunOnly(int maxSteps = 0)
    {
        _consoleCapture = new StringWriter();
        var savedOut = Console.Out;
        Console.SetOut(_consoleCapture);
        try
        {
            if (maxSteps > 0)
                await Engine.Run(maxSteps);
            else
                await Engine.Run();
        }
        finally
        {
            Console.SetOut(savedOut);
        }

        WriteStatisticsTable();
        return Logger.ErrorCount == 0;
    }

    /// <summary>
    /// Gets all objects of a given class from working memory.
    /// </summary>
    public List<IWMElement> GetObjects(string className)
    {
        return WorkingMemory.ListWMEsByClass(className);
    }

    /// <summary>
    /// Gets the value of a specific attribute from the first object of the given class.
    /// </summary>
    public string? GetFirstObjectAttribute(string className, string attributeName)
    {
        var objects = WorkingMemory.ListWMEsByClass(className);
        if (objects.Count == 0) return null;
        return objects[0].GetAttributeValue(attributeName);
    }

    /// <summary>
    /// Gets all Write action output produced during Engine.Run().
    /// Only captures output from rule Write actions (Console.WriteLine),
    /// not logger messages.
    /// </summary>
    public List<string> GetOutputMessages()
    {
        if (_consoleCapture == null)
            return new List<string>();

        var consoleOutput = _consoleCapture.ToString();
        var lines = consoleOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines.ToList();
    }

    /// <summary>
    /// Gets the number of errors logged during the run.
    /// </summary>
    public int ErrorCount => Logger.ErrorCount;

    /// <summary>
    /// Writes a per-rule firing statistics table to the test output.
    /// </summary>
    private void WriteStatisticsTable()
    {
        if (_output == null) return;

        var firedRules = Rules.GetRules()
            .Where(r => r.FireCount > 0)
            .OrderByDescending(r => r.FireCount)
            .ToList();

        if (firedRules.Count == 0 && Engine.LastRunRuleFirings == 0)
        {
            _output.WriteLine("No rules fired.");
            return;
        }

        int nameWidth = Math.Max("Rule".Length, firedRules.Max(r => r.Name.Length));
        string divider = new string('\u2500', nameWidth + 26);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("Rule Firing Statistics");
        sb.AppendLine(divider);
        sb.AppendLine($"{"Rule".PadRight(nameWidth)}  {"Firings",8}  {"Specificity",11}");
        sb.AppendLine(divider);
        foreach (var rule in firedRules)
            sb.AppendLine($"{rule.Name.PadRight(nameWidth)}  {rule.FireCount,8}  {rule.Specificity,11}");
        sb.AppendLine(divider);

        var duration = Engine.LastRunDuration;
        string durationText = duration.TotalSeconds >= 1
            ? $"{duration.TotalSeconds:F2} s"
            : $"{duration.TotalMilliseconds:F1} ms";
        sb.AppendLine($"Total: {Engine.LastRunRuleFirings} rules fired in {durationText}");

        _output.WriteLine(sb.ToString());
    }

    public void Dispose()
    {
        _consoleCapture?.Dispose();
        _serviceProvider.Dispose();
    }

    /// <summary>
    /// Resolves the path to ioCogProjects directory.
    /// Checks IOCOG_PROJECTS_PATH environment variable first,
    /// then walks up from the solution directory to find the sibling folder.
    /// </summary>
    public static string ResolveProjectsPath()
    {
        // Check environment variable first
        string? envPath = Environment.GetEnvironmentVariable("IOCOG_PROJECTS_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            return envPath;

        // Walk up from the test assembly location to find the solution root,
        // then look for sibling ioCogProjects directory
        string? dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            string candidate = Path.Combine(dir, "ioCogProjects");
            if (Directory.Exists(candidate))
                return candidate;

            // Also check sibling directory from parent
            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent != null)
            {
                candidate = Path.Combine(parent, "ioCogProjects");
                if (Directory.Exists(candidate))
                    return candidate;
            }

            dir = parent;
        }

        // Fallback to the known development path
        string fallback = @"C:\Development\Code\ioCogProjects";
        if (Directory.Exists(fallback))
            return fallback;

        throw new DirectoryNotFoundException(
            "Cannot find ioCogProjects directory. Set the IOCOG_PROJECTS_PATH environment variable.");
    }
}
