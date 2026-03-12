using OPS5.Engine.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OPS5.Engine
{
    internal class WMClasses : IWMClasses
    {
        private IWMClassFactory _WMClassFactory;
        private readonly IOPS5Logger _logger;
        /// <summary>
        /// Dictionary of Classes that have been defined
        /// </summary>
        private Dictionary<string, IWMClass> _WMClasses { get; set; } = new Dictionary<string, IWMClass>();

        public WMClasses(IWMClassFactory WMClassFactory, IOPS5Logger OPS5Logger)
        {
            _WMClassFactory = WMClassFactory;
            _logger = OPS5Logger;

            Reset();
        }

        public bool ClassExists(string className)
        {
            return _WMClasses.ContainsKey(className);
        }

        public void Reset()
        {
            _WMClasses = new Dictionary<string, IWMClass>(StringComparer.OrdinalIgnoreCase);
        }

        public IWMClass Add(string className)
        {
           if (_WMClasses.ContainsKey(className))
            {
                throw new Exception($"Class {className} already exists");
            }
            else
            {
                IWMClass newClass = _WMClassFactory.NewClass(className);
                _WMClasses.Add(className, newClass);
                return newClass;
            }
        }

        public IWMClass Add(string className, List<string> attributes)
        {
            if (_WMClasses.ContainsKey(className))
            {
                throw new Exception($"Class {className} already exists");
            }
            else
            {
                IWMClass newClass = _WMClassFactory.NewClass(className);
                _WMClasses.Add(className, newClass);
                newClass.AddAttributes(attributes);
                return newClass;
            }
        }

        public IWMClass GetClass(string className)
        {
            return _WMClasses[className];
        }
        internal List<IWMClass> List()
        {
            return _WMClasses.Values.ToList();
        }

        public void Delete(string className)
        {
            try
            {
                _WMClasses.Remove(className);
                _logger.WriteInfo($"Deleted class {className}", 0);
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "Delete");
            }
        }

        public void PrintClasses()
        {
            foreach (WMClass iclass in _WMClasses.Values)
            {
                string message = $"Class {iclass.ClassName}\t\n";
                foreach (string attribute in iclass.GetAttributes())
                {
                    message += $"{attribute}\t";
                }
                message += "\n";
                Console.WriteLine(message);
            }
        }

        public List<IWMClass> GetClasses()
        {
            return _WMClasses.Values.ToList();
        }

    }

}
