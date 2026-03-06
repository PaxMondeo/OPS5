using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IBetaMemory
    {
        IBetaNode Reset();
        public IBetaNode BuildShareBeta(IBetaNode betaParent, IAlphaNode alphaParent, List<ConditionTest> newTests, Dictionary<string, Binding> ruleBindings, bool negative, bool isAny, bool isFindPath, IFindPathInfo? findPath);
        IBetaNode AddBetaNode();
        IBetaNode GetBetaNode(int id);
        void PrintBetaMemory();
        void ExamineBeta(int nodeID);
    }
}
