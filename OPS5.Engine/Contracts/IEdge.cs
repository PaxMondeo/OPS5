using System;
using System.Collections.Generic;
using System.Text;

namespace OPS5.Engine.Contracts
{
    internal interface IEdgeFactory
    {
        IEdge GetEdge(string node1, string node2, string edgeID);
        IEdge GetEdge(string node1, string node2, string edgeID, string distance);
    }

    internal interface IEdge
    {
        string Node1 { get; set; }
        string Node2 { get; set; }
        string EdgeID { get; set; }
        double Distance { get; set; }
        void SetProperties(string node1, string node2, string edgeID);
        void SetProperties(string node1, string node2, string edgeID, string distance);
    }
}
