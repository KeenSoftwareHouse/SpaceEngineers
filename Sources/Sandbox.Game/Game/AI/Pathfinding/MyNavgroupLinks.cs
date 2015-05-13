using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Algorithms;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyNavgroupLinks
    {
        private Dictionary<MyNavigationPrimitive, List<MyNavigationPrimitive>> m_links;

        private class PathEdge : IMyPathEdge<MyNavigationPrimitive>
        {
            private static PathEdge Static = new PathEdge(); // We only ever need one instance

            private MyNavigationPrimitive m_primitive1;
            private MyNavigationPrimitive m_primitive2;

            public static PathEdge GetEdge(MyNavigationPrimitive primitive1, MyNavigationPrimitive primitive2)
            {
                Static.m_primitive1 = primitive1;
                Static.m_primitive2 = primitive2;
                return Static;
            }

            public float GetWeight()
            {
                if (m_primitive1.Group == m_primitive2.Group)
                    return Vector3.Distance(m_primitive1.Position, m_primitive2.Position);
                else
                    return (float)Vector3D.Distance(m_primitive1.WorldPosition, m_primitive2.WorldPosition);
            }

            public MyNavigationPrimitive GetOtherVertex(MyNavigationPrimitive vertex1)
            {
                if (vertex1 == m_primitive1) return m_primitive2;
                else
                {
                    Debug.Assert(vertex1 == m_primitive2);
                    return m_primitive1;
                }
            }
        }

        public MyNavgroupLinks()
        {
            m_links = new Dictionary<MyNavigationPrimitive, List<MyNavigationPrimitive>>();
        }

        public void AddLink(MyNavigationPrimitive primitive1, MyNavigationPrimitive primitive2, bool onlyIfNotPresent = false)
        {
            Debug.Assert(primitive1 != null);
            Debug.Assert(primitive2 != null);

            AddLinkInternal(primitive1, primitive2, onlyIfNotPresent);
            AddLinkInternal(primitive2, primitive1, onlyIfNotPresent);

            primitive1.HasExternalNeighbors = true;
            primitive2.HasExternalNeighbors = true;
        }

        public void RemoveLink(MyNavigationPrimitive primitive1, MyNavigationPrimitive primitive2)
        {
            Debug.Assert(primitive1 != null);
            Debug.Assert(primitive2 != null);

            bool empty = RemoveLinkInternal(primitive1, primitive2);
            if (empty) primitive1.HasExternalNeighbors = false;

            empty = RemoveLinkInternal(primitive2, primitive1);
            if (empty) primitive2.HasExternalNeighbors = false;
        }

        public int GetLinkCount(MyNavigationPrimitive primitive)
        {
            List<MyNavigationPrimitive> links = null;
            m_links.TryGetValue(primitive, out links);
            return links == null ? 0 : links.Count;
        }

        public MyNavigationPrimitive GetLinkedNeighbor(MyNavigationPrimitive primitive, int index)
        {
            List<MyNavigationPrimitive> links = null;
            m_links.TryGetValue(primitive, out links);
            Debug.Assert(links != null);
            if (links == null) return null;

            return links[index];
        }

        public IMyPathEdge<MyNavigationPrimitive> GetEdge(MyNavigationPrimitive primitive, int index)
        {
            List<MyNavigationPrimitive> links = null;
            m_links.TryGetValue(primitive, out links);
            Debug.Assert(links != null);
            if (links == null) return null;

            MyNavigationPrimitive otherPrimitive = links[index];
            return PathEdge.GetEdge(primitive, otherPrimitive);
        }

        public List<MyNavigationPrimitive> GetLinks(MyNavigationPrimitive primitive)
        {
            List<MyNavigationPrimitive> links = null;
            m_links.TryGetValue(primitive, out links);
            return links;
        }

        public void RemoveAllLinks(MyNavigationPrimitive primitive)
        {
            List<MyNavigationPrimitive> links = null;
            m_links.TryGetValue(primitive, out links);
            if (links == null) return;

            foreach (var otherPrimitive in links)
            {
                List<MyNavigationPrimitive> links2 = null;
                m_links.TryGetValue(otherPrimitive, out links2);
                Debug.Assert(links2 != null, "Corruption in links. One-way navmesh links are not supported yet!");
                if (links2 == null) return;

                links2.Remove(primitive);
                if (links2.Count == 0)
                    m_links.Remove(otherPrimitive);
            }

            m_links.Remove(primitive);
        }

        public void DebugDraw(VRageMath.Color lineColor)
        {
            if (!MyFakes.DEBUG_DRAW_NAVMESH_LINKS)
            {
                return;
            }

            foreach (var entry in m_links)
            {
                MyNavigationPrimitive fromPrimitive = entry.Key;
                var links = entry.Value;

                for (int i = 0; i < links.Count; ++i)
                {
                    var toPrimitive = links[i];

                    Vector3D from = fromPrimitive.WorldPosition;
                    Vector3D to = toPrimitive.WorldPosition;
                    Vector3D center = (from + to) * 0.5;
                    Vector3D v1 = (center + from) * 0.5;
                    Vector3D perp = Vector3D.Up;

                    VRageRender.MyRenderProxy.DebugDrawLine3D(from, v1 + perp * 0.4, lineColor, lineColor, false);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(v1 + perp * 0.4, center + perp * 0.5, lineColor, lineColor, false);
                }
            }
        }

        private void AddLinkInternal(MyNavigationPrimitive primitive1, MyNavigationPrimitive primitive2, bool onlyIfNotPresent = false)
        {
            List<MyNavigationPrimitive> links = null;
            m_links.TryGetValue(primitive1, out links);
            if (links == null)
            {
                links = new List<MyNavigationPrimitive>();
                m_links.Add(primitive1, links);
            }

            if (onlyIfNotPresent)
            {
                if (!links.Contains(primitive2))
                {
                    links.Add(primitive2);
                }
            }
            else
            {
                Debug.Assert(!links.Contains(primitive2));
                links.Add(primitive2);
            }
        }

        private bool RemoveLinkInternal(MyNavigationPrimitive primitive1, MyNavigationPrimitive primitive2)
        {
            List<MyNavigationPrimitive> links = null;
            m_links.TryGetValue(primitive1, out links);
            if (links != null)
            {
                links.Remove(primitive2);
                if (links.Count == 0)
                {
                    m_links.Remove(primitive1);
                    return true;
                }
            }

            return false;
        }
    }
}
