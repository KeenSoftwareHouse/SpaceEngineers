using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageMath.Spatial;
using VRageRender.Utils;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyVoxelConnectionHelper
    {
        private struct InnerEdgeIndex: IEquatable<InnerEdgeIndex>
        {
            public ushort V0;
            public ushort V1;

            public InnerEdgeIndex(ushort vert0, ushort vert1)
            {
                V0 = vert0;
                V1 = vert1;
            }

            public override int GetHashCode()
            {
                return (int)V0 + ((int)V1) << 16;
            }

            public override bool Equals(object obj)
            {
                Debug.Assert(false, "Equals on struct does allocation!");
                if (!(obj is InnerEdgeIndex)) return false;
                return this.Equals((InnerEdgeIndex)obj);
            }

            public override string ToString()
            {
                return "{" + V0 + ", " + V1 + "}";
            }

            public bool Equals(InnerEdgeIndex other)
            {
                return other.V0 == V0 && other.V1 == V1;
            }
        }

        public struct OuterEdgePoint
        {
            public int EdgeIndex;
            public bool FirstPoint; // Whether this point is the first one in the given edge

            public OuterEdgePoint(int edgeIndex, bool firstPoint)
            {
                EdgeIndex = edgeIndex;
                FirstPoint = firstPoint;
            }

            public override string ToString()
            {
                return "{" + EdgeIndex + (FirstPoint ? " O--->" : " <---O") + "}";
            }
        }

        private Dictionary<InnerEdgeIndex, int> m_innerEdges = new Dictionary<InnerEdgeIndex, int>();
        private MyVector3Grid<OuterEdgePoint> m_outerEdgePoints = new MyVector3Grid<OuterEdgePoint>(1.0f);

        // This helps with registering multiple edges under one. It maps edge indices to other edge indices that share the same InnerEdgeIndex
        private Dictionary<int, int> m_innerMultiedges = new Dictionary<int, int>();

        // All edges in the cell should be present here, if they're preprocessed.
        // If their value is 0, they're inner; value > 0 means outer; value < 0 should not be present for edges from the cell's triangles
        private Dictionary<InnerEdgeIndex, int> m_edgeClassifier = new Dictionary<InnerEdgeIndex, int>();

        private List<OuterEdgePoint> m_tmpOuterEdgePointList = new List<OuterEdgePoint>();

        public static float OUTER_EDGE_EPSILON = 0.05f /*float.Epsilon*/;
        public static float OUTER_EDGE_EPSILON_SQ = OUTER_EDGE_EPSILON * OUTER_EDGE_EPSILON;

        public void ClearCell()
        {
            m_innerEdges.Clear();
            m_innerMultiedges.Clear();
            m_edgeClassifier.Clear();
        }

        public void PreprocessInnerEdge(ushort a, ushort b)
        {
            InnerEdgeIndex thisEdge = new InnerEdgeIndex(a, b);
            InnerEdgeIndex otherEdge = new InnerEdgeIndex(b, a);
            int value;

            if (!m_edgeClassifier.TryGetValue(thisEdge, out value))
                value = 1;
            else
                value += 1;
            m_edgeClassifier[thisEdge] = value;

            if (!m_edgeClassifier.TryGetValue(otherEdge, out value))
                value = -1;
            else
                value -= 1;
            m_edgeClassifier[otherEdge] = value;
        }

        public bool IsInnerEdge(ushort v0, ushort v1)
        {
            return IsInnerEdge(new InnerEdgeIndex(v0, v1));
        }

        private bool IsInnerEdge(InnerEdgeIndex edgeIndex)
        {
            // CH: This seems to be OK indeed. It can be < 0 for non-manifold outer edges
            // Debug.Assert(m_edgeClassifier[edgeIndex] >= 0, "Edge classification in voxel connection helper was < 0. This is probably OK.");
            return m_edgeClassifier[edgeIndex] == 0;
        }

        public int TryGetAndRemoveEdgeIndex(ushort iv0, ushort iv1, ref Vector3 posv0, ref Vector3 posv1)
        {
            ProfilerShort.Begin("TryGetAndRemoveEdgeIndex");
            int retval = -1;
            InnerEdgeIndex innerIndex = new InnerEdgeIndex(iv0, iv1);
            if (IsInnerEdge(new InnerEdgeIndex(iv1, iv0)))
            {
                if (!m_innerEdges.TryGetValue(innerIndex, out retval))
                    retval = -1;
                else
                    RemoveInnerEdge(retval, innerIndex);
            }
            else
            {
                TryRemoveOuterEdge(ref posv0, ref posv1, ref retval);
            }

            ProfilerShort.End();
            return retval;
        }

        public void AddEdgeIndex(ushort iv0, ushort iv1, ref Vector3 posv0, ref Vector3 posv1, int edgeIndex)
        {
            InnerEdgeIndex innerIndex = new InnerEdgeIndex(iv0, iv1);
            if (IsInnerEdge(innerIndex))
            {
                int oldValue;
                if (m_innerEdges.TryGetValue(innerIndex, out oldValue))
                {
                    m_innerMultiedges.Add(edgeIndex, oldValue);
                    m_innerEdges[innerIndex] = edgeIndex;
                }
                else
                {
                    m_innerEdges.Add(innerIndex, edgeIndex);
                }
            }
            else
                AddOuterEdgeIndex(ref posv0, ref posv1, edgeIndex);
        }

        public void AddOuterEdgeIndex(ref Vector3 posv0, ref Vector3 posv1, int edgeIndex)
        {
            m_outerEdgePoints.AddPoint(ref posv0, new OuterEdgePoint(edgeIndex, firstPoint: true));
            m_outerEdgePoints.AddPoint(ref posv1, new OuterEdgePoint(edgeIndex, firstPoint: false));
        }

        public void FixOuterEdge(int edgeIndex, bool firstPoint, Vector3 currentPosition)
        {
            OuterEdgePoint data = new OuterEdgePoint(edgeIndex, firstPoint);
            var query = m_outerEdgePoints.QueryPointsSphere(ref currentPosition, OUTER_EDGE_EPSILON * 3);
            bool moved = false;
            while (query.MoveNext())
            {
                if (query.Current.EdgeIndex == edgeIndex && query.Current.FirstPoint == firstPoint)
                {
                    //Debug.Assert(moved == false, "The point was already moved!");
                    m_outerEdgePoints.MovePoint(query.StorageIndex, ref currentPosition);
                    moved = true;
                }
            }
        }

        private InnerEdgeIndex RemoveInnerEdge(int formerEdgeIndex, InnerEdgeIndex innerIndex)
        {
            ProfilerShort.Begin("RemoveInnerEdge");

            int nextEdgeIndex;
            if (m_innerMultiedges.TryGetValue(formerEdgeIndex, out nextEdgeIndex))
            {
                m_innerMultiedges.Remove(formerEdgeIndex);
                m_innerEdges[innerIndex] = nextEdgeIndex;
            }
            else
            {
                m_innerEdges.Remove(innerIndex);
            }

            ProfilerShort.End();
            return innerIndex;
        }

        // If edgeIndex == -1, this method finds the first matching edge. Otherwise, it finds the edge with the given index
        public bool TryRemoveOuterEdge(ref Vector3 posv0, ref Vector3 posv1, ref int edgeIndex)
        {
            // Careful: This is quadratic in the number of entries in a bin in m_outerEdgePoints, so don't make the bins too large!
            if (edgeIndex == -1)
            {
                var en0 = m_outerEdgePoints.QueryPointsSphere(ref posv0, OUTER_EDGE_EPSILON);
                while (en0.MoveNext())
                {
                    var en1 = m_outerEdgePoints.QueryPointsSphere(ref posv1, OUTER_EDGE_EPSILON);
                    while (en1.MoveNext())
                    {
                        OuterEdgePoint p0 = en0.Current;
                        OuterEdgePoint p1 = en1.Current;
                        if (p0.EdgeIndex == p1.EdgeIndex && p0.FirstPoint && !p1.FirstPoint)
                        {
                            edgeIndex = p0.EdgeIndex;
                            m_outerEdgePoints.RemoveTwo(ref en0, ref en1);
                            return true;
                        }
                    }
                }

                edgeIndex = -1;
            }
            else
            {
                int found = 0;
                var en0 = m_outerEdgePoints.QueryPointsSphere(ref posv0, OUTER_EDGE_EPSILON);
                while (en0.MoveNext())
                {
                    if (en0.Current.EdgeIndex == edgeIndex && en0.Current.FirstPoint)
                    {
                        found++;
                        break;
                    }
                }

                var en1 = m_outerEdgePoints.QueryPointsSphere(ref posv1, OUTER_EDGE_EPSILON);
                while (en1.MoveNext())
                {
                    if (en1.Current.EdgeIndex == edgeIndex && !en1.Current.FirstPoint)
                    {
                        found++;
                        break;
                    }
                }

                if (found == 2)
                {
                    m_outerEdgePoints.RemoveTwo(ref en0, ref en1);
                    return true;
                }
                else
                {
                    edgeIndex = -1;
                }
            }

            return false;
        }

        public void DebugDraw(ref Matrix drawMatrix, MyWingedEdgeMesh mesh)
        {
            var binEnum = m_outerEdgePoints.EnumerateBins();
            int i = 0;
            while (binEnum.MoveNext())
            {
                int binIndex = Sandbox.Game.Gui.MyCestmirDebugInputComponent.BinIndex;
                if (binIndex == m_outerEdgePoints.InvalidIndex || i == binIndex)
                {
                    Vector3I position = binEnum.Current.Key;
                    int storageIndex = binEnum.Current.Value;

                    BoundingBoxD bb, bbTform;
                    m_outerEdgePoints.GetLocalBinBB(ref position, out bb);
                    bbTform.Min = Vector3D.Transform(bb.Min, drawMatrix);
                    bbTform.Max = Vector3D.Transform(bb.Max, drawMatrix);

                    while (storageIndex != m_outerEdgePoints.InvalidIndex)
                    {
                        Vector3 p = m_outerEdgePoints.GetPoint(storageIndex);
                        var edge = mesh.GetEdge(m_outerEdgePoints.GetData(storageIndex).EdgeIndex);
                        Vector3 v1 = mesh.GetVertexPosition(edge.Vertex1);
                        Vector3 v2 = mesh.GetVertexPosition(edge.Vertex2);
                        Vector3 vc = (v1 + v2) * 0.5f;

                        Vector3D vcTformed = Vector3D.Transform((Vector3D)vc, drawMatrix);
                        Vector3D pTformed = Vector3D.Transform((Vector3D)p, drawMatrix);

                        VRageRender.MyRenderProxy.DebugDrawArrow3D(vcTformed, pTformed, Color.Yellow, Color.Yellow, false);

                        storageIndex = m_outerEdgePoints.GetNextBinIndex(storageIndex);
                    }

                    VRageRender.MyRenderProxy.DebugDrawAABB(bbTform, Color.PowderBlue, 1.0f, 1.0f, false);
                }

                i++;
            }
        }

        [Conditional("DEBUG")]
        public void CollectOuterEdges(List<MyTuple<OuterEdgePoint, Vector3>> output)
        {
            var binEnum = m_outerEdgePoints.EnumerateBins();
            while (binEnum.MoveNext())
            {
                int storageIndex = binEnum.Current.Value;
                while (storageIndex != -1)
                {
                    output.Add(new MyTuple<OuterEdgePoint, Vector3>(m_outerEdgePoints.GetData(storageIndex), m_outerEdgePoints.GetPoint(storageIndex)));
                    storageIndex = m_outerEdgePoints.GetNextBinIndex(storageIndex);
                }
            }
        }
    }
}
