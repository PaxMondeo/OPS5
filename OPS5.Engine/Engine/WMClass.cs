using System;
using System.Collections;
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
        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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

        /// <summary>
        /// Author's comment describing the Class
        /// </summary>
        public string Comment { get; set; } = "";
        /// <summary>
        /// True if this Class is enabled for use
        /// </summary>
        public bool Enabled { get; set; } = true;
        public string ClassFile { get; set; } = string.Empty;

        private int _objectCount = 0;

        public WMClass(IOPS5Logger logger)
        {
            _logger = logger;
            _attributes.Add("ID","NUMBER");
        }


        public void AddAttribute(string attribute, string dataType = "GENERAL")
        {
            _attributes.Add(attribute.ToUpper(), dataType);
        }

        public void AddAttributes(List<string> attributes)
        {
            foreach (string attribute in attributes)
            {
                string attr = attribute.ToUpper();
                if (attr.Contains(":"))
                {
                    attr = attr.Substring(0, attr.IndexOf(':'));
                    attr = attr.Trim();
                    try
                    {
                        _attributes.Add(attr, "VECTOR");
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteError($"Could not add Vector attribute {attr} to class {ClassName}, {ex.Message}", "AddAttributes");
                    }
                }
                else if (attr.Contains(" "))
                {
                    string[] atts = attr.Split(' ');
                    if (atts.Length > 1)
                    {
                        string att0 = atts[0].Trim();
                        string att1 = atts[1].Trim();
                        switch (att1)
                        {
                            case "DATE":
                            case "TIME":
                            case "DATETIME":
                            case "NUMBER":
                            case "TEXT":
                                try
                                {
                                    _attributes.Add(att0, att1);
                                }
                                catch (Exception ex)
                                {
                                    _logger.WriteError($"Could not add {att1} attribute {att0} to class {ClassName}, {ex.Message}", "AddAttributes");
                                }
                                break;
                            default:
                                try
                                {
                                    _attributes.Add(attr, "GENERAL");
                                }
                                catch (Exception ex)
                                {
                                    _logger.WriteError($"Could not add attribute {attr} to class {ClassName}, {ex.Message}", "AddAttributes");
                                }
                                break;
                        }
                    }
                }
                else
                    try
                    {
                        _attributes.Add(attr, "GENERAL");
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
            else return _attributes.ContainsKey(attributeName);
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
            return _attributes.Keys.Where(_ => _ != "ID").ToList();
        }

        public List<string> GetAttributes()
        {
            return _attributes.Keys.ToList();
        }

        public string GetAttributeType(string attributeName)
        {
            if (_attributes.ContainsKey(attributeName))
                return _attributes[attributeName];
            else
            {
                return ""; //Default is to just return the raw value if this function can't identify a type.
            }
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

        public void SetAttributeType(string attributeName, string dataType)
        {
            string key = attributeName.ToUpper();
            if (_attributes.ContainsKey(key))
                _attributes[key] = dataType;
        }
    }

}
