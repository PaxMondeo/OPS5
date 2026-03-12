using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IWMClasses
    {
        bool ClassExists(string className);
        void Reset();
        IWMClass Add(string className);
        IWMClass Add(string className, List<string> attributes);
        IWMClass GetClass(string className);
        void Delete(string className);
        void PrintClasses();
        List<IWMClass> GetClasses();
    }
}
