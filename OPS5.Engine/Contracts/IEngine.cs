using System;
using System.Threading.Tasks;

namespace OPS5.Engine.Contracts
{

    public interface IEngine
    {
        Task<bool> RunEngine(bool isDaemon);
        Task Run();
        Task Run(int maxSteps);
        void Halt();

        /// <summary>
        /// Total number of rule firings in the last completed run.
        /// </summary>
        int LastRunRuleFirings { get; }

        /// <summary>
        /// Wall-clock duration of the last completed run.
        /// </summary>
        TimeSpan LastRunDuration { get; }

        event EventHandler<bool> RunStarted;
        event EventHandler<bool> RunComplete;
    }
}
