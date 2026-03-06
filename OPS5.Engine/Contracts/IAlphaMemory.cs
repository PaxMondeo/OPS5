using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IAlphaMemory
    {
        IAlphaNode Reset();
        IAlphaNode BuildShareAlpha(IAlphaNode parent, ConditionTest test);
        void PrintAlphaMemory();
        void ExamineAlpha(int nodeID);
    }
}
