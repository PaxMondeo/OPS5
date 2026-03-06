using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface IRHSActionFactory
    {
        IRHSAction NewRHSAction(string actionText, List<object> action);
        IRHSAction NewRHSAction(string actionText, List<string> action);
    }
    public interface IRHSAction
    {
        string ActionText { get; }
        List<object> Action { get; }
        void SetProperties(string actionText, List<string> action);
        void SetProperties(string actionText, List<object> action);
    }
}
