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
        private IClassRelationships _classRelationships;
        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
        /// Optional parent class that this class inherits from
        /// </summary>
        public string BasedOn { get; set; } = "";
        /// <summary>
        /// True if this is a Base Class
        /// </summary>
        public bool IsBaseClass { get; set; } = false;
        /// <summary>
        /// Author's comment describing the Class
        /// </summary>
        public string Comment { get; set; } = "";
        /// <summary>
        /// True if this Class is enabled for use
        /// </summary>
        public bool Enabled { get; set; } = true;
        public string ClassFile { get; set; } = string.Empty;

        public bool ReadOnly { get; set; } = false;
        public bool IsPersistent { get; set; } = false;
        public bool PersistIndividualObjects { get; set; } = false;

        private int _objectCount = 0;

        public WMClass(IOPS5Logger logger,
                          IClassRelationships classRelationships)
        {
            _logger = logger;
            _classRelationships = classRelationships;
            _attributes.Add("ID","NUMBER");
        }


        public void AddAttribute(string attribute, string dataType = "GENERAL")
        {
            _attributes.Add(attribute.ToUpper(), dataType);
        }

        public void AddAttributes(List<string> attributes)
        {
            AddAttributes(attributes, true);
        }

        private void AddAttributes(List<string> attributes, bool isInternal)
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
            return AttributeExists(attributeName, true);
        }
        private bool AttributeExists(string attributeName, bool isInternal)
        {
            attributeName = attributeName.ToUpper();
            if (attributeName == "ID" || attributeName == "CLASS")
                return true;
            else return _attributes.ContainsKey(attributeName);
        }

        public string NextObjectID()
        {
            return NextObjectID(true);
        }

        private string NextObjectID(bool isInternal)
        {
            _objectCount++;
            return _objectCount.ToString();
        }

        public void TrySetObjectCount(int id)
        {
            TrySetObjectCount(id, true);
        }

        private void TrySetObjectCount(int id, bool isInternal)
        {
            if (id > _objectCount)
                _objectCount = id;
        }

        public List<string> GetUserAttributes()
        {
            return GetUserAttributes(true);
        }

        private List<string> GetUserAttributes(bool isInternal)
        {
            return _attributes.Keys.Where(_ => _ != "ID").ToList();
        }

        public List<string> GetAttributes()
        {
            return GetAttributes(true);
        }

        private List<string> GetAttributes(bool isInternal)
        {
            return _attributes.Keys.ToList();
        }

        public bool HasClassAttribute(string attributeName)
        {
            return HasClassAttribute(attributeName, true);
        }

        private bool HasClassAttribute(string attributeName, bool isInternal)
        {
            bool has = _classRelationships.HasChild(ClassName, attributeName);
            return has;
        }

        public string? GetSubClass(string attributeName)
        {
            return GetSubClass(attributeName, true);
        }

        private string? GetSubClass(string attributeName, bool isInternal)
        {
            return _classRelationships.GetChildClass(ClassName, attributeName);
        }

        public bool HasComplexAttribute(string attributePrefix)
        {
            return HasComplexAttribute(attributePrefix, true);
        }

        private bool HasComplexAttribute(string attributePrefix, bool isInternal)
        {
            bool any = _attributes.Keys.Where(_ => _.StartsWith(attributePrefix.ToUpper() + ".")).Any();
            return any;
        }

        public string GetAttributeType(string attributeName)
        {
            return GetAttributeType(attributeName, true);
        }

        private string GetAttributeType(string attributeName, bool isInternal)
        {
            if (_attributes.ContainsKey(attributeName))
                return _attributes[attributeName];
            else
            {
                return ""; //Default is to just return the raw value if this function can't identify a type.
            }
        }
    }

}
