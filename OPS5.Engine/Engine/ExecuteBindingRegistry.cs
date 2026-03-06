using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;

namespace OPS5.Engine
{
    internal class ExecuteBindingRegistry : IExecuteBindingRegistry
    {
        private readonly Dictionary<string, IExecuteBinding> _bindings = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string name, IExecuteBinding binding)
        {
            _bindings[name] = binding;
        }

        public bool ContainsKey(string name)
        {
            return _bindings.ContainsKey(name);
        }

        public IExecuteBinding? Get(string name)
        {
            return _bindings.TryGetValue(name, out var binding) ? binding : null;
        }

        public void Clear()
        {
            _bindings.Clear();
        }
    }
}
