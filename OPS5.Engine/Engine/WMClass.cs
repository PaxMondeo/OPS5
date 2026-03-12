using System;
using System.Collections.Generic;
using System.Linq;
using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    internal class WMClassFactory : IWMClassFactory
    {
        IServiceProvider _serviceProvider;
        public WMClassFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWMClass NewClass(string className)
        {
            var c = _serviceProvider.GetService(typeof(IWMClass));
            if (c == null)
                throw new Exception("Unable to instantiate new WMClass");
            IWMClass newClass = (IWMClass)c;
            newClass.ClassName = className;
            return newClass;
        }
    }
    /// <summary>
    /// WM Class
    /// </summary>
    internal class WMClass : IWMClass
    {
        private IOPS5Logger _logger;
        private HashSet<string> _attributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _vectorAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Name of the Class
        /// </summary>
        private string _className = string.Empty;

        public string ClassName
        {
            get
            {
                return _className;
            }
            set
            {
                _className = value.ToUpper();
            }
        }

        private int _objectCount = 0;

        public WMClass(IOPS5Logger logger)
        {
            _logger = logger;
            _attributes.Add("ID");
        }


        public void AddAttribute(string attribute)
        {
            _attributes.Add(attribute.ToUpper());
        }

        public void AddAttributes(List<string> attributes)
        {
            foreach (string attribute in attributes)
            {
                string attr = attribute.ToUpper().Trim();
                try
                {
                    _attributes.Add(attr);
                }
                catch (Exception ex)
                {
                    _logger.WriteError($"Could not add attribute {attr} to class {ClassName}, {ex.Message}", "AddAttributes");
                }
            }
        }

        public bool AttributeExists(string attributeName)
        {
            attributeName = attributeName.ToUpper();
            if (attributeName == "ID" || attributeName == "CLASS")
                return true;
            else return _attributes.Contains(attributeName);
        }

        public string NextObjectID()
        {
            _objectCount++;
            return _objectCount.ToString();
        }

        public void TrySetObjectCount(int id)
        {
            if (id > _objectCount)
                _objectCount = id;
        }

        public List<string> GetUserAttributes()
        {
            return _attributes.Where(_ => _ != "ID").ToList();
        }

        public List<string> GetAttributes()
        {
            return _attributes.ToList();
        }

        public void SetDefaults(Dictionary<string, string> defaults)
        {
            foreach (var kvp in defaults)
                _defaults[kvp.Key.ToUpper()] = kvp.Value;
        }

        public string? GetDefaultValue(string attributeName)
        {
            if (_defaults.TryGetValue(attributeName.ToUpper(), out string? value))
                return value;
            return null;
        }

        public void SetVectorAttribute(string attributeName)
        {
            _vectorAttributes.Add(attributeName.ToUpper());
        }

        public bool IsVectorAttribute(string attributeName)
        {
            return _vectorAttributes.Contains(attributeName.ToUpper());
        }
    }

}
