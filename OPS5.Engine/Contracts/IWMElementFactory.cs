using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    public interface IWMElementFactory
    {
        IWMElement NewObject();
        IWMElement NewObject(string className);
    }
}
