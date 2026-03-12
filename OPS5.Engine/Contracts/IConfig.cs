
namespace OPS5.Engine.Contracts
{
    public interface IConfig
    {
        void ReadSettings(string platform);
        string? Platform { get; set; }
        string? FilePath { get; set; }
        string ClientAppPath { get; set; }
    }
}
