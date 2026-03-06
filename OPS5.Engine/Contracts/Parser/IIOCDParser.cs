using OPS5.Engine.Models;

namespace OPS5.Engine.Contracts.Parser
{
    internal interface IIOCDParser
    {
        public IOCDFileModel ParseIOCDFile(string file, string fileName);
    }
}
