using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRage.Algorithms
{
    public class MyPathFindingSystem<V> : IEnumerable<V>
        where V : class, IMyPathVertex<V>
    {
        public class Enumerator : IEnumerator<V>
        {
            private V m_currentVertex;
            private MyPathFindingSystem<V> m_parent;
            Predicate<V> m_vertexFilter = null;
            Predicate<V> m_vertexTraversable = null;
            Predicate<IMyPathEdge<V>> m_edgeTraversable = null;

            public void Init(
                MyPathFindingSystem<V> parent,
                V startingVertex,
                Predicate<V> vertexFilter = null,
                Predicate<V> vertexTraversable = null,
                Predicate<IMyPathEdge<V>> edgeTraversable = null)
            {
                Debug.Assert(parent.m_enumerating == false, "The pathfinding system is still enumerating. Maybe you forgot to Dispose() af an enumerator?");
                Debug.Assert(parent.m_bfsQueue.Count() == 0, "BFS queue was not empty in MyPathFindingSystem");

                m_parent = parent;
                m_vertexFilter = vertexFilter;
                m_vertexTraversable = vertexTraversable;
                m_edgeTraversable = edgeTraversable;
                m_parent.CalculateNextTimestamp();
                m_parent.m_enumerating = true;
                m_parent.m_bfsQueue.Enqueue(startingVertex);

                startingVertex.PathfindingData.Timestamp = m_parent.m_timestamp;
            }

            public V Current
            {
                get { return m_currentVertex; }
            }

            public void Dispose()
            {
                m_parent.m_enumerating = false;
                m_parent.m_bfsQueue.Clear();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return m_currentVertex; }
            }

            public bool MoveNext()
            {
                while (m_parent.m_bfsQueue.Count() != 0)
                {
                    m_currentVertex = m_parent.m_bfsQueue.Dequeue();

                    V otherVertex = null;

                    for (int i = 0; i < m_currentVertex.GetNeighborCount(); ++i)
                    {
                        if (m_edgeTraversable == null)
                        {
                            otherVertex = (V)m_currentVertex.GetNeighbor(i);
                            if (otherVertex == null)
                                continue;
                        }
                        else
                        {
                            IMyPathEdge<V> otherEdge = m_currentVertex.GetEdge(i);
                            if (!m_edgeTraversable(otherEdge))
                                continue;
                            otherVertex = otherEdge.GetOtherVertex(m_currentVertex);
                            if (otherVertex == null)
                                continue;
                        }

                        if (otherVertex.PathfindingData.Timestamp != m_parent.m_timestamp && (m_vertexTraversable == null || m_vertexTraversable(otherVertex)))
                        {
                            m_parent.m_bfsQueue.Enqueue(otherVertex);
                            otherVertex.PathfindingData.Timestamp = m_parent.m_timestamp;
                        }
                    }
                    
                    if (m_vertexFilter == null || m_vertexFilter(m_currentVertex))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        private long m_timestamp;
        private Func<long> m_timestampFunction;

        private Queue<V> m_bfsQueue;

        private List<V> m_reachableList;
        private MyBinaryHeap<float, MyPathfindingData> m_openVertices;

        private Enumerator m_enumerator;
        private bool m_enumerating;

        public MyPathFindingSystem(int queueInitSize = 128, Func<long> timestampFunction = null)
        {
            m_bfsQueue = new Queue<V>(queueInitSize);
            m_reachableList = new List<V>(128);
            m_openVertices = new MyBinaryHeap<float, MyPathfindingData>(128);
            m_timestamp = 0;
            m_timestampFunction = timestampFunction;
            m_enumerating = false;
            m_enumerator = new Enumerator();
        }

        protected void CalculateNextTimestamp()
        {
            if (m_timestampFunction != null)
            {
                m_timestamp = m_timestampFunction();
            }
            else
            {
                m_timestamp++;
            }
        }

        public MyPath<V> FindPath(V start, V end, Predicate<V> vertexTraversable = null, Predicate<IMyPathEdge<V>> edgeTraversable = null)
        {
            // CH: TODO: Make multiple private copies of this method and call the right one
            // according to what were the arguments to the public interface method

            CalculateNextTimestamp();

            MyPathfindingData startData = start.PathfindingData;
            Visit(startData);
            startData.Predecessor = null;
            startData.PathLength = 0.0f;

            IMyPathVertex<V> retVal = null;
            float shortestPathLength = float.PositiveInfinity;

            m_openVertices.Insert(start.PathfindingData, start.EstimateDistanceTo(end));
            while (m_openVertices.Count > 0)
            {
                MyPathfindingData currentData = m_openVertices.RemoveMin();
                V current = currentData.Parent as V;
                float currentPathLength = currentData.PathLength;

                if (retVal != null && currentPathLength >= shortestPathLength)
                {
                    break;
                }

                for (int i = 0; i < current.GetNeighborCount(); ++i)
                {
                    /*IMyPathVertex<V> neighbor = current.GetNeighbor(i);
                    if (neighbor == null) continue;*/

                    IMyPathEdge<V> edge = current.GetEdge(i);
                    if (edge == null || (edgeTraversable != null && !edgeTraversable(edge))) continue;

                    V neighbor = edge.GetOtherVertex(current);
                    if (neighbor == null || (vertexTraversable != null && !vertexTraversable(neighbor))) continue;

                    float newPathLength = currentData.PathLength + edge.GetWeight();
                    MyPathfindingData neighborData = neighbor.PathfindingData;

                    if (neighbor == end && newPathLength < shortestPathLength)
                    {
                        retVal = neighbor;
                        shortestPathLength = newPathLength;
                    }

                    if (Visited(neighborData))
                    {
                        if (newPathLength < neighborData.PathLength)
                        {
                            neighborData.PathLength = newPathLength;
                            neighborData.Predecessor = currentData;
                            m_openVertices.ModifyUp(neighborData, newPathLength + neighbor.EstimateDistanceTo(end));
                        }
                    }
                    else
                    {
                        Visit(neighborData);
                        neighborData.PathLength = newPathLength;
                        neighborData.Predecessor = currentData;
                        m_openVertices.Insert(neighborData, newPathLength + neighbor.EstimateDistanceTo(end));
                    }
                }
            }

            m_openVertices.Clear();

            if (retVal == null)
            {
                return null;
            }
            else
            {
                return ReturnPath(retVal.PathfindingData, null, 0);
            }
        }

        // Note: Termination criterion tells the pathfinding system, how much we want to terminate the pathfinding in the given vertex.
        //
        // Values can range from 0.0 to positive infinity. Infinity means that the vertex will never be returned as a result. Zero, on the
        // other hand, is equivalent to classical pathfinding, which means that the pathfinding will terminate when a path has been found
        // to a vertex with a criterion value of zero and that path's length could not be improved by looking further.
        //
        // Non-zero values on vertices tell the pathfinding that these vertices are acceptable as final vertices, but only if no better
        // vertex is found (i.e. one that would have lower length + heuristic value).
        //
        // The termination criterion is not used as weighting for the pathfinding priority queue though, which means that you can separate
        // termination in a vertex from the searching heuristic
        public MyPath<V> FindPath(V start, Func<V, float> heuristic, Func<V, float> terminationCriterion, Predicate<V> vertexTraversable = null, bool returnClosest = true)
        {
            Debug.Assert(heuristic != null);
            Debug.Assert(terminationCriterion != null);

            CalculateNextTimestamp();

            MyPathfindingData startData = start.PathfindingData;
            Visit(startData);
            startData.Predecessor = null;
            startData.PathLength = 0.0f;

            IMyPathVertex<V> retVal = null;
            float lowestAcceptableWeight = float.PositiveInfinity;

            IMyPathVertex<V> closest = null;
            float closestWeight = float.PositiveInfinity;

            float terminationValue = terminationCriterion(start);

            if (terminationValue != float.PositiveInfinity)
            {
                retVal = start;
                lowestAcceptableWeight = heuristic(start) + terminationValue;
            }

            m_openVertices.Insert(start.PathfindingData, heuristic(start));
            while (m_openVertices.Count > 0)
            {
                MyPathfindingData currentData = m_openVertices.RemoveMin();
                V current = currentData.Parent as V;
                float currentPathLength = currentData.PathLength;

                if (retVal != null && currentPathLength + heuristic(current) >= lowestAcceptableWeight)
                {
                    break;
                }

                for (int i = 0; i < current.GetNeighborCount(); ++i)
                {
                    IMyPathEdge<V> edge = current.GetEdge(i);
                    if (edge == null) continue;

                    V neighbor = edge.GetOtherVertex(current);
                    if (neighbor == null || (vertexTraversable != null && !vertexTraversable(neighbor))) continue;

                    float newPathLength = currentData.PathLength + edge.GetWeight();
                    MyPathfindingData neighborData = neighbor.PathfindingData;
                    float neighborWeight = newPathLength + heuristic(neighbor);

                    if (neighborWeight < closestWeight)
                    {
                        closest = neighbor;
                        closestWeight = neighborWeight;
                    }

                    terminationValue = terminationCriterion(neighbor);
                    if (neighborWeight + terminationValue < lowestAcceptableWeight)
                    {
                        retVal = neighbor;
                        lowestAcceptableWeight = neighborWeight + terminationValue;
                    }

                    if (Visited(neighborData))
                    {
                        if (newPathLength < neighborData.PathLength)
                        {
                            neighborData.PathLength = newPathLength;
                            neighborData.Predecessor = currentData;
                            m_openVertices.ModifyUp(neighborData, neighborWeight);
                        }
                    }
                    else
                    {
                        Visit(neighborData);
                        neighborData.PathLength = newPathLength;
                        neighborData.Predecessor = currentData;
                        m_openVertices.Insert(neighborData, neighborWeight);
                    }
                }
            }

            m_openVertices.Clear();

            if (retVal == null)
            {
                if (returnClosest == false || closest == null)
                {
                    return null;
                }
                else
                {
                    return ReturnPath(closest.PathfindingData, null, 0);
                }
            }
            else
            {
                return ReturnPath(retVal.PathfindingData, null, 0);
            }
        }

        private MyPath<V> ReturnPath(MyPathfindingData vertexData, MyPathfindingData successor, int remainingVertices)
        {
            if (vertexData.Predecessor == null)
            {
                MyPath<V> retval = new MyPath<V>(remainingVertices + 1);
                retval.Add(vertexData.Parent as V, successor != null ? (successor.Parent as V) : null);
                return retval;
            }
            else
            {
                MyPath<V> retval = ReturnPath(vertexData.Predecessor, vertexData, remainingVertices + 1);
                retval.Add(vertexData.Parent as V, successor != null ? (successor.Parent as V) : null);
                return retval;
            }
        }

        public bool Reachable(V from, V to)
        {
            PrepareTraversal(from);
            foreach (var vertex in this)
            {
                if (vertex.Equals(to))
                    return true;
            }

            return false;
        }

        public void FindReachable(IEnumerable<V> fromSet, List<V> reachableVertices, Predicate<V> vertexFilter = null, Predicate<V> vertexTraversable = null, Predicate<IMyPathEdge<V>> edgeTraversable = null)
        {
            CalculateNextTimestamp();
            foreach (V vertex in fromSet)
            {
                if (!Visited(vertex))
                    FindReachableInternal(vertex, reachableVertices, vertexFilter, vertexTraversable, edgeTraversable);
            }
        }

        public void FindReachable(V from, List<V> reachableVertices, Predicate<V> vertexFilter = null, Predicate<V> vertexTraversable = null, Predicate<IMyPathEdge<V>> edgeTraversable = null)
        {
            FindReachableInternal(from, reachableVertices, vertexFilter, vertexTraversable, edgeTraversable);
        }

        public long GetCurrentTimestamp()
        {
            return m_timestamp;
        }

        public bool VisitedBetween(V vertex, long start, long end)
        {
            return vertex.PathfindingData.Timestamp >= start && vertex.PathfindingData.Timestamp <= end;
        }

        private void FindReachableInternal(V from, List<V> reachableVertices, Predicate<V> vertexFilter = null, Predicate<V> vertexTraversable = null, Predicate<IMyPathEdge<V>> edgeTraversable = null)
        {
            PrepareTraversal(from, vertexFilter, vertexTraversable, edgeTraversable);
            foreach (var vertex in this)
            {
                reachableVertices.Add(vertex);
            }
        }

        private void Visit(V vertex)
        {
            vertex.PathfindingData.Timestamp = m_timestamp;
        }

        private void Visit(MyPathfindingData vertexData)
        {
            vertexData.Timestamp = m_timestamp;
        }

        private bool Visited(V vertex)
        {
            return vertex.PathfindingData.Timestamp == m_timestamp;
        }

        private bool Visited(MyPathfindingData vertexData)
        {
            return vertexData.Timestamp == m_timestamp;
        }

        /// <summary>
        /// Has to be called before any traversal of the pathfinding system using enumerators.
        /// 
        /// Several predicates can be supplied to the system that change the behavior of the traversal.
        /// </summary>
        /// <param name="startingVertex">The vertex from which the traversal starts</param>
        /// <param name="vertexFilter">If set, this predicate is applied to the output vertices so that we only get those that we are interested in.</param>
        /// <param name="vertexTraversable">
        ///     This predicate allows to make vertices of the graph untraversable, blocking the paths through them.
        ///     It is guaranteed to be called only once on every vertex when enumerating the graph or finding reachable vertices, but
        ///     for pathfinding functions, this guarantee is no longer valid.
        /// </param>
        /// <param name="edgeTraversable">This predicate allows to make edges untraversable, blocking the paths through them.</param>
        public void PrepareTraversal(
            V startingVertex,
            Predicate<V> vertexFilter = null,
            Predicate<V> vertexTraversable = null,
            Predicate<IMyPathEdge<V>> edgeTraversable = null)
        {
            m_enumerator.Init(this, startingVertex, vertexFilter, vertexTraversable, edgeTraversable);
        }

        // Traverses the system using the given predicates from function PrepareTraversal
        public void PerformTraversal()
        {
            while (m_enumerator.MoveNext());
            m_enumerator.Dispose();
        }

        private Enumerator GetEnumeratorInternal()
        {
            return m_enumerator;
        }

        public IEnumerator<V> GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal();
        }
    }
}
