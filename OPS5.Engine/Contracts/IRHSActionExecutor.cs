using System.Threading.Tasks;

namespace OPS5.Engine.Contracts
{
    internal interface IRHSActionExecutor
    {
        Task ExecuteActions(IRule rule, IToken token);
        bool HaltRequested { get; }
        string HaltingRule { get; }
        void ResetHalt();
    }
}
