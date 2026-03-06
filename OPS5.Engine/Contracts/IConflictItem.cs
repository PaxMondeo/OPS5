using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface IConflictItemFactory
    {
        IConflictItem NewConflictItem();
    }
    internal interface IConflictItem
    {
        IToken TheToken { get; set; }
        Rule TheRule { get; set; }
        void SetProperties(IToken token, Rule rule);

    }
}
