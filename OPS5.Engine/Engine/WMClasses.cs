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

        private int _classesCount = 0;

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
                _classesCount++;
                return newClass;
            }
        }

        public IWMClass Add(string className, string basedOn)
        {
            if (_WMClasses.ContainsKey(className))
            {
                throw new Exception($"Class {className} already exists");
            }
            else
            {
                IWMClass newClass = _WMClassFactory.NewClass(className);
                _WMClasses.Add(className, newClass);
                newClass.BasedOn = basedOn.ToUpper();
                newClass.IsBaseClass = false;
                newClass.ReadOnly = false;

                foreach (string attribute in _WMClasses[basedOn].GetUserAttributes())
                {
                    newClass.AddAttribute(attribute);
                }
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
        public IWMClass? GetClassIfExists(string className)
        {
            if (_WMClasses.ContainsKey(className))
                return _WMClasses[className];
            else
                return null;
        }
        internal List<IWMClass> List()
        {
            return _WMClasses.Values.ToList();
        }

        public Dictionary<string, IWMClass> GetClassesBasedOn(string className)
        {
            return _WMClasses.Where(c => c.Value.BasedOn == className).ToDictionary(c => c.Key, c => c.Value);
        }

        public void Delete(string className)
        {
            //Check to make sure no other class is inheriting from this class

            try
            {
                foreach (IWMClass iClass in _WMClasses.Values)
                    if (iClass.BasedOn == className)
                        _logger.WriteInfo($"Class {className} is the parent of {iClass.ClassName} - please review before reloading data", 0);

                // TODO: Check database adapters and CSV bindings that reference this class
                _WMClasses.Remove(className);
                _logger.WriteInfo($"Deleted class {className}", 0);
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "Delete");
            }

            _WMClasses.Remove(className);
        }

        public bool IsBaseClass(string className)
        {
            return _WMClasses[className].IsBaseClass;
        }

        public bool IsPersistentClass(string className)
        {
            return _WMClasses[className].IsPersistent && !_WMClasses[className].PersistIndividualObjects;
        }

        public bool IsIndividuallyPersistentClass(string className)
        {
            return _WMClasses[className].PersistIndividualObjects;
        }

        public void PrintClasses()
        {
            foreach (WMClass iclass in _WMClasses.Values)
            {
                string message = $"Class {iclass.ClassName}\t";
                if (!iclass.Enabled)
                    message += "DISABLED";
                message += "\n";
                foreach (string attribute in iclass.GetAttributes())
                {
                    message += $"{attribute}\t";
                }
                message += "\n";
                Console.WriteLine(message);
            }
        }

        public List<IWMClass> GetEnabledClasses()
        {
            return _WMClasses.Values.Where(_ => _.Enabled == true).ToList();
        }

        public List<IWMClass> GetPersistentClasses()
        {
            return _WMClasses.Values.Where(_ => _.IsPersistent == true).ToList();
        }

        public List<string> ListClassNames()
        {
            return _WMClasses.Keys.ToList();
        }
    }

}
