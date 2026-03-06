using AttributeLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OPS5.Engine.Contracts
{
    public interface IWorkingMemory
    {
        IAlphaNode AlphaRoot { get; set; }
        IBetaNode BetaRoot { get; set; }
        int TimeTag { get; }
        ConcurrentQueue<IWMElement> IncomingData { get; set; }
        event EventHandler<IWMElement> ObjectAdded;
        event EventHandler<IWMElement> ObjectChanged;
        event EventHandler<IWMElement> ObjectRemoved;

        void Reset(IAlphaNode alphaRoot, IBetaNode betaRoot);

        /// <summary>
        /// Returns a dictionary containing all objects in working memory
        /// </summary>
        Dictionary<int, IWMElement> GetWMEs();

        /// <summary>
        /// Returns a list of all objects of the given class
        /// </summary>
        List<IWMElement> ListWMEsByClass(string className);

        /// <summary>
        /// Returns a list of all objects of the given class whose attributes match the filter
        /// </summary>
        List<IWMElement> ListWMEsByClass(string className, List<string> attributeFilter);

        /// <summary>
        /// Returns a list of all objects of the given class whose attributes match the filter
        /// </summary>
        List<IWMElement> ListWMEsByClass(string className, AttributesCollection attributeFilter);

        List<IWMElement> ListWMEsByClassSinceLastCycle(string className);
        List<IWMElement> ListWMEsSinceLastCycle();
        IWMElement GetWME(int objectID);
        IWMElement? FindWME(string className, string id);
        List<int> MatchObjects(string className, string[] elements);
        List<int> MatchObjects(string className, AttributesCollection attributes);
        IWMElement? AddObject(string className, AttributesCollection attributes, bool modifying);
        IWMElement AddObject(string className, string[] elements);
        void AddObject(IWMElement iObject);

        IWMElement CopyObject(IWMElement iObject);
        bool RemoveObject(int objectID, bool signalEvent, bool isModify = false);
        void UpdateDatesTimes();
        void InjectObjects();
        void DoRemoveAll(List<string> atoms);
        void DoRemoveAll(string className);
        List<int> ListObjectIDs();
        void ListWM(string className);
        void ListWM(List<string> bits);
        void ListWM();
        bool WMEExists(int objectID);
        void AddObjectAsync(string className, string[] elements);
        void ModifyObject(IWMElement iObject);
        void ReplaceObject(int objectID, IWMElement newObject);
        IWMElement? AddOrUpdateObject(string className, AttributesCollection attributes, bool mergeDuplicates = false);
    }
}
