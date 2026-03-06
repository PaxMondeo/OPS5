using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface IAlphaNodeFactory
    {
        IAlphaNode NewAlphaNode(IAlphaNode parent, ConditionTest test, List<string> distinctAttributes);
        IAlphaNode AlphaRoot();
    }
    public interface IAlphaNode
    {
        int ID { get; set; }
        IAlphaNode Parent { get; set; }
        List<IAlphaNode> AlphaChildren { get; set; }
        List<IBetaNode> BetaChildren { get; set; }
        string? Attribute { get; set; }
        string Op { get; set; }
        string Value { get; set; }
        bool IsClassTest { get; }
        List<string> Classes { get; }
        void SetProperties(IAlphaNode parent, ConditionTest test, List<string> distinctAttributes);
        void AddObject(int objectID, bool isThreaded);
        List<int> ListObjects();
        void RemoveObject(int objectID);
        int ObjectCount();
        void AttachChildAlpha(IAlphaNode node);
        void AttachChildBeta(IBetaNode node);

    }
}
