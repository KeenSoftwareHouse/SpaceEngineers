using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Algorithms;
using VRageMath;

namespace Sandbox.Game.GameSystems.Conveyors
{
    public class MyAttachableConveyorEndpoint : MyMultilineConveyorEndpoint
    {
        class MyAttachableLine : IMyPathEdge<IMyConveyorEndpoint>
        {
            private MyAttachableConveyorEndpoint m_endpoint1;
            private MyAttachableConveyorEndpoint m_endpoint2;

            public MyAttachableLine(MyAttachableConveyorEndpoint endpoint1, MyAttachableConveyorEndpoint endpoint2)
            {
                m_endpoint1 = endpoint1;
                m_endpoint2 = endpoint2;
            }

            public float GetWeight()
            {
                return 2.0f;
            }

            public IMyConveyorEndpoint GetOtherVertex(IMyConveyorEndpoint vertex1)
            {
                if (vertex1 == m_endpoint1) return m_endpoint2;
                Debug.Assert(vertex1 == m_endpoint2);

                return m_endpoint1;
            }

            public bool Contains(MyAttachableConveyorEndpoint endpoint)
            {
                return (endpoint == m_endpoint1 || endpoint == m_endpoint2);
            }
        }

        List<MyAttachableLine> m_lines;

        public MyAttachableConveyorEndpoint(MyCubeBlock block)
            : base(block)
        {
            m_lines = new List<MyAttachableLine>();
        }

        public void Attach(MyAttachableConveyorEndpoint other)
        {
            var line = new MyAttachableLine(this, other);

            AddAttachableLine(line);
            other.AddAttachableLine(line);
        }

        public void Detach(MyAttachableConveyorEndpoint other)
        {
            for (int i = 0; i < m_lines.Count; ++i)
            {
                var line = m_lines[i];
                if (line.Contains(other))
                {
                    RemoveAttachableLine(line);
                    other.RemoveAttachableLine(line);
                    return;
                }
            }
        }

        public void DetachAll()
        {
            for (int i = 0; i < m_lines.Count; ++i)
            {
                var line = m_lines[i];
                var other = line.GetOtherVertex(this) as MyAttachableConveyorEndpoint;
                other.RemoveAttachableLine(line);
            }

            m_lines.Clear();
        }

        private void AddAttachableLine(MyAttachableLine line)
        {
            Debug.Assert(line.Contains(this), "Adding a line to the attachable conveyor endpoint that does not contain it!");
            Debug.Assert(!AlreadyAttachedTo(line.GetOtherVertex(this) as MyAttachableConveyorEndpoint), "An attachable conveyor line is already attached to the given counterpart!");

            m_lines.Add(line);
        }

        private void RemoveAttachableLine(MyAttachableLine line)
        {
            Debug.Assert(m_lines.Contains(line), "Attachable line was not attached in an attachable conveyor endpoint");

            m_lines.Remove(line);
        }

        public bool AlreadyAttachedTo(MyAttachableConveyorEndpoint other)
        {
            foreach (var line in m_lines)
            {
                if (line.GetOtherVertex(this) == other) return true;
            }

            return false;
        }

        public bool AlreadyAttached()
        {
            return m_lines.Count != 0;
        }

        protected override int GetNeighborCount()
        {
            return base.GetNeighborCount() + m_lines.Count;
        }

        protected override IMyPathVertex<IMyConveyorEndpoint> GetNeighbor(int index)
        {
            int baseNeighborCount = base.GetNeighborCount();
            if (index < baseNeighborCount)
                return base.GetNeighbor(index);
            return m_lines[index - baseNeighborCount].GetOtherVertex(this);
        }

        protected override IMyPathEdge<IMyConveyorEndpoint> GetEdge(int index)
        {
            int baseNeighborCount = base.GetNeighborCount();
            if (index < baseNeighborCount)
                return base.GetEdge(index);
            return m_lines[index - baseNeighborCount];
        }
    }
}
