using OPS5.Engine.Contracts;
using System;

namespace OPS5.Engine
{
    internal class ConflictItemFactory : IConflictItemFactory
    {
        IServiceProvider _serviceProvider;

        public ConflictItemFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

        }

        public IConflictItem NewConflictItem()
        {
            var ci = _serviceProvider.GetService(typeof(IConflictItem));
            if (ci == null)
                throw new Exception("Unable to instantiate Conflict Item");
            IConflictItem conflictItem = (IConflictItem)ci;

            return conflictItem;
        }

    }
    internal class ConflictItem : IConflictItem
    {
        public IToken TheToken { get; set; } = default!;
        public Rule TheRule { get; set; } = default!;

        public void SetProperties(IToken token, Rule rule)
        {
            TheToken = token;
            TheRule = rule;
        }
    }
}
