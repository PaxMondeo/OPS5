using System;
using System.Collections;

using System.Collections.Generic;
using System.Linq;

namespace AttributeLibrary
{
    public class AttributesCollection : IEnumerable<KeyValuePair<string, string?>>
    {
        private int _position = -1;

        /// <summary>
        /// Dictionary of attribute key value pairs
        /// </summary>
        public Dictionary<string, string?> _attributes { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// ctor
        /// </summary>
        public AttributesCollection()
        {

        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="attributes"></param>
        public AttributesCollection(Dictionary<string, string?> attributes)
        {
            _attributes = new Dictionary<string, string?>(attributes, StringComparer.OrdinalIgnoreCase);
        }

        public AttributesCollection(AttributesCollection attributes)
        {
            _attributes = new Dictionary<string, string?>(attributes.GetAttributes(), StringComparer.OrdinalIgnoreCase);
        }

        public AttributesCollection(IEnumerable<KeyValuePair<string, string?>> values)
        {
            _attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in values)
                _attributes[kvp.Key] = kvp.Value;
        }

        /// <summary>
        /// Returns the selected value as a string
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetVal(string key)
        {
            if (_attributes.ContainsKey(key) && _attributes[key] is string value)
                return value;
            else
                return "NIL";
        }

        /// <summary>
        /// Returns the selected value as a string or returns null
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string? TryGetVal(string key)
        {
            if (_attributes.ContainsKey(key))
                return _attributes[key];
            else
                return null;
        }

        public List<string?> GetValues()
        {
            return _attributes.Values.ToList();
        }

        public List<string> GetKeys()
        {
            return _attributes.Keys.ToList();
        }

        public Dictionary<string, string?> GetAttributes()
        {
            return new Dictionary<string, string?>(_attributes, StringComparer.OrdinalIgnoreCase);
        }

        public KeyValuePair<string, string?> ElementAt(int pos)
        {
            return _attributes.ElementAt(pos);
        }

        /// <summary>
        /// Adds an attribute key value pair
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, string? value)
        {
            _attributes.TryAdd(key, value);
        }

        /// <summary>
        /// Removes the selected attribute
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            _attributes.Remove(key);
        }

        /// <summary>
        /// Clears the collection
        /// </summary>
        public void Clear()
        {
            _attributes.Clear();
        }

        public void SetAttributeValue(string name, string? value)
        {
            if (_attributes.ContainsKey(name))
                _attributes[name] = value;
            else
            {
                _attributes.TryAdd(name, value);
            }
        }


        /// <summary>
        /// Indicates that the key exists in the collection
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return _attributes.ContainsKey(key);
        }


        public int Count { get { return _attributes.Count(); } }

        public bool MoveNext()
        {
            _position++;
            return (_position < _attributes.Count());
        }
        public void Reset()
        {
            _position = -1;
        }
        public object Current
        {
            get { return _attributes.ElementAt(_position); }
        }

        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
        {
            return _attributes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public AttributesCollection WhereKeyEquals(string key)
        {
            var col = _attributes.Where(a => a.Key == key);
            return new AttributesCollection(col);
        }
        public AttributesCollection WhereKeyNotEquals(string key)
        {
            var col = _attributes.Where(a => a.Key != key);
            return new AttributesCollection(col);
        }
    }
}