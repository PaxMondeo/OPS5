using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    internal class ObjectIDs : IObjectIDs
    {
        private int _objectId = 0;
        private int _tokenId = 0;
        private int _ruleId = 0;

        public int NextTokenID()
        {
            return ++_tokenId;
        }

        public int NextObjectID()
        {
            return ++_objectId;
        }

        public int NextRuleID()
        {
            return ++_ruleId;
        }

    }
}
