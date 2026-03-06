using OPS5.Engine.Models;

namespace OPS5.Engine.Contracts.Parser
{
    internal interface IIOCRParser
    {
        public IOCRFileModel ParseIOCRFile(string file, string fileName);
    }
}
