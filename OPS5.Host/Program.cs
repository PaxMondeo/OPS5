using OPS5.Engine.Contracts;
using OPS5.Engine.DI;
using Microsoft.Extensions.DependencyInjection;

namespace OPS5.Host;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("OPS5 Production Rules Engine");
        Console.WriteLine("Based on the RETE Algorithm (Forgy, 1982)");
        Console.WriteLine();

        var services = new ServiceCollection();
        services.AddOPS5Services();
        var provider = services.BuildServiceProvider();

        var config = provider.GetRequiredService<IConfig>();
        string platform = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Windows" : "Linux";
        config.ReadSettings(platform);
        var fileProcessing = provider.GetRequiredService<IFileProcessing>();
        var engine = provider.GetRequiredService<IEngine>();

        // If a file was provided as argument, load and run it
        if (args.Length > 0)
        {
            string fileName = args[0];
            Console.WriteLine($"Loading: {fileName}");
            if (await fileProcessing.ProcessFile(fileName))
            {
                await engine.Run();
            }
            else
            {
                Console.WriteLine("Failed to load file. Please check the file and try again.");
                Environment.Exit(1);
            }
        }
        else
        {
            // Interactive console mode
            Console.WriteLine("Type HELP for a list of commands, or LOAD <filename> to load an OPS5 file.");
            Console.WriteLine();

            bool exit = false;
            while (!exit)
            {
                exit = await engine.RunEngine();
            }
        }
    }
}
