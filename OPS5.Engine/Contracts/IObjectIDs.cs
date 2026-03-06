using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface IObjectIDs
    {
        int NextTokenID();
        int NextObjectID();
        int NextRuleID();
    }
}
