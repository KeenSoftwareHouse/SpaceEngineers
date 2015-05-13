using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Algorithms
{
    public class MyPath<V> : IEnumerable<MyPath<V>.PathNode>
        where V : class, IMyPathVertex<V>, IEnumerable<IMyPathEdge<V>>
    {
        public struct PathNode
        {
            public IMyPathVertex<V> Vertex;
            public int nextVertex;
        }

        private List<PathNode> m_vertices;

        public int Count { get { return m_vertices.Count; } }

        internal MyPath(int size)
        {
            m_vertices = new List<PathNode>(size);
        }

        public PathNode this[int position]
        {
            get
            {
                return m_vertices[position];
            }
            set
            {
                m_vertices[position] = value;
            }
        }

        internal void Add(IMyPathVertex<V> vertex, IMyPathVertex<V> nextVertex)
        {
            PathNode node = new PathNode();
            node.Vertex = vertex;

            if (nextVertex == null)
            {
                m_vertices.Add(node);
                return;
            }

            int count = vertex.GetNeighborCount();
            for (int i = 0; i < count; ++i)
            {
                var neigh = vertex.GetNeighbor(i);
                if (neigh == nextVertex)
                {
                    node.nextVertex = i;
                    m_vertices.Add(node);
                    return;
                }
            }
            Debug.Assert(false, "Could not get from vertex to its neighbor");
        }

        public IEnumerator<MyPath<V>.PathNode> GetEnumerator()
        {
            return m_vertices.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_vertices.GetEnumerator();
        }
    }
}
