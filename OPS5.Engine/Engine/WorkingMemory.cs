using OPS5.Engine.Contracts;
using AttributeLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OPS5.Engine
{
    internal class WorkingMemory : IWorkingMemory
    {
        private IOPS5Logger _logger;
        private readonly IWMElementFactory _objectFactory;
        private IWMClasses _WMClasses;

        /// <summary>
        /// Dictionary of Working Memory Elements currently in existence
        /// </summary>
        private Dictionary<int, IWMElement> _wmes { get; set; } = new Dictionary<int, IWMElement>();

        /// <summary>
        /// The root Alpha node through which all Objects are added
        /// </summary>
        public IAlphaNode AlphaRoot { get; set; } = default!;

        /// <summary>
        /// The root Beta node that generates all empoty Tokens to feed into the network
        /// </summary>
        public IBetaNode BetaRoot { get; set; } = default!;

        /// The current system Time Tag
        /// </summary>
        private int _timeTag;
        public int TimeTag => _timeTag;
        public event EventHandler<IWMElement> ObjectAdded = default!;
        public event EventHandler<IWMElement> ObjectChanged = default!;
        public event EventHandler<IWMElement> ObjectRemoved = default!;

        public WorkingMemory(IOPS5Logger logger,
                             IWMElementFactory objectFactory,
                             IWMClasses WMClasses)
        {
            _logger = logger;
            _objectFactory = objectFactory;
            _WMClasses = WMClasses;
        }

        public void Reset(IAlphaNode alphaRoot, IBetaNode betaRoot)
        {
            _wmes = new Dictionary<int, IWMElement>();
            AlphaRoot = alphaRoot;
            BetaRoot = betaRoot;
            _timeTag = 1;
        }




        public Dictionary<int, IWMElement> GetWMEs()
        {
            return new Dictionary<int, IWMElement>(_wmes);
        }

        public List<IWMElement> ListWMEsByClass(string className)
        {
            return _wmes.Where(wme => wme.Value.ClassName == className.ToUpper()).ToDictionary(wme => wme.Key, wme => wme.Value).Values.ToList();
        }

        public List<IWMElement> ListWMEsByClass(string className, List<string> attributeFilter)
        {
            List<IWMElement> objects = _wmes.Where(wme => wme.Value.ClassName == className.ToUpper()).ToDictionary(wme => wme.Key, wme => wme.Value).Values.ToList();
            for (int a = 0; a < attributeFilter.Count; a++)
            {
                string attName = attributeFilter[a++];
                string attVal = attributeFilter[a].ToUpper();
                if (attVal.EndsWith("*"))
                {
                    objects = objects.Where(w => w.GetAttributeValue(attName) is string a && a.StartsWith(attVal.Substring(0, attVal.Length - 1))).ToList();
                }
                else
                {
                    objects = objects.Where(w => w.GetAttributeValue(attName) == attVal).ToList();
                }
            }
            return objects;
        }

        public List<IWMElement> ListWMEsByClass(string className, AttributesCollection attributeFilter)
        {
            List<IWMElement> objects = _wmes.Where(wme => wme.Value.ClassName == className.ToUpper()).ToDictionary(wme => wme.Key, wme => wme.Value).Values.ToList();
            foreach(KeyValuePair<string, string?> attFil in attributeFilter)
            {
                if (attFil.Value is string af &&  af.EndsWith("*"))
                {
                    objects = objects.Where(w => w.GetAttributeValue(attFil.Key) is string a && a.StartsWith(attFil.Value.Substring(0, attFil.Value.Length - 1))).ToList();
                }
                else
                {
                    objects = objects.Where(w => w.GetAttributeValue(attFil.Key) == attFil.Value).ToList();
                }
            }
            return objects;
        }


        public IWMElement GetWME(int objectID)
        {
            if (!_wmes.TryGetValue(objectID, out var wme))
                throw new KeyNotFoundException($"Working memory object #{objectID} not found");
            return wme;
        }

        /// <summary>
        /// Returns a list of the IDs of all objects of the given class that match the given elements
        /// </summary>
        /// <param name="className"></param>
        /// <param name="elements"></param>
        /// <returns></returns>
        public List<int> MatchObjects(string className, string[] elements)
        {
            //Start filter to see if we have this Object
            IEnumerable<IWMElement> result = _wmes.Select(w => w.Value).Where(w => w.ClassName == className.ToUpper());

            for (int x = 0; x < elements.Count() - 1; x += 2)
            {
                result = FilterObjects(result, elements[x], elements[x + 1]);
            }

            List<IWMElement> iObjects = result.ToList();
            List<int> response = new List<int>();
            foreach (IWMElement iObject in iObjects)
                response.Add(iObject.ID);
            return response;
        }

        /// <summary>
        /// Returns a list of the IDs of all objects of the given class that match the given attributes
        /// </summary>
        /// <param name="className"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public List<int> MatchObjects(string className, AttributesCollection attributes)
        {
            //Start filter to see if we have this Object
            IEnumerable<IWMElement> result = _wmes.Select(w => w.Value).Where(w => w.ClassName == className.ToUpper());

            foreach (KeyValuePair<string, string?> attribute in attributes)
            {
                result = FilterObjects(result, attribute.Key, attribute.Value);
            }

            List<IWMElement> iObjects = result.ToList();
            List<int> response = new List<int>();
            foreach (IWMElement iObject in iObjects)
                response.Add(iObject.ID);
            return response;
        }

        /// <summary>
        /// Adds an Object to working memory
        /// Accepts the class name of the Object and a Dictionary of elements mapping values to attributes
        /// </summary>
        /// <param name="className"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public IWMElement? AddObject(string className, AttributesCollection attributes, bool modifying)
        {
            IWMElement? wme = null;
            if (_WMClasses.ClassExists(className))
            {

                wme = _objectFactory.NewObject(className);
                _wmes[wme.ID] = wme;
                wme.TimeTag = ++_timeTag;
                wme.ProcessAttributes(attributes);

                AlphaRoot.AddObject(wme.ID);
                if (_logger.Verbosity > 1)
                {
                    string message = $"Created Object #{wme.ID} for {wme.ClassName}";
                    foreach (KeyValuePair<string, string?> attr in wme.GetAttributes())
                    {
                        message += $"\n {attr.Key} {attr.Value}";
                    }
                    _logger.WriteInfo(message, 2);
                }

                if (modifying)
                    ObjectChanged?.Invoke(this, wme);
                else
                    ObjectAdded?.Invoke(this, wme);
            }
            else
                _logger.WriteError($"Attempted to create object for non-existent Class {className}", "WorkingMemory.AddObject");
            return wme;
        }

        /// <summary>
        /// Adds a new object to WM
        /// </summary>
        /// <param name="iObject"></param>
        public void AddObject(IWMElement iObject)
        {
            iObject.TimeTag = ++_timeTag;
            _wmes[iObject.ID] = iObject;
            AlphaRoot.AddObject(iObject.ID);
            ObjectAdded?.Invoke(this, iObject);
        }

        public void ModifyObject(IWMElement iObject)
        {
            var id = iObject.ID;
            var newObject = CopyObject(iObject);
            AlphaRoot.AddObject(newObject.ID);
            RemoveObject(id, true, true);
        }

        public void ReplaceObject(int objectID, IWMElement newObject)
        {
            if (objectID == newObject.ID)
                _logger.WriteError($"Attempt to replace object {objectID} with itself.", "Working Memory");
            else
            {
                AlphaRoot.AddObject(newObject.ID);
                if (!RemoveObject(objectID, false))
                    _logger.WriteError($"Attempt to replace non existent object {objectID}.", "Working Memory");
            }
        }

        /// <summary>
        /// Creates a new instance of an Object
        /// </summary>
        /// <param name="iObject"></param>
        /// <returns></returns>
        public IWMElement CopyObject(IWMElement iObject)
        {
            IWMElement newObject = default!;
            newObject = _objectFactory.NewObject();
            newObject.TimeTag = ++_timeTag;
            newObject.Copy(iObject);
            _wmes[newObject.ID] = newObject;

            ObjectAdded?.Invoke(this, newObject);

            return newObject;
        }

        /// <summary>
        /// Removes the passed Object from working memory
        /// </summary>
        /// <param name="objectID"></param>
        public bool RemoveObject(int objectID, bool signalEvent, bool isModify = false)
        {
            if (_wmes.ContainsKey(objectID))
            {
                IWMElement iObject = _wmes[objectID];
                try
                {
                    AlphaRoot.RemoveObject(objectID);
                    bool result = _wmes.Remove(objectID);

                    if(signalEvent)  // If this is removing an object prior to replacing it, don't bother signalling its removal
                        ObjectRemoved?.Invoke(this, iObject);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.WriteError($"{ex.Message} when trying to remove Object {objectID}", "RemoveObject");
                    return false;
                }
            }
            else
                throw new Exception($"Unable to remove object ID {objectID}, that object does not exist.");
        }


        /// <summary>
        /// Returns a list of objects whose attribute values match the filter value
        /// </summary>
        /// <param name="iObjects"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private IEnumerable<IWMElement> FilterObjects(IEnumerable<IWMElement> iObjects, string attribute, string value)
        {
            IEnumerable<IWMElement> result = iObjects.Where(w => w.GetAttributeValue(attribute) == value);
            return result;
        }

        /// <summary>
        /// Adds an Object to working memory
        /// Accepts the class name of the Object and an array alternating between Attribute and Value
        /// </summary>
        /// <param name="className"></param>
        /// <param name="elements"></param>
        public IWMElement AddObject(string className, string[] elements)
        {
            IWMElement wme = default!;
            if (_WMClasses.ClassExists(className))
            {

                wme = _objectFactory.NewObject();
                _wmes[wme.ID] = wme;
                wme.TimeTag = ++_timeTag;
                wme.ClassName = className;
                wme.ProcessElements(elements);
                var watch = Stopwatch.StartNew();
                AlphaRoot.AddObject(wme.ID);
                watch.Stop();
                if (watch.ElapsedMilliseconds > 1000)
                    Console.Write(".");
                if (_logger.Verbosity > 1)
                {
                    string message = $"Created Object #{wme.ID} for {wme.ClassName}";
                    foreach (KeyValuePair<string, string?> attr in wme.GetAttributes())
                    {
                        message += $"\n {attr.Key} {attr.Value}";
                    }
                    _logger.WriteInfo(message, 2);
                }

                ObjectAdded?.Invoke(this, wme);
            }
            else
                _logger.WriteError($"Attempted to create object for non-existent Class {className}", "AddObject");
            return wme;
        }

        public void ListWM(string className)
        {
            string message;
            try
            {
                if (_WMClasses.ClassExists(className))
                {
                    IWMClass wmClass = _WMClasses.GetClass(className);
                    message = $"\nObjects of Class {className}\nObject#\t";

                    Dictionary<int, IWMElement> iObjects2 = _wmes.Where(w => w.Value.ClassName == className.ToUpper())
                                                                    .ToDictionary(w => w.Key, w => w.Value);
                    List<List<string>> cols = ListAttributes(wmClass, iObjects2);
                    List<int> colLens = new List<int>();
                    colLens.Add(7);
                    int x = 1;
                    foreach (string attribute in wmClass.GetAttributes())
                    {
                        string attName = attribute;
                        int colLen = BalanceTabs(ref attName, cols[x++]);
                        message += $"{attName}\t";
                        colLens.Add(colLen);
                    }
                    colLens.Add(0);
                    Console.WriteLine(message + "TimeTag");

                    printAttributeVals(cols, colLens, x);
                }
                else
                    _logger.WriteError($"Unknown class {className}", "ListWM");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void ListWM(List<string> bits)
        {
            //WM can have ClassName followed by any number of attribute value pairs

            if (bits[1] is string className)
            {
                IWMClass wmClass = _WMClasses.GetClass(className);
                string message = $"Objects of Class {className}\nObject#\t";

                Dictionary<int, IWMElement> iObjects2 = _wmes
                                                   .Where(w => w.Value.ClassName == className.ToUpper())
                                                   .ToDictionary(w => w.Key, w => w.Value);

                for (int z = 2; z < bits.Count(); z += 2)
                {
                    try
                    {
                        string attName = bits[z].ToUpper();
                        string? attVal = bits[z + 1];
                        ArgumentNullException.ThrowIfNull(attVal);
                        iObjects2 = iObjects2.Where(w => w.Value.GetAttributeValue(attName) is string a && a.ToUpper() == attVal.ToUpper()).ToDictionary(w => w.Key, w => w.Value);
                    }
                    catch (Exception)
                    {
                        _logger.WriteError($"Class {bits[1]} does not have an attribute {bits[z]}", "Main");
                    }
                }
                List<List<string>> cols = ListAttributes(wmClass, iObjects2);
                List<int> colLens = new List<int>();
                colLens.Add(7);
                int x = 1;
                foreach (string attribute in wmClass.GetAttributes())
                {
                    string attName = attribute;
                    int colLen = BalanceTabs(ref attName, cols[x++]);
                    message += $"{attName}\t";
                    colLens.Add(colLen);
                }
                colLens.Add(0);
                Console.WriteLine(message + "TimeTag");

                printAttributeVals(cols, colLens, x);
            }
        }


        private void printAttributeVals(List<List<string>> cols, List<int> colLens, int x)
        {
            try
            {
                for (int y = 0; y < cols[0].Count; y++)
                {
                    for (int z = 0; z < x + 1; z++)
                    {
                        string val = cols[z][y];
                        if (val.Length > 500)
                            val = val.Substring(0, 500);
                        Console.Write($"{val}");
                        for (int t = val.Length; t < colLens[z]; t += 8)
                            Console.Write("\t");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"WorkingMemory display error: {ex.Message}");
            }
        }

        private int BalanceTabs(ref string attributeName, List<string> col)
        {
            int colLen = attributeName.Length;
            int maxValLen = 0;
            foreach(string val in col)
            {
                if (val.Length > maxValLen)
                    maxValLen = val.Length;
                if (maxValLen > 500)
                    maxValLen = 500;
            }
            int nameTabs = colLen / 8 + 1;
            int valTabs = maxValLen / 8 + 1;
            int diff = valTabs- nameTabs;
            colLen = (colLen / 8) + 1;
            colLen = colLen * 8;
            if (diff > 0)
            {
                for (int y = 0; y < diff; y++)
                {
                    attributeName += "\t";
                    colLen += 8;
                }
            }

            return colLen;
        }

        private List<List<string>> ListAttributes(IWMClass WMClass, Dictionary<int, IWMElement> wmes)
        {
            List<List<string>> cols = new List<List<string>>();
            for (int c = 0; c < WMClass.GetAttributes().Count + 2; c++)
            {
                cols.Add(new List<string>());
            }
            foreach (KeyValuePair<int, IWMElement> wPair in wmes)
            {
                int x = 0;
                cols[x++].Add( $"#{wPair.Key}");
                foreach (string attribute in WMClass.GetAttributes())
                {
                    string? val = wPair.Value.GetAttributeValue(attribute);
                    cols[x++].Add($"{val}");
                }
                cols[x++].Add($"{wPair.Value.TimeTag}");
            }
            return cols;
        }

        public void ListWM()
        {
            foreach (WMClass opsClass in _WMClasses.GetClasses())
            {
                ListWM(opsClass.ClassName);
            }
        }

        public bool WMEExists(int objectID)
        {
            return _wmes.ContainsKey(objectID);
        }

    }

}
