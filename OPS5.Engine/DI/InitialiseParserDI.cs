using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.FileProcessing;
using OPS5.Engine.Parsers.OPS5;
using OPS5.Engine.Parsers.Tokenizer;
using Microsoft.Extensions.DependencyInjection;

namespace OPS5.Engine.DI
{
    internal static class InitialiseParserDI
    {
        public static void AddParserServices(this IServiceCollection services)
        {
            services.AddTransient<IUtils, OPS5.Engine.Parsers.Utils>();
            services.AddTransient<IFileProcessing, OPS5FileProcessing>();
            services.AddSingleton<ISourceFiles, SourceFiles>();
            services.AddTransient<IOPS5Transpiler, OPS5Transpiler>();

            // Token-based parsers (parse the transpiler's intermediate output)
            services.AddTransient<IIOCCParser, TokenIOCCParser>();
            services.AddTransient<IIOCDParser, TokenIOCDParser>();
            services.AddTransient<IIOCRParser, TokenIOCRParser>();
        }
    }
}
