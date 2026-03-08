using System.Collections.Generic;

namespace OPS5.Engine.Contracts
{
    internal interface IBetaNodeFactory
    {
        IBetaNode NewBetaNode();
    }
    public interface IBetaNode
    {
        int ID { get; set; }
        IAlphaNode AlphaParent { get; set; }
        IBetaNode BetaParent { get; set; }
        List<ConditionTest> Tests { get;  }
        Dictionary<string, Binding> Bindings { get; }
        Dictionary<int, IToken> Tokens { get; }
        List<IBetaNode> BetaChildren { get; }
        bool Negative { get; }
        bool IsFindPath { get; }
        bool IsAny { get; set; }
        IFindPathInfo FindPath { get; set; }
        void SetProperties(IBetaNode betaParent, IAlphaNode alphaParent, List<ConditionTest> tests, Dictionary<string, Binding> bindings, bool negative, bool isAny, bool isFindPath, IFindPathInfo? findPath);
        void AttachChildNode(IBetaNode node);
        int TokenCount();
        void AddBindings(Dictionary<string, Binding> newBindings);
        void LeftActivation(IToken token);
        void NegativeLeftActivation(IToken token);
        void RightActivation(int objectID);
        void NegativeRightActivation(int objectID);
        void RemoveObject(int objectID);
        void RemoveToken(List<int> objectIDs);
        bool HasTokens();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="objectID"></param>
        /// <param name="step"></param>
        void UpdatePathVars(IToken token, int objectID, int step);
    }
}
