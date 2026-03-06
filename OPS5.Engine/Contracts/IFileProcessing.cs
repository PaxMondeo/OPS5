using System.Collections.Generic;
using System.Threading.Tasks;

namespace OPS5.Engine.Contracts
{
    public interface IFileProcessing
    {
        Task<bool> ProcessFile(string fileName);
        void Make(List<string> atoms);
    }
}
