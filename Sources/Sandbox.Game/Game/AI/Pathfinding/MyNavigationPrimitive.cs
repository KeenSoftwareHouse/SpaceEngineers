using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Profiler;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public abstract class MyNavigationPrimitive : IMyPathVertex<MyNavigationPrimitive>
    {
        MyPathfindingData m_pathfindingData;

        public MyPathfindingData PathfindingData
        {
            get { return m_pathfindingData; }
        }

        private bool m_externalNeighbors;
        public bool HasExternalNeighbors { set { m_externalNeighbors = value; } }

        protected MyNavigationPrimitive()
        {
            m_pathfindingData = new MyPathfindingData(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        float IMyPathVertex<MyNavigationPrimitive>.EstimateDistanceTo(IMyPathVertex<MyNavigationPrimitive> other)
        {
            ProfilerShort.Begin("Pathfinding heuristic");
            var otherPrimitive = other as MyNavigationPrimitive;
            float dist;
            if (Group == otherPrimitive.Group)
                dist = Vector3.Distance(this.Position, otherPrimitive.Position);
            else
                dist = (float)Vector3D.Distance(this.WorldPosition, otherPrimitive.WorldPosition);
            ProfilerShort.End();

            return dist;
        }

        int IMyPathVertex<MyNavigationPrimitive>.GetNeighborCount()
        {
            ProfilerShort.Begin("GetNeighborCount");
            int neighbors = GetOwnNeighborCount();
            if (!m_externalNeighbors)
            {
                ProfilerShort.End();
                return neighbors;
            }

            neighbors += Group.GetExternalNeighborCount(this);
            ProfilerShort.End();
            return neighbors;
        }

        IMyPathVertex<MyNavigationPrimitive> IMyPathVertex<MyNavigationPrimitive>.GetNeighbor(int index)
        {
            ProfilerShort.Begin("GetNeighbor");
            int ownNeighbors = GetOwnNeighborCount();
            IMyPathVertex<MyNavigationPrimitive> neighbor = null;
            if (index < ownNeighbors)
            {
                neighbor = GetOwnNeighbor(index);
            }
            else
            {
                neighbor = Group.GetExternalNeighbor(this, index - ownNeighbors);
            }
            ProfilerShort.End();

            return neighbor;
        }

        IMyPathEdge<MyNavigationPrimitive> IMyPathVertex<MyNavigationPrimitive>.GetEdge(int index)
        {
            ProfilerShort.Begin("GetNeighbor");
            int ownNeighbors = GetOwnNeighborCount();
            IMyPathEdge<MyNavigationPrimitive> edge = null;
            if (index < ownNeighbors)
            {
                edge = GetOwnEdge(index);
            }
            else
            {
                edge = Group.GetExternalEdge(this, index - ownNeighbors);
            }
            ProfilerShort.End();

            return edge;
        }

        public abstract Vector3 Position { get; }
        public abstract Vector3D WorldPosition { get; }
        public virtual Vector3 ProjectLocalPoint(Vector3 point) { return Position; }

        public abstract IMyNavigationGroup Group { get; }

        public abstract int GetOwnNeighborCount();
        public abstract IMyPathVertex<MyNavigationPrimitive> GetOwnNeighbor(int index);
        public abstract IMyPathEdge<MyNavigationPrimitive> GetOwnEdge(int index);

        public abstract MyHighLevelPrimitive GetHighLevelPrimitive();

        public IEnumerator<IMyPathEdge<MyNavigationPrimitive>> GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
