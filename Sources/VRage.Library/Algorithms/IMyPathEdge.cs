using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Algorithms
{
    public interface IMyPathEdge<V>
    {
        float GetWeight();

        /// <summary>
        /// Returns the other vertex on this edge.
        /// Can return null, if the edge is a loop or if the edge is not traversable
        /// </summary>
        V GetOtherVertex(V vertex1);
    }
}
