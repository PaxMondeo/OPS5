
namespace OPS5.Engine.Contracts
{
    public interface IConfig
    {
        void ReadSettings(string platform);
        bool Ops5 { get; set; }
        string? Platform { get; set; }
        string? FilePath { get; set; }
        OPS5Config ops5Config { get; }
        LearnParams LearnParameters { get; }
        string ClientAppPath { get; set; }
        char Slash { get; }
    }
}
