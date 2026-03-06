using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OPS5.Engine
{
    internal class ObjectIDs : IObjectIDs
    {
        private int _objectId = 0;
        private int _tokenId = 0;
        private int _ruleId = 0;

        public int NextTokenID()
        {
            return Interlocked.Increment(ref _tokenId);
        }

        public int NextObjectID()
        {
            return Interlocked.Increment(ref _objectId);
        }

        public int NextRuleID()
        {
            return Interlocked.Increment(ref _ruleId);
        }

    }
}
