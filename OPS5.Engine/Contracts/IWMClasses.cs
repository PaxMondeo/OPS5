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
        IWMClass Add(string className, string basedOn);
        IWMClass GetClass(string className);
        IWMClass? GetClassIfExists(string className);
        void Delete(string className);
        bool IsBaseClass(string className);
        bool IsPersistentClass(string className);
        bool IsIndividuallyPersistentClass(string className);
        Dictionary<string, IWMClass> GetClassesBasedOn(string className);
        void PrintClasses();
        List<IWMClass> GetEnabledClasses();
        List<IWMClass> GetPersistentClasses();
        List<string> ListClassNames();
    }
}
