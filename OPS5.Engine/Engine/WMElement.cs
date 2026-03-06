using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using OPS5.Engine.Contracts;
using OPS5.Engine.Models;
using AttributeLibrary;
using System.Text.RegularExpressions;

namespace OPS5.Engine
{
    internal class WMElementFactory : IWMElementFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private IObjectIDs _objectIDs;

        public WMElementFactory(IServiceProvider serviceProvider, IObjectIDs objectIDs)
        {
            _serviceProvider = serviceProvider;
           _objectIDs = objectIDs;
        }
        public IWMElement NewObject()
        {
            var o = _serviceProvider.GetService(typeof(IWMElement));
            if (o == null)
                throw new Exception("Could not instantiate new WMElement");
            IWMElement iObject = (IWMElement)o;
            iObject.ID = _objectIDs.NextObjectID();
            return iObject;
        }

        public IWMElement NewObject(string className)
        {
            var iObject = NewObject();
            iObject.ClassName = className;
            return iObject;
        }
    }

    /// <summary>
    /// WMElement represents an Object in OPS5 working memory
    /// </summary>
    internal class WMElement : IWMElement
    {
        private IOPS5Logger _logger;
        private IWMClasses _WMClasses;
        private IClassRelationships _classRelationships;

        /// <summary>
        /// Unique Integer ID of the Object
        /// </summary>
        public int ID { get; set;  }
        /// <summary>
        /// Class of which the Object is an instance
        /// </summary>
        public string ClassName
        {
            get { return _className; }
            set { _className = value.ToUpper(); }
        }
        private string _className = string.Empty;
        /// <summary>
        /// Dictionary of the Objects attributes and their values
        /// </summary>
        private AttributesCollection _attributes;
        /// <summary>
        /// List of Alpha nodes that currently contain this Object
        /// </summary>
        private List<int> _alphaNodes;
        /// <summary>
        /// List of Tokens that currently contain this Object
        /// </summary>
        private List<int> _tokens; 
        /// <summary>
        /// The system Time Tag when this Object was created
        /// </summary>
        public int TimeTag { get; set; }

        public WMElement(IOPS5Logger logger, IWMClasses WMClasses, IClassRelationships classRelationships)
        {
            _logger = logger;
            _WMClasses = WMClasses;
            _attributes = new AttributesCollection();
            _alphaNodes = new List<int>();
            _tokens = new List<int>();
            _classRelationships = classRelationships;
        }

        public void ProcessAttributes(AttributesCollection attributes)
        {
            try 
            {
                if (_className == null)
                    throw new Exception("Need to set class name before processing elements");

                IWMClass iClass = _WMClasses.GetClass(ClassName);
                //Loop through the attributes of this class, and fill in any missing ones
                foreach (string classAtt in iClass.GetAttributes())
                {
                    if (classAtt.ToUpper() == "ID")
                    {
                        if (attributes.ContainsKey("ID"))
                        {
                            _attributes.Add("ID", attributes.GetVal("ID"));
                            if (int.TryParse(attributes.GetVal("ID"), out int id))
                            {
                                iClass.TrySetObjectCount(id);
                            }
                        }
                        else
                        {
                            _attributes.Add("ID", iClass.NextObjectID());
                        }
                    }
                    else
                    {
                        bool done = false;
                        foreach (KeyValuePair<string, string?> attribute in attributes.GetAttributes().Where(_ => _.Key.ToUpper() == classAtt.ToUpper()))
                        {
                            string attName = attribute.Key.ToUpper();
                            if (attribute.Value == "")
                                _attributes.Add(attName, "NIL");
                            else
                            {
                                _attributes.Add(attName, Utilities.Formatting.CheckForDateTime(attribute.Value));
                            }
                            done = true;
                            break;
                        }
                        if (!done)
                        {
                            _attributes.Add(classAtt.ToUpper(), "NIL");
                        }
                    }
                }

                foreach (KeyValuePair<string, string?> attribute in attributes.GetAttributes().Where(_ => _.Key.ToUpper().EndsWith("ID")))
                {
                    string attName = attribute.Key.ToUpper();
                    string parentClass = attName.Substring(0, attName.Length - 2);
                    if (_WMClasses.ClassExists(parentClass))
                    {
                        _attributes.Add(attName, attribute.Value);

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "Object");
            }
        }
        public void ProcessElements(string[] elements)
        {
            try
            {
                if (_className == null)
                    throw new Exception("Need to set class name before processing elements");

                //Loop through the attributes of this class, and fill in any missing ones
                foreach (string classAtt in _WMClasses.GetClass(ClassName).GetAttributes())
                {
                    if (classAtt.ToUpper() == "ID")
                        _attributes.Add("ID", _WMClasses.GetClass(ClassName).NextObjectID());
                    else
                    {
                        bool done = false;
                        for (int x = 0; x < elements.Count(); x += 2)
                        {
                            string attributeName = elements[x].ToUpper();
                            if (attributeName == classAtt.ToUpper())
                            {
                                string value = elements[x + 1];
                                if (value == "")
                                    _attributes.Add(attributeName, "NIL");
                                else
                                {
                                    string dataType = _WMClasses.GetClass(_className).GetAttributeType(attributeName);
                                    switch (dataType)
                                    {
                                        case "DATE":
                                        case "DATETIME":
                                            _attributes.Add(attributeName, Utilities.Formatting.CheckForDateTime(value));
                                            break;

                                        default:
                                            _attributes.Add(attributeName, value);
                                            break;
                                    }
                                }
                                done = true;
                                break;
                            }
                        }
                        if (!done)
                        {
                            _attributes.Add(classAtt.ToUpper(), "NIL");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.Message, "Object");
            }

        }
        public void Copy(IWMElement source)
        {
            ClassName = source.ClassName;
            foreach (KeyValuePair<string, string?> attr in source.GetAttributes())
            {
                _attributes.Add(attr.Key, attr.Value);
            }
        }

        public string? AttributeValue(string attr)
        {
            return _attributes.GetVal(attr);
        }


        public void AddAlphaNode(int node)
        {
            lock (_alphaNodes)
            {
                _alphaNodes.Add(node);
            }
        }

        public AttributesCollection GetAttributes()
        {
            //Return a fresh collection with copies of the attributes, not the original, to avoid it getting trashed.
            return new AttributesCollection(_attributes);
        }

        public AttributesCollection GetUserAttributes()
        {
            //Return a fresh dictionary with copies of the attributes, not the original, to avoid it getting trashed.
            string? parentClass = _classRelationships.GetParent(_className);
            if(parentClass == null)
            {
                return new AttributesCollection(_attributes.WhereKeyNotEquals("ID"));
            }
            else
                return new AttributesCollection(_attributes.WhereKeyNotEquals("ID").WhereKeyNotEquals($"{parentClass}ID"));
        }

        public string? GetAttributeValue(string attribute)
        {
            if (_attributes.ContainsKey(attribute))
            {
                string? val = _attributes.GetVal(attribute);
                string dataType = _WMClasses.GetClass(_className).GetAttributeType(attribute);
                switch (dataType)
                {
                    case "DATE":
                    case "DATETIME":
                        return Utilities.Formatting.CheckForDateTime(val);

                    default:
                        return val;
                }
            }
            else
                return "";
        }

        public List<string?> GetAttributeValues()
        {
            return _attributes.GetValues();
        }

        public List<string?> GetUserAttributeValues()
        {
            AttributesCollection filteredAttributes;
            //Return a fresh dictionary with copies of the attributes, not the original, to avoid it getting trashed.
            string? parentClass = _classRelationships.GetParent(_className);
            if (parentClass == null)
                filteredAttributes = _attributes.WhereKeyNotEquals("ID");
            else
                filteredAttributes = _attributes.WhereKeyNotEquals("ID").WhereKeyNotEquals($"{parentClass}ID");
            return filteredAttributes.GetValues();
        }

        public void SetAttributeValue(string attribute, string value)
        {
            var iClass = _WMClasses.GetClass(ClassName);
            if (iClass.AttributeExists(attribute))
                _attributes.SetAttributeValue(attribute, value);
            else
                _logger.WriteError($"Attempt to set value of non existent attribute {attribute} in object of class {ClassName}", "Object");
        }

        public bool AddToken(int id)
        {
            try
            {
                lock (_tokens)
                {
                    _tokens.Add(id);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return true;
        }

        public string GetAttributeType(string attributeName)
        {
            return _WMClasses.GetClass(_className).GetAttributeType(attributeName);
        }

        public bool IsPersistent()
        {
            return _WMClasses.GetClass(ClassName).IsPersistent;
        }

        public bool PersistIndividualObjects()
        {
            return _WMClasses.GetClass(ClassName).PersistIndividualObjects;
        }

        public bool HasAttribute(string attributeName)
        {
            return _attributes.ContainsKey(attributeName);
        }

        public bool HasChildClass(string attributeName)
        {
            var item = _attributes.Where(k => k.Key.ToUpper().StartsWith(attributeName.ToUpper())).FirstOrDefault();
            if (item.Key == null)
                return false;
            else
            {
                var child = item.Key;
                Match match = Regex.Match(child, "(?<=\\[).+?(?=\\])");
                return match.Success;
            }

        }
    }
}
