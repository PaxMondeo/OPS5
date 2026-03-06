using System;
using System.IO;
using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    internal class Config : IConfig
    {
        public int ThreadThreshold { get; set; } = 20;
        public bool Ops5 { get; set; } = true;
        public string? Platform { get; set; }
        public string? FilePath { get; set; }
        public OPS5Config ops5Config { get; private set; } = new OPS5Config();
        public LearnParams LearnParameters { get; private set; } = new LearnParams();
        public string ClientAppPath { get; set; } = string.Empty;
        public char Slash { get; private set; } = '/';

        private IOPS5Logger _logger;

        public Config(IOPS5Logger logger)
        {
            _logger = logger;
        }

        public void ReadSettings(string platform)
        {
            Platform = platform;
            if (Platform == "Windows")
                Slash = '\\';
        }
    }

    public class OPS5Config
    {
        public int Verbosity { get; set; } = 0;
        public string Source { get; set; } = "";
        public int MaxParallel { get; set; } = 1;
        public string PluginPath { get; set; } = "";
        public bool UseTokenParser { get; set; } = true;
    }

    public class LearnParams
    {
        public int Init { get; set; }
        public float QuitErr { get; set; }
        public int Retries { get; set; }
        public AnnealParams AP { get; set; } = new AnnealParams();
        public GenInitParams GP { get; set; } = new GenInitParams();
        public KohParams KP { get; set; } = new KohParams();
    }

    public class AnnealParams
    {
        public int Temps0 { get; set; }
        public int Temps { get; set; }
        public int Iters0 { get; set; }
        public int Iters { get; set; }
        public int Setback0 { get; set; }
        public int Setback { get; set; }
        public float Start0 { get; set; }
        public float Start { get; set; }
        public float Stop0 { get; set; }
        public float Stop { get; set; }
    }

    public class GenInitParams
    {
        public int Pool { get; set; }
        public int Gens { get; set; }
        public bool Climb { get; set; }
        public float OverInit { get; set; }
        public float PCross { get; set; }
        public float PMutate { get; set; }
    }

    public class KohParams
    {
        public bool normalisation { get; set; }
        public bool LearnMethod { get; set; }
        public float Rate { get; set; }
        public float reduction { get; set; }
    }
}
