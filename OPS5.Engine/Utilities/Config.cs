using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    internal class Config : IConfig
    {
        public string? Platform { get; set; }
        public string? FilePath { get; set; }
        public string ClientAppPath { get; set; } = string.Empty;

        private IOPS5Logger _logger;

        public Config(IOPS5Logger logger)
        {
            _logger = logger;
        }

        public void ReadSettings(string platform)
        {
            Platform = platform;
        }
    }
}
