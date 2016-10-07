using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Profiler;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyNavigationEdge : IMyPathEdge<MyNavigationPrimitive>
    {
        // A static member for use in graph traversal. We don't need any pool, because only one instance will ever be needed
        public static MyNavigationEdge Static = new MyNavigationEdge();

        private MyNavigationPrimitive m_triA;
        private MyNavigationPrimitive m_triB;

        private int m_index;
        public int Index { get { return m_index; } }

        public void Init(MyNavigationPrimitive triA, MyNavigationPrimitive triB, int index)
        {
            m_triA = triA;
            m_triB = triB;
            m_index = index;
        }

        public float GetWeight()
        {
            ProfilerShort.Begin("MyNavigationEdge.GetWeight");
            float retval = (m_triA.Position - m_triB.Position).Length() * 1.0f;
            ProfilerShort.End();

            return retval;
        }

        public MyNavigationPrimitive GetOtherVertex(MyNavigationPrimitive vertex1)
        {
            if (vertex1 == m_triA) return m_triB;
            Debug.Assert(vertex1 == m_triB);
            return m_triA;
        }
    }
}
