namespace OPS5.Engine.Contracts
{
    public interface IExecuteBindingRegistry
    {
        void Add(string name, IExecuteBinding binding);
        bool ContainsKey(string name);
        IExecuteBinding? Get(string name);
        void Clear();
    }
}
