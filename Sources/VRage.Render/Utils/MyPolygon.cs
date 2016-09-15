using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;

namespace VRageRender.Utils
{
    public class MyPolygon
    {
        public struct Vertex
        {
            public Vector3 Coord;
            public int Prev;
            public int Next;
        }

        public struct LoopIterator
        {
            private List<Vertex> m_data;
            private int m_begin;
            private int m_currentIndex;
            private Vertex m_current;

            public LoopIterator(MyPolygon poly, int loopBegin)
            {
                m_data = poly.m_vertices;
                m_begin = loopBegin;
                m_currentIndex = -1;
                m_current = new Vertex();
                m_current.Next = m_begin;
            }

            public Vector3 Current
            {
                get
                {
                    return m_current.Coord;
                }
            }

            public int CurrentIndex
            {
                get
                {
                    return m_currentIndex;
                }
            }

            public bool MoveNext()
            {
                if (m_currentIndex != -1 && m_current.Next == m_begin) return false;

                m_currentIndex = m_current.Next;
                m_current = m_data[m_currentIndex];
                return true;
            }
        }

        private List<Vertex> m_vertices;
        public int VertexCount
        {
            get
            {
                return m_vertices.Count;
            }
        }

        private List<int> m_loops;
        public int LoopCount
        {
            get
            {
                return m_loops.Count;
            }
        }

        private Plane m_plane;
        public Plane PolygonPlane
        {
            get
            {
                return m_plane;
            }
        }

        public MyPolygon(Plane polygonPlane)
        {
            m_vertices = new List<Vertex>();
            m_loops = new List<int>();
            m_plane = polygonPlane;
        }

        public void Transform(ref Matrix transformationMatrix)
        {
            for (int i = 0; i < m_vertices.Count; ++i)
            {
                var vertex = m_vertices[i];
                Vector3.Transform(ref vertex.Coord, ref transformationMatrix, out vertex.Coord);
                m_vertices[i] = vertex;
            }
        }

        public LoopIterator GetLoopIterator(int loopIndex)
        {
            return new LoopIterator(this, m_loops[loopIndex]);
        }

        public int GetLoopStart(int loopIndex)
        {
            return m_loops[loopIndex];
        }

        public void GetVertex(int vertexIndex, out Vertex v)
        {
            v = m_vertices[vertexIndex];
        }

        public void GetXExtents(out float minX, out float maxX)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            for (int i = 0; i < m_vertices.Count; ++i)
            {
                float x = m_vertices[i].Coord.X;

                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
            }
        }

        public void AddLoop(List<Vector3> loop)
        {
            Debug.Assert(loop.Count >= 3, "Adding an invalid loop to a polygon!");
            if (loop.Count < 3) return;

            for (int i = 0; i < loop.Count; ++i)
            {
                Debug.Assert(Math.Abs(m_plane.DotCoordinate(loop[i])) < 0.0002f);
            }

            int firstIndex = m_vertices.Count;
            int prevIndex = m_vertices.Count + loop.Count - 1;
            m_loops.Add(firstIndex);

            for (int i = 0; i < loop.Count - 1; ++i)
            {
                m_vertices.Add(new Vertex() { Coord = loop[i], Next = firstIndex + i + 1, Prev = prevIndex });
                prevIndex = firstIndex + i;
            }
            m_vertices.Add(new Vertex() { Coord = loop[loop.Count - 1], Next = firstIndex, Prev = prevIndex });
        }

        public void Clear()
        {
            m_vertices.Clear();
            m_loops.Clear();
        }

        public void DebugDraw(ref MatrixD drawMatrix)
        {
            for (int i = 0; i < m_vertices.Count; ++i)
            {
                MyRenderProxy.DebugDrawLine3D(m_vertices[i].Coord, m_vertices[m_vertices[i].Next].Coord, Color.DarkRed, Color.DarkRed, false);
                MyRenderProxy.DebugDrawPoint(m_vertices[i].Coord, Color.Red, false);
                MyRenderProxy.DebugDrawText3D(m_vertices[i].Coord + Vector3.Right * 0.05f, i.ToString() + "/" + m_vertices.Count.ToString(), Color.Red, 0.45f, false);
            }
        }
    }
}
