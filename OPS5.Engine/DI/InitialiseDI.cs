using OPS5.Engine.Calculators;
using OPS5.Engine.Commands;
using OPS5.Engine.Contracts;
using OPS5.Engine.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace OPS5.Engine.DI
{
    public static class InitialiseDI
    {
        public static void AddOPS5Services(this IServiceCollection services)
        {
            // Core engine singletons
            services.AddSingleton<IFileHandleManager, FileHandleManager>();
            services.AddSingleton<IRHSActionExecutor, RHSActionExecutor>();
            services.AddSingleton<IEngine, Engine>();
            services.AddSingleton<IOPS5Logger, OPS5Logger>();
            services.AddSingleton<IConfig, Config>();
            services.AddSingleton<IWorkingMemory, WorkingMemory>();
            services.AddSingleton<IWMClasses, WMClasses>();
            services.AddSingleton<IAlphaMemory, AlphaMemory>();
            services.AddSingleton<IBetaMemory, BetaMemory>();
            services.AddSingleton<IObjectIDs, ObjectIDs>();
            services.AddSingleton<IRules, Rules>();
            services.AddSingleton<ICalculators, OPS5Calculators>();
            services.AddSingleton<IOPS5Settings, OPS5Settings>();

            // Transient types
            services.AddTransient<ICalculator, PrefixCalculator>();
            services.AddTransient<IWMElementFactory, WMElementFactory>();
            services.AddTransient<IBetaNodeFactory, BetaNodeFactory>();
            services.AddTransient<IWMClassFactory, WMClassFactory>();
            services.AddTransient<IRuleFactory, RuleFactory>();
            services.AddTransient<IConflictItemFactory, ConflictItemFactory>();
            services.AddTransient<IRHSActionFactory, RHSActionFactory>();
            services.AddTransient<IAlphaNodeFactory, AlphaNodeFactory>();
            services.AddTransient<ICheckFactory, CheckFactory>();
            services.AddTransient<ITokenFactory, TokenFactory>();
            services.AddTransient<IAlphaNode, AlphaNode>();
            services.AddTransient<IBetaNode, BetaNode>();
            services.AddTransient<ICheck, Check>();
            services.AddTransient<IConflictItem, ConflictItem>();
            services.AddTransient<IWMClass, WMClass>();
            services.AddTransient<IWMElement, WMElement>();
            services.AddTransient<IRHSAction, RHSAction>();
            services.AddTransient<IRule, Rule>();
            services.AddTransient<IToken, Token>();
            services.AddTransient<IOPS5Console, OPS5Console>();

            // Parser services
            services.AddParserServices();
        }
    }
}
