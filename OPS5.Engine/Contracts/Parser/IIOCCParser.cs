using OPS5.Engine.Models;

namespace OPS5.Engine.Contracts.Parser
{
    internal interface IIOCCParser
    {
        public IOCCFileModel ParseIOCCFile(string file, string fileName);
    }
}
