using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRageMath;

namespace VRageRender.Utils
{
    public partial class MyPolygonBoolOps
    {
        private static MyPolygonBoolOps m_static = null;
        public static MyPolygonBoolOps Static
        {
            get
            {
                if (m_static == null)
                    m_static = new MyPolygonBoolOps();
                return m_static;
            }
        }

        private struct BoundPair
        {
            public MyPolygon Parent;
            public int Left;
            public int Minimum;
            public int Right;
            // Whether the previous edge of the Right vertex is horizontal
            public bool RightIsPrecededByHorizontal;

            private float m_minimumCoordinate;
            public float MinimumCoordinate { get { return m_minimumCoordinate; } }

            public BoundPair(MyPolygon parent, int left, int minimum, int right, bool rightHorizontal)
            {
                Debug.Assert(parent != null);

                Parent = parent;
                Left = left;
                Minimum = minimum;
                Right = right;
                RightIsPrecededByHorizontal = rightHorizontal;

                m_minimumCoordinate = 0;
            }

            public bool IsValid()
            {
                return Parent != null && Left != -1 && Right != -1 && Minimum != -1 && Left != Minimum && Right != Minimum && !float.IsNaN(MinimumCoordinate);
            }

            public void CalculateMinimumCoordinate()
            {
                Debug.Assert(Minimum != -1);
                MyPolygon.Vertex vertex;
                Parent.GetVertex(Minimum, out vertex);
                m_minimumCoordinate = vertex.Coord.Y;
            }
        }

        private struct SortedEdgeEntry
        {
            public int Index;
            public float XCoord;
            public float DX;
            public PolygonType Kind;
            public float QNumerator;
        }

        private struct IntersectionListEntry
        {
            public int LIndex; // Index of the edge that is left before the intersection ("before" means "when Y is lower than intersection Y")
            public int RIndex; // Index of the edge that is right before the intersection ("before" means "when Y is lower than intersection Y")
            public float X;
            public float Y;
        }

        private enum PolygonType : byte
        {
            SUBJECT = 0,
            CLIP = 1,
        }

        private enum Side : byte
        {
            LEFT = 0,
            RIGHT = 2,
        }

        private static Side OtherSide(Side side)
        {
            if (side == Side.LEFT)
            {
                return Side.RIGHT;
            }
            Debug.Assert(side == Side.RIGHT);
            return Side.LEFT;
        }

        private class PartialPolygon
        {
            private List<Vector3> m_vertices = new List<Vector3>();

            public void Clear()
            {
                m_vertices.Clear();
            }

            public void Append(Vector3 newVertex)
            {
                int vNum = m_vertices.Count;
                if (vNum == 0 || Vector3.DistanceSquared(newVertex, m_vertices[vNum - 1]) > 0.000001f)
                {
                    m_vertices.Add(newVertex);
                }
            }

            public void Prepend(Vector3 newVertex)
            {
                int vNum = m_vertices.Count;
                if (vNum == 0 || Vector3.DistanceSquared(newVertex, m_vertices[0]) > 0.000001f)
                {
                    m_vertices.Insert(0, newVertex);
                }
            }

            public void Reverse()
            {
                m_vertices.Reverse();
            }

            public void Add(PartialPolygon other)
            {
                if (other.m_vertices.Count == 0) return;
                // Append the first vertex with the check and the rest without
                Append(other.m_vertices[0]);
                for (int i = 1; i < other.m_vertices.Count; ++i)
                {
                    m_vertices.Add(other.m_vertices[i]);
                }
            }

            public List<Vector3> GetLoop()
            {
                return m_vertices;
            }

            public void Postprocess()
            {
                if (m_vertices.Count >= 3)
                {
                    if (Vector3.DistanceSquared(m_vertices[m_vertices.Count - 1], m_vertices[0]) <= 0.000001f)
                    {
                        m_vertices.RemoveAt(m_vertices.Count - 1);
                    }
                }

                if (m_vertices.Count < 3)
                {
                    m_vertices.Clear();
                }
            }
        }

        private struct Edge
        {
            public int BoundPairIndex;
            public Side BoundPairSide;
            public int TopVertexIndex;
            public float BottomX;
            public float TopY;
            public float DXdy;
            public PolygonType Kind;
            public Side OutputSide;
            public bool Contributing;
            public PartialPolygon AssociatedPolygon;

            public float CalculateX(float dy)
            {
                return BottomX + DXdy * dy;
            }

            public float CalculateQNumerator(float bottom, float top, float topX)
            {
                Debug.Assert(Math.Abs(topX - (BottomX + DXdy * (top - bottom))) < 0.001f);
                return BottomX * top - topX * bottom;
            }
        }

        private MyPolygon m_polyA = new MyPolygon(new Plane(Vector3.Forward, 0));
        private MyPolygon m_polyB = new MyPolygon(new Plane(Vector3.Forward, 0));
        private Operation m_operation = null;

        private List<BoundPair> m_boundsA = new List<BoundPair>();
        private List<BoundPair> m_boundsB = new List<BoundPair>();
        private List<BoundPair> m_usedBoundPairs = new List<BoundPair>();

        // All these lists are sorted in a descending order
        private List<float> m_scanBeamList           = new List<float>();
        private List<float> m_horizontalScanBeamList = new List<float>();
        private List<BoundPair> m_localMinimaList    = new List<BoundPair>();

        private List<Edge> m_activeEdgeList       = new List<Edge>();

        private List<Vector3> m_tmpList                        = new List<Vector3>();
        private List<SortedEdgeEntry> m_sortedEdgeList         = new List<SortedEdgeEntry>();
        private List<IntersectionListEntry> m_intersectionList = new List<IntersectionListEntry>();

        // During processing of intersections, at which position is the given active edge
        private List<int> m_edgePositionInfo = new List<int>();

        private List<PartialPolygon> m_results = new List<PartialPolygon>();

        private Matrix m_projectionTransform;
        private Matrix m_invProjectionTransform;
        private Plane m_projectionPlane;

        static MyPolygonBoolOps()
        {
            InitializeOperations();
        }

        public MyPolygon Intersection(MyPolygon polyA, MyPolygon polyB)
        {
            return PerformBooleanOperation(polyA, polyB, m_operationIntersection);
        }

        public MyPolygon Union(MyPolygon polyA, MyPolygon polyB)
        {
            return PerformBooleanOperation(polyA, polyB, m_operationUnion);
        }

        public MyPolygon Difference(MyPolygon polyA, MyPolygon polyB)
        {
            return PerformBooleanOperation(polyA, polyB, m_operationDifference);
        }

        private MyPolygon PerformBooleanOperation(MyPolygon polyA, MyPolygon polyB, Operation operation)
        {
            Debug.Assert(polyA.PolygonPlane.Equals(polyB.PolygonPlane));
            Clear();
            PrepareTransforms(polyA);
            ProjectPoly(polyA, m_polyA, ref m_projectionTransform);
            ProjectPoly(polyB, m_polyB, ref m_projectionTransform);
            m_operation = operation;
            PerformInPlane();
            m_operation = null;
            return UnprojectResult();
        }

        private void Clear()
        {
            m_polyA.Clear();
            m_polyB.Clear();
            m_boundsA.Clear();
            m_boundsB.Clear();
            m_usedBoundPairs.Clear();
            m_scanBeamList.Clear();
            m_horizontalScanBeamList.Clear();
            m_localMinimaList.Clear();
            m_activeEdgeList.Clear();
            m_results.Clear();
        }

        private void PrepareTransforms(MyPolygon polyA)
        {
            m_projectionPlane        = polyA.PolygonPlane;
            Vector3 origin           = -polyA.PolygonPlane.Normal * polyA.PolygonPlane.D;
            Vector3 forward          = polyA.PolygonPlane.Normal;
            Vector3 right            = Vector3.CalculatePerpendicularVector(forward);
            Vector3 up               = Vector3.Cross(right, forward);
            m_invProjectionTransform = Matrix.CreateWorld(origin, forward, up);
            Matrix.Invert(ref m_invProjectionTransform, out m_projectionTransform);
        }

        private void ProjectPoly(MyPolygon input, MyPolygon output, ref Matrix projection)
        {
            for (int i = 0; i < input.LoopCount; ++i)
            {
                m_tmpList.Clear();

                var iterator = input.GetLoopIterator(i);
                while (iterator.MoveNext())
                {
                    Vector3 transformed = Vector3.Transform(iterator.Current, projection);
                    m_tmpList.Add(transformed);
                }

                output.AddLoop(m_tmpList);
            }
        }

        private void PerformInPlane()
        {
            ConstructBoundPairs(m_polyA, m_boundsA);
            ConstructBoundPairs(m_polyB, m_boundsB);
            
            AddBoundPairsToLists(m_boundsA);
            AddBoundPairsToLists(m_boundsB);

            float bottomY = m_scanBeamList[m_scanBeamList.Count - 1];
            m_scanBeamList.RemoveAt(m_scanBeamList.Count - 1);
            do
            {
                // Add edges starting at bottomY to the Active edge list
                AddBoundPairsToActiveEdges(bottomY);
                float topY = m_scanBeamList[m_scanBeamList.Count - 1];
                m_scanBeamList.RemoveAt(m_scanBeamList.Count - 1);

                //if (bottomY == 2.0f) Debugger.Break();

                if (bottomY == topY)
                {
                    ProcessHorizontalLine(bottomY);
                    if (m_scanBeamList.Count == 0)
                    {
                        // No more scan beams to process. Finish
                        break;
                    }
                    topY = m_scanBeamList[m_scanBeamList.Count - 1];
                    Debug.Assert(topY != bottomY);
                    m_scanBeamList.RemoveAt(m_scanBeamList.Count - 1);
                }

                ProcessIntersections(bottomY, topY);
                UpdateActiveEdges(bottomY, topY);

                bottomY = topY;
            } while (m_scanBeamList.Count > 0);

            m_scanBeamList.Clear();
        }

        private void ProcessHorizontalLine(float bottomY)
        {
            float closestHorizontalEnd = float.PositiveInfinity;
            PolygonType closestHorizontalType = PolygonType.SUBJECT;
            int horizontalEdgeStart = 0;
            for (int i = 0; i < m_activeEdgeList.Count; ++i)
            {
                int currentEdge = i;
                var edge = m_activeEdgeList[currentEdge];
                Debug.Assert(edge.TopY != bottomY || edge.DXdy >= 0, "Horizontal edge should have its left vertex processed first!");

                // If the edge is further than where the closest horizontal edge ends, first process that horizontal edge
                while (closestHorizontalEnd < edge.BottomX || (closestHorizontalEnd == edge.BottomX && closestHorizontalType == PolygonType.CLIP && edge.Kind == PolygonType.SUBJECT))
                {
                    ProcessOldHorizontalEdges(bottomY, ref closestHorizontalEnd, ref closestHorizontalType, ref horizontalEdgeStart, i);
                }

                // Edge is horizontal or a "leftover" edge (i.e. a right edge that should join with a horizontal edge)
                if (edge.TopY == bottomY)
                {
                    // If the edge is horizontal (i.e. is going right), we either already intersected it's sibling (if the sibling is going up),
                    // or there should have been no intersection (if it was going from the bottom)

                    // A leftover edge
                    if (edge.DXdy == 0.0f)
                    {
                        bool edgeClosed = false;
                        for (int j = i - 1; j >= horizontalEdgeStart; --j)
                        {
                            var horizontalEdge = m_activeEdgeList[j];
                            if (horizontalEdge.Kind == edge.Kind && horizontalEdge.TopVertexIndex == edge.TopVertexIndex) // The edge should end this horizontal edge
                            {
                                if (horizontalEdge.Contributing)
                                {
                                    Debug.Assert(edge.Contributing);
                                    Debug.Assert(Math.Abs(horizontalEdge.BottomX + horizontalEdge.DXdy - edge.BottomX) < 0.001f);
                                    AddLocalMaximum(j, currentEdge, ref horizontalEdge, ref edge, new Vector3(edge.BottomX, bottomY, 0.0f));
                                }
                                else
                                {
                                    Debug.Assert(!edge.Contributing);
                                }
                                m_activeEdgeList.RemoveAt(currentEdge);
                                m_activeEdgeList.RemoveAt(j);
                                i -= 2;
                                edgeClosed = true;
                                break;
                            }
                            else
                            {
                                Vector3 intersection = new Vector3(edge.BottomX, bottomY, 0.0f);
                                var intersectionClassification = m_operation.ClassifyIntersection(horizontalEdge.OutputSide, horizontalEdge.Kind, edge.OutputSide, edge.Kind);
                                PerformIntersection(j, currentEdge, ref horizontalEdge, ref edge, ref intersection, intersectionClassification);
                                currentEdge = j;
                            }
                        }

                        Debug.Assert(edgeClosed);
                    }
                    else // Horizontal edge
                    {
                        float edgeEndX = edge.BottomX + edge.DXdy;
                        if (edgeEndX < closestHorizontalEnd || (edgeEndX == closestHorizontalEnd && edge.Kind == PolygonType.CLIP && closestHorizontalType == PolygonType.SUBJECT))
                        {
                            closestHorizontalEnd = edgeEndX;
                            closestHorizontalType = edge.Kind;
                        }
                    }
                }
                else
                {
                    for (int j = i - 1; j >= horizontalEdgeStart; --j)
                    {
                        var horizontalEdge = m_activeEdgeList[j];
                        Vector3 intersection = new Vector3(edge.BottomX, bottomY, 0.0f);
                        var intersectionClassification = m_operation.ClassifyIntersection(horizontalEdge.OutputSide, horizontalEdge.Kind, edge.OutputSide, edge.Kind);
                        PerformIntersection(j, currentEdge, ref horizontalEdge, ref edge, ref intersection, intersectionClassification);
                        currentEdge = j;
                    }
                    ++horizontalEdgeStart;
                }
            }

            // All non-horizontal edges were processed, do only the horizontal ones now
            while (horizontalEdgeStart < m_activeEdgeList.Count)
            {
                ProcessOldHorizontalEdges(bottomY, ref closestHorizontalEnd, ref closestHorizontalType, ref horizontalEdgeStart, m_activeEdgeList.Count);
            }
        }

        private void ProcessOldHorizontalEdges(float bottomY, ref float endX, ref PolygonType endType, ref int from, int to)
        {
            float currentEnd = endX;
            PolygonType currentType = endType;
            endX = float.PositiveInfinity;
            endType = PolygonType.SUBJECT;
            for (int j = from; j < to; ++j)
            {
                var horizontalEdge = m_activeEdgeList[j];
                Debug.Assert(horizontalEdge.TopY == bottomY && horizontalEdge.DXdy != 0.0f, "The edge was not a horizontal edge!?");

                float edgeEndX = horizontalEdge.BottomX + horizontalEdge.DXdy;
                if (edgeEndX == currentEnd && horizontalEdge.Kind == currentType)
                {
                    BoundPair pair = m_usedBoundPairs[horizontalEdge.BoundPairIndex];
                    MyPolygon.Vertex endVertex, newTopVertex;
                    pair.Parent.GetVertex(horizontalEdge.TopVertexIndex, out endVertex);
                    Vector3 newPosition = endVertex.Coord;

                    if (horizontalEdge.Contributing)
                    {
                        if (horizontalEdge.OutputSide == Side.LEFT)
                        {
                            horizontalEdge.AssociatedPolygon.Append(newPosition);
                        }
                        else
                        {
                            horizontalEdge.AssociatedPolygon.Prepend(newPosition);
                        }
                    }

                    if (horizontalEdge.BoundPairSide == Side.LEFT)
                    {
                        pair.Parent.GetVertex(endVertex.Next, out newTopVertex);
                    }
                    else
                    {
                        pair.Parent.GetVertex(endVertex.Prev, out newTopVertex);
                    }

                    RecalculateActiveEdge(ref horizontalEdge, ref endVertex, ref newTopVertex, horizontalEdge.BoundPairSide);
                    m_activeEdgeList[j] = horizontalEdge;

                    if (horizontalEdge.TopY == bottomY && horizontalEdge.DXdy != 0.0f)
                    {
                        float newEndX = horizontalEdge.BottomX + horizontalEdge.DXdy;
                        if (newEndX < endX || (newEndX == endX && horizontalEdge.Kind == PolygonType.CLIP && endType == PolygonType.SUBJECT))
                        {
                            endX = newEndX;
                            endType = horizontalEdge.Kind;
                        }
                    }
                    else
                    {
                        // The "horizontalEdge" is no longer horizontal :-) We intersect it with horizontal edges above
                        int risingEdge = j;
                        for (int k = j - 1; k >= from; --k)
                        {
                            var horizontalEdge2 = m_activeEdgeList[k];
                            Vector3 intersection = new Vector3(horizontalEdge.BottomX, bottomY, 0.0f);
                            var intersectionClassification = m_operation.ClassifyIntersection(horizontalEdge2.OutputSide, horizontalEdge2.Kind, horizontalEdge.OutputSide, horizontalEdge.Kind);
                            PerformIntersection(k, risingEdge, ref horizontalEdge2, ref horizontalEdge, ref intersection, intersectionClassification);
                            risingEdge = k;
                        }

                        ++from;
                    }
                }
                else
                {
                    float newEndX = horizontalEdge.BottomX + horizontalEdge.DXdy;
                    if (newEndX < endX || (newEndX == endX && horizontalEdge.Kind == PolygonType.CLIP && endType == PolygonType.SUBJECT))
                    {
                        endX = newEndX;
                        endType = horizontalEdge.Kind;
                    }
                }
            }
        }

        private void UpdateActiveEdges(float bottomY, float topY)
        {
            if (m_activeEdgeList.Count == 0) return;

            float dy = topY - bottomY;
            MyPolygon.Vertex topVertex, newTopVertex;
            Vector3 newVertexPosition;

            int i = 0;
            while (i < m_activeEdgeList.Count)
            {
                Edge e = m_activeEdgeList[i];
                if (e.TopY == topY)
                {
                    BoundPair pair = m_usedBoundPairs[e.BoundPairIndex];
                    bool localMaximum = false;

                    pair.Parent.GetVertex(e.TopVertexIndex, out topVertex);
                    newVertexPosition = topVertex.Coord;

                    if (e.TopVertexIndex == (e.BoundPairSide == Side.LEFT ? pair.Left : pair.Right))
                    {
                        localMaximum = true;
                    }
                    else
                    {
                        if (e.Contributing)
                        {
                            if (e.OutputSide == Side.LEFT)
                            {
                                e.AssociatedPolygon.Append(newVertexPosition);
                            }
                            else
                            {
                                e.AssociatedPolygon.Prepend(newVertexPosition);
                            }
                        }

                        if (e.BoundPairSide == Side.LEFT)
                        {
                            pair.Parent.GetVertex(topVertex.Next, out newTopVertex);
                        }
                        else
                        {
                            pair.Parent.GetVertex(topVertex.Prev, out newTopVertex);
                        }

                        RecalculateActiveEdge(ref e, ref topVertex, ref newTopVertex, e.BoundPairSide);
                    }

                    if (localMaximum)
                    {
                        if (e.BoundPairSide == Side.RIGHT && m_usedBoundPairs[e.BoundPairIndex].RightIsPrecededByHorizontal)
                        {
                            e.BottomX = e.CalculateX(dy);
                            e.TopY = topY;
                            e.DXdy = 0.0f;
                            m_activeEdgeList[i] = e;
                        }
                        else
                        {
                            // Search for the matching edge
                            int matchingIndex = -1;
                            Edge matchingEdge = default(Edge);
                            {
                                for (int j = i + 1; j < m_activeEdgeList.Count; ++j)
                                {
                                    matchingEdge = m_activeEdgeList[j];
                                    if (matchingEdge.Kind == e.Kind && matchingEdge.TopVertexIndex == e.TopVertexIndex)
                                    {
                                        matchingIndex = j;
                                        break;
                                    }
                                }
                            }
                            Debug.Assert(matchingIndex != -1, "Couldn't find matching closing edge!");

                            // The contribution flag could be inconsistent due to inaccuracies. But we don't care. We simply throw away such polygons.
                            if (e.Contributing)
                            {
                                if (matchingEdge.Contributing)
                                {
                                    AddLocalMaximum(i, matchingIndex, ref e, ref matchingEdge, newVertexPosition);
                                }
                            }

                            m_activeEdgeList.RemoveAt(matchingIndex);
                            m_activeEdgeList.RemoveAt(i);
                            continue;
                        }
                    }
                    else
                    {
                        m_activeEdgeList[i] = e;
                    }
                }
                else
                {
                    e.BottomX = e.CalculateX(dy);
                    m_activeEdgeList[i] = e;
                }

                ++i;
            }
        }

        private void ProcessIntersections(float bottom, float top)
        {
            //if (bottom == 8.0f) Debugger.Break();
            if (m_activeEdgeList.Count == 0)
            {
                return;
            }

            BuildIntersectionList(bottom, top);
            ProcessIntersectionList();
        }

        private void BuildIntersectionList(float bottom, float top)
        {
            Debug.Assert(m_intersectionList.Count == 0);

            float dy = top - bottom;
            float invDy = 1.0f / dy;

            SortedEdgeEntry entry = new SortedEdgeEntry();
            GetSortedEdgeEntry(bottom, top, dy, 0, ref entry);
            m_sortedEdgeList.Add(entry);

            for (int i = 1; i < m_activeEdgeList.Count; ++i)
            {
                GetSortedEdgeEntry(bottom, top, dy, i, ref entry);

                int j = m_sortedEdgeList.Count - 1;
                while (j >= 0)
                {
                    SortedEdgeEntry otherEntry = m_sortedEdgeList[j];

                    if (CompareSortedEdgeEntries(ref otherEntry, ref entry) == -1) break;

                    float x, y;

                    /*if (entry.DX - otherEntry.DX == 0.0f)
                    {
                        x = entry.XCoord + entry.DX * 0.5f;
                        y = (bottom + top) * 0.5f;
                    }
                    else
                    {*/
                        float invDxDiff = 1.0f / (entry.DX - otherEntry.DX);
                        y = (otherEntry.QNumerator - entry.QNumerator) * invDxDiff;
                        x = entry.DX * invDy * y + entry.QNumerator * invDy;
                        Debug.Assert(Math.Abs(otherEntry.DX * invDy * y + otherEntry.QNumerator * invDy - x) < 0.001f);
                    //}

                    IntersectionListEntry intersection = new IntersectionListEntry();
                    intersection.RIndex = entry.Index;
                    intersection.LIndex = otherEntry.Index;
                    intersection.X = x;
                    intersection.Y = y;

                    InsertIntersection(ref intersection);

                    --j;
                }

                m_sortedEdgeList.Insert(j + 1, entry);
            }

            m_sortedEdgeList.Clear();
        }

        private SortedEdgeEntry GetSortedEdgeEntry(float bottom, float top, float dy, int i, ref SortedEdgeEntry entry)
        {
            var edge = m_activeEdgeList[i];
            entry.Index = i;
            if (edge.TopY == top)
            {
                MyPolygon.Vertex vert;
                if (edge.Kind == PolygonType.SUBJECT)
                {
                    m_polyA.GetVertex(edge.TopVertexIndex, out vert);
                }
                else
                {
                    m_polyB.GetVertex(edge.TopVertexIndex, out vert);
                }
                entry.XCoord = vert.Coord.X;
            }
            else
            {
                entry.XCoord = edge.CalculateX(dy);
            }
            entry.DX = entry.XCoord - edge.BottomX;
            entry.Kind = edge.Kind;
            entry.QNumerator = edge.CalculateQNumerator(bottom, top, entry.XCoord);
            Debug.Assert(edge.TopY != bottom);
            return entry;
        }

        private void InsertIntersection(ref IntersectionListEntry intersection)
        {
            for (int i = 0; i < m_intersectionList.Count; ++i)
            {
                if (m_intersectionList[i].Y > intersection.Y)
                {
                    m_intersectionList.Insert(i, intersection);
                    return;
                }
            }
            m_intersectionList.Add(intersection);
        }

        private void ProcessIntersectionList()
        {
            InitializeEdgePositions();
            for (int i = 0; i < m_intersectionList.Count; ++i)
            {
                IntersectionListEntry intersection = m_intersectionList[i];
                int leftEdgeIndex = m_edgePositionInfo[intersection.LIndex];
                int rightEdgeIndex = m_edgePositionInfo[intersection.RIndex];
                Edge e1 = m_activeEdgeList[leftEdgeIndex];
                Edge e2 = m_activeEdgeList[rightEdgeIndex];
                Vector3 intersectionPosition = new Vector3(intersection.X, intersection.Y, 0.0f);

                var intersectionClassification = m_operation.ClassifyIntersection(e1.OutputSide, e1.Kind, e2.OutputSide, e2.Kind);
                PerformIntersection(leftEdgeIndex, rightEdgeIndex, ref e1, ref e2, ref intersectionPosition, intersectionClassification);

                SwapEdgePositions(intersection.LIndex, intersection.RIndex);
            }
            m_intersectionList.Clear();
            m_edgePositionInfo.Clear();
        }

        private void PerformIntersection(int leftEdgeIndex, int rightEdgeIndex, ref Edge e1, ref Edge e2, ref Vector3 intersectionPosition, IntersectionClassification intersectionClassification)
        {
            Debug.Assert(intersectionClassification != IntersectionClassification.INVALID);
            switch (intersectionClassification)
            {
                case IntersectionClassification.LIKE_INTERSECTION:
                    Debug.Assert((e1.Contributing && e2.Contributing) || (!e1.Contributing && !e2.Contributing));
                    Debug.Assert((e1.OutputSide == Side.LEFT && e2.OutputSide == Side.RIGHT) || (e1.OutputSide == Side.RIGHT && e2.OutputSide == Side.LEFT));

                    Side swapSide = e1.OutputSide;
                    e1.OutputSide = e2.OutputSide;
                    e2.OutputSide = swapSide;

                    if (e1.Contributing)
                    {
                        if (e1.OutputSide == Side.RIGHT)
                        {
                            e1.AssociatedPolygon.Append(intersectionPosition);
                            e2.AssociatedPolygon.Prepend(intersectionPosition);
                        }
                        else
                        {
                            e1.AssociatedPolygon.Prepend(intersectionPosition);
                            e2.AssociatedPolygon.Append(intersectionPosition);
                        }
                    }
                    break;
                case IntersectionClassification.LEFT_E1_INTERMEDIATE:
                    e1.AssociatedPolygon.Append(intersectionPosition);
                    break;
                case IntersectionClassification.RIGHT_E1_INTERMEDIATE:
                    e1.AssociatedPolygon.Prepend(intersectionPosition);
                    break;
                case IntersectionClassification.LEFT_E2_INTERMEDIATE:
                    e2.AssociatedPolygon.Append(intersectionPosition);
                    break;
                case IntersectionClassification.RIGHT_E2_INTERMEDIATE:
                    e2.AssociatedPolygon.Prepend(intersectionPosition);
                    break;
                case IntersectionClassification.LOCAL_MINIMUM:
                    var newPoly = new PartialPolygon();
                    newPoly.Append(intersectionPosition);
                    e1.AssociatedPolygon = newPoly;
                    e2.AssociatedPolygon = newPoly;
                    break;
                case IntersectionClassification.LOCAL_MAXIMUM:
                    AddLocalMaximum(leftEdgeIndex, rightEdgeIndex, ref e1, ref e2, intersectionPosition);
                    break;
            }

            PartialPolygon swap = e1.AssociatedPolygon;
            e1.AssociatedPolygon = e2.AssociatedPolygon;
            e2.AssociatedPolygon = swap;

            if (intersectionClassification != IntersectionClassification.LIKE_INTERSECTION)
            {
                e1.Contributing = !e1.Contributing;
                e2.Contributing = !e2.Contributing;
            }

            m_activeEdgeList[leftEdgeIndex] = e2;
            m_activeEdgeList[rightEdgeIndex] = e1;
        }

        private void AddLocalMaximum(int leftEdgeIndex, int rightEdgeIndex, ref Edge e1, ref Edge e2, Vector3 maximumPosition)
        {
            if (e1.OutputSide == Side.LEFT)
            {
                e1.AssociatedPolygon.Append(maximumPosition);
            }
            else
            {
                e1.AssociatedPolygon.Prepend(maximumPosition);
            }

            if (e1.AssociatedPolygon == e2.AssociatedPolygon)
            {
                m_results.Add(e1.AssociatedPolygon);
                // TODO: Set also contributing flags?
            }
            else
            {
                int otherEdgeIndex;
                if (e1.OutputSide == Side.LEFT)
                {
                    Debug.Assert(e2.OutputSide == Side.RIGHT);

                    Edge e2other = FindOtherPolygonEdge(e2.AssociatedPolygon, rightEdgeIndex, out otherEdgeIndex);
                    //e2.AssociatedPolygon.Reverse();
                    e1.AssociatedPolygon.Add(e2.AssociatedPolygon);
                    e2.AssociatedPolygon = e1.AssociatedPolygon;

                    e2other.AssociatedPolygon = e2.AssociatedPolygon;
                    m_activeEdgeList[otherEdgeIndex] = e2other;
                }
                else
                {
                    Debug.Assert(e2.OutputSide == Side.LEFT);

                    /*if (e2.OutputSide == Side.LEFT)
                    {*/
                        Edge e1other = FindOtherPolygonEdge(e1.AssociatedPolygon, leftEdgeIndex, out otherEdgeIndex);
                        e2.AssociatedPolygon.Add(e1.AssociatedPolygon);
                        e1.AssociatedPolygon = e2.AssociatedPolygon;

                        e1other.AssociatedPolygon = e2.AssociatedPolygon;
                        m_activeEdgeList[otherEdgeIndex] = e1other;
                    /*}
                    else
                    {
                        Edge e2other = FindOtherPolygonEdge(e2.AssociatedPolygon, rightEdgeIndex, out otherEdgeIndex);
                        e1.AssociatedPolygon.Reverse();
                        e1.AssociatedPolygon.Add(e2.AssociatedPolygon);
                        e2.AssociatedPolygon = e1.AssociatedPolygon;

                        e2other.AssociatedPolygon = e1.AssociatedPolygon;
                        m_activeEdgeList[otherEdgeIndex] = e2other;
                    }*/
                }
            }
            e1.AssociatedPolygon = null;
            e2.AssociatedPolygon = null;
        }

        private Edge FindOtherPolygonEdge(PartialPolygon polygon, int thisEdgeIndex, out int otherEdgeIndex)
        {
            Debug.Assert(m_activeEdgeList.Where(edge => edge.AssociatedPolygon == polygon).Count() == 2);
            for (int i = 0; i < m_activeEdgeList.Count; ++i)
            {
                if (i == thisEdgeIndex) continue;

                if (m_activeEdgeList[i].AssociatedPolygon == polygon)
                {
                    otherEdgeIndex = i;
                    return m_activeEdgeList[i];
                }
            }

            Debug.Assert(false);
            otherEdgeIndex = -1;
            return default(Edge);
        }

        private void InitializeEdgePositions()
        {
            m_edgePositionInfo.Capacity = Math.Max(m_edgePositionInfo.Capacity, m_intersectionList.Count);
            for (int i = 0; i < m_edgePositionInfo.Count; ++i)
            {
                m_edgePositionInfo[i] = i;
            }
            for (int i = m_edgePositionInfo.Count; i < m_activeEdgeList.Count; ++i)
            {
                m_edgePositionInfo.Add(i);
            }
        }

        private void SwapEdgePositions(int leftEdge, int rightEdge)
        {
            int leftEdgePosition = m_edgePositionInfo[leftEdge];
            int rightEdgePosition = m_edgePositionInfo[rightEdge];
            Debug.Assert(Math.Abs(leftEdgePosition - rightEdgePosition) == 1);
            m_edgePositionInfo[leftEdge] = rightEdgePosition;
            m_edgePositionInfo[rightEdge] = leftEdgePosition;
        }

        private void AddBoundPairsToLists(List<BoundPair> boundsList)
        {
            foreach (var pair in boundsList)
            {
                pair.CalculateMinimumCoordinate();

                // Insert bound pair to the sorted local minima list
                // CH: TODO: Divide and conquer, maybe use linked list
                int i = m_localMinimaList.Count - 1;
                while (i >= 0)
                {
                    if (m_localMinimaList[i].MinimumCoordinate >= pair.MinimumCoordinate)
                    {
                        m_localMinimaList.Insert(i + 1, pair);
                        break;
                    }

                    --i;
                }
                if (i == -1) // We got to the end of the loop => All minima are smaller that this one
                {
                    m_localMinimaList.Insert(0, pair);
                }

                InsertScanBeamDivide(pair.MinimumCoordinate);

                MyPolygon.Vertex vertex, minVertex;
                pair.Parent.GetVertex(pair.Minimum, out minVertex);
                pair.Parent.GetVertex(minVertex.Next, out vertex);
                Debug.Assert(vertex.Coord.Y > minVertex.Coord.Y);
                InsertScanBeamDivide(vertex.Coord.Y);

                pair.Parent.GetVertex(minVertex.Prev, out vertex);
                while (vertex.Coord.Y == minVertex.Coord.Y)
                {
                    pair.Parent.GetVertex(vertex.Prev, out vertex);
                }
                InsertScanBeamDivide(vertex.Coord.Y);
            }
        }

        private void AddBoundPairsToActiveEdges(float bottomY)
        {
            int i = m_localMinimaList.Count - 1;
            while (i >= 0)
            {
                if (m_localMinimaList[i].MinimumCoordinate != bottomY)
                    break;

                int boundPairIndex = m_usedBoundPairs.Count;
                m_usedBoundPairs.Add(m_localMinimaList[i]);

                var parentPoly = m_localMinimaList[i].Parent;
                MyPolygon.Vertex minimumVertex, prevVertex, nextVertex;
                parentPoly.GetVertex(m_localMinimaList[i].Minimum, out minimumVertex);
                parentPoly.GetVertex(minimumVertex.Prev, out prevVertex);
                parentPoly.GetVertex(minimumVertex.Next, out nextVertex);
                Debug.Assert(minimumVertex.Coord.Y == bottomY);

                PolygonType polyType = parentPoly == m_polyA ? PolygonType.SUBJECT : PolygonType.CLIP;

                Edge leftEdge = PrepareActiveEdge(boundPairIndex, ref minimumVertex, ref nextVertex, polyType, Side.LEFT);
                Edge rightEdge = PrepareActiveEdge(boundPairIndex, ref minimumVertex, ref prevVertex, polyType, Side.RIGHT);

                int insertPosition = SortInMinimum(ref leftEdge, ref rightEdge, polyType);
                if (leftEdge.Contributing)
                {
                    Debug.Assert(rightEdge.Contributing, "Contributing status of minimum edges was not consistent!");
                    var newPoly = new PartialPolygon();
                    newPoly.Append(minimumVertex.Coord);
                    leftEdge.AssociatedPolygon = newPoly;
                    rightEdge.AssociatedPolygon = newPoly;
                }
                else
                {
                    Debug.Assert(!rightEdge.Contributing, "Contributing status of minimum edges was not consistent!");
                }

                if (leftEdge.DXdy > rightEdge.DXdy)
                {
                    /*leftEdge.OutputSide = Side.RIGHT;
                    rightEdge.OutputSide = Side.LEFT;*/
                    m_activeEdgeList.Insert(insertPosition, rightEdge);
                    m_activeEdgeList.Insert(insertPosition + 1, leftEdge);
                }
                else
                {
                    Debug.Assert(leftEdge.DXdy < rightEdge.DXdy, "Overlapping edges in an input polygon!");
                    m_activeEdgeList.Insert(insertPosition, leftEdge);
                    m_activeEdgeList.Insert(insertPosition + 1, rightEdge);
                }

                --i;
            }

            int removeCount = m_localMinimaList.Count - 1 - i;
            m_localMinimaList.RemoveRange(i + 1, removeCount);
        }

        private Edge PrepareActiveEdge(int boundPairIndex, ref MyPolygon.Vertex lowerVertex, ref MyPolygon.Vertex upperVertex, PolygonType polyType, Side side)
        {
            Edge newEdge = new Edge();
            newEdge.BoundPairIndex = boundPairIndex;
            newEdge.BoundPairSide = side;
            newEdge.Kind = polyType;
            if (polyType == PolygonType.CLIP)
            {
                newEdge.OutputSide = m_operation.ClipInvert ? OtherSide(side) : side;
            }
            else
            {
                newEdge.OutputSide = m_operation.SubjectInvert ? OtherSide(side) : side;
            }

            RecalculateActiveEdge(ref newEdge, ref lowerVertex, ref upperVertex, side);

            return newEdge;
        }

        private void RecalculateActiveEdge(ref Edge edge, ref MyPolygon.Vertex lowerVertex, ref MyPolygon.Vertex upperVertex, Side boundPairSide)
        {
            float Dy = upperVertex.Coord.Y - lowerVertex.Coord.Y;
            float Dx = upperVertex.Coord.X - lowerVertex.Coord.X;
            Debug.Assert(Dx != 0.0f || Dy != 0.0f, "Invalid polygon! Right point and minimum of a bound were the same point!");

            edge.TopVertexIndex = boundPairSide == Side.LEFT ? lowerVertex.Next : lowerVertex.Prev;
            edge.BottomX = lowerVertex.Coord.X;
            edge.TopY = upperVertex.Coord.Y;
            edge.DXdy = Dy == 0 ? Dx : Dx / Dy;

            InsertScanBeamDivide(upperVertex.Coord.Y);
        }

        private int SortInMinimum(ref Edge leftEdge, ref Edge rightEdge, PolygonType type)
        {
            bool parity = false;
            int i = 0;
            while (i < m_activeEdgeList.Count)
            {
                var edge = m_activeEdgeList[i];

                if (CompareEdges(ref leftEdge, ref edge) == -1) break;

                if (edge.Kind != type)
                {
                    parity = !parity;
                }

                ++i;
            }

            bool contributing = m_operation.InitializeContributing(parity, type);
            leftEdge.Contributing = contributing;
            rightEdge.Contributing = contributing;

            return i;
        }

        private void InsertScanBeamDivide(float value)
        {
            // Note: Scan beam list is sorted descending so that we can pop values from it
            // CH: TODO: Divide and conquer, maybe use linked list
            for (int i = 0; i < m_scanBeamList.Count; ++i)
            {
                if (m_scanBeamList[i] > value)
                    continue;
                if (m_scanBeamList[i] == value)
                    return;
                m_scanBeamList.Insert(i, value);
                return;
            }
            m_scanBeamList.Add(value);
        }

        private static int CompareEdges(ref Edge edge1, ref Edge edge2)
        {
            // Sorting rules:
            // 1.) sort by X coordinate
            // 2.) sort by type of input polygon
            // 3.) sort by DXdy value
            if (edge1.BottomX < edge2.BottomX)
            {
                return -1;
            }
            else if (edge1.BottomX == edge2.BottomX)
            {
                if (edge1.Kind == edge2.Kind)
                {
                    Debug.Assert(edge1.DXdy >= edge2.DXdy, "Overtaking a sorted edge entry that should be processed after us. This shouldn't happen!");
                    Debug.Assert(edge1.DXdy > edge2.DXdy, "Overlapping edge in an input polygon!");
                    return 1;
                }
                else if (edge1.Kind == PolygonType.CLIP)
                {
                    return -1;
                }
            }

            return 1;
        }

        private static int CompareSortedEdgeEntries(ref SortedEdgeEntry entry1, ref SortedEdgeEntry entry2)
        {
            // Sorting rules:
            // 1.) sort by X coordinate
            // 2.) sort by type of input polygon
            // 3.) sort by DXdy value (or in this case, only DX)
            if (entry1.XCoord < entry2.XCoord)
            {
                return -1;
            }
            else if (entry1.XCoord == entry2.XCoord)
            {
                if (entry1.Kind == entry2.Kind)
                {
                    Debug.Assert(entry1.DX >= entry2.DX, "Overtaking a sorted edge entry that should be processed after us. This shouldn't happen!");
                    Debug.Assert(entry1.DX > entry2.DX, "Overlapping edge in an input polygon!");
                    return 1;
                }
                else if (entry1.Kind == PolygonType.CLIP)
                {
                    return -1;
                }
            }

            return 1;
        }

        private static int CompareCoords(Vector3 coord1, Vector3 coord2)
        {
            if (coord1.Y > coord2.Y)
            {
                return 1;
            }
            else if (coord1.Y < coord2.Y)
            {
                return -1;
            }
            else
            {
                if (coord1.X > coord2.X)
                {
                    return 1;
                }
                else if (coord1.X < coord2.X)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private static void ConstructBoundPairs(MyPolygon poly, List<BoundPair> boundList)
        {
            for (int l = 0; l < poly.LoopCount; ++l)
            {
                int start, current, prev;
                MyPolygon.Vertex vertex;

                start = FindLoopLocalMaximum(poly, l);

                poly.GetVertex(start, out vertex);
                current = start;

                MyPolygon.Vertex otherVertex;
                poly.GetVertex(vertex.Prev, out otherVertex);

                BoundPair bounds = new BoundPair(poly, -1, -1, start, otherVertex.Coord.Y == vertex.Coord.Y);
                bool right = true;

                int comparison, prevComparison;
                comparison = -1;

                do
                {
                    Vector3 prevCoord = vertex.Coord;
                    prev = current;
                    current = vertex.Next;

                    poly.GetVertex(current, out vertex);
                    prevComparison = comparison;
                    comparison = CompareCoords(vertex.Coord, prevCoord);
                    Debug.Assert(comparison != 0, "Duplicate vertex in input polygon!");

                    if (right)
                    {
                        if (comparison > 0)
                        {
                            bounds.Minimum = prev;
                            right = false;
                        }
                    }
                    else
                    {
                        if (comparison < 0)
                        {
                            bounds.Left = prev;
                            Debug.Assert(bounds.IsValid());
                            boundList.Add(bounds);
                            bounds = new BoundPair(poly, -1, -1, prev, prevComparison == 0);
                            right = true;
                        }
                    }
                } while (current != start);

                bounds.Left = current;
                Debug.Assert(right == false);
                Debug.Assert(bounds.IsValid());
                boundList.Add(bounds);
            }
        }

        private static int FindLoopLocalMaximum(MyPolygon poly, int loop)
        {
            int index, maxIndex;
            Vector3 localMax;
            MyPolygon.Vertex vertex, otherVertex;

            index = poly.GetLoopStart(loop);
            poly.GetVertex(index, out vertex);

            maxIndex = index;
            localMax = vertex.Coord;

            // Find local maximum while going to the previous vertices in the loop
            index = vertex.Prev;
            poly.GetVertex(index, out otherVertex);
            while (otherVertex.Coord.Y > localMax.Y || (otherVertex.Coord.Y == localMax.Y && otherVertex.Coord.X > localMax.X))
            {
                maxIndex = index;
                localMax = otherVertex.Coord;
                index = otherVertex.Prev;
                poly.GetVertex(index, out otherVertex);
            }

            // Find local maximum while going to the next vertices in the loop
            index = vertex.Next;
            poly.GetVertex(index, out otherVertex);
            while (otherVertex.Coord.Y > localMax.Y || (otherVertex.Coord.Y == localMax.Y && otherVertex.Coord.X > localMax.X))
            {
                maxIndex = index;
                localMax = otherVertex.Coord;
                index = otherVertex.Next;
                poly.GetVertex(index, out otherVertex);
            }

            return maxIndex;
        }

        private MyPolygon UnprojectResult()
        {
            MyPolygon tmp = new MyPolygon(new Plane(Vector3.Forward, 0));
            MyPolygon result = new MyPolygon(m_projectionPlane);
            foreach (var poly in m_results)
            {
                poly.Postprocess();
                var loop = poly.GetLoop();
                if (loop.Count == 0) continue;
                tmp.AddLoop(poly.GetLoop());
            }
            ProjectPoly(tmp, result, ref m_invProjectionTransform);
            return result;
        }

        public void DebugDraw(MatrixD drawMatrix)
        {
            drawMatrix = drawMatrix * m_invProjectionTransform * Matrix.CreateTranslation(m_invProjectionTransform.Left * 12.0f);

            DebugDrawBoundList(drawMatrix, m_polyA, m_boundsA);
            DebugDrawBoundList(drawMatrix, m_polyB, m_boundsB);
        }

        private static MatrixD DebugDrawBoundList(MatrixD drawMatrix, MyPolygon drawPoly, List<BoundPair> boundList)
        {
            foreach (var bound in boundList)
            {
                MyPolygon.Vertex v1, v2;
                Vector3 vec1 = default(Vector3);
                Vector3 vec2 = default(Vector3);

                int prev = bound.Left;
                drawPoly.GetVertex(prev, out v1);
                int current = v1.Prev;
                while (prev != bound.Minimum)
                {
                    drawPoly.GetVertex(current, out v2);

                    vec1 = Vector3.Transform(v1.Coord, drawMatrix);
                    vec2 = Vector3.Transform(v2.Coord, drawMatrix);

                    MyRenderProxy.DebugDrawLine3D(vec1, vec2, Color.Red, Color.Red, false);

                    prev = current;
                    v1 = v2;
                    current = v1.Prev;
                }

                MatrixD minimum = drawMatrix;
                minimum.Translation = vec2;
                MyRenderProxy.DebugDrawAxis(minimum, 0.25f, false);
                MyRenderProxy.DebugDrawSphere(vec2, 0.03f, Color.Yellow, 1.0f, false);

                prev = bound.Minimum;
                drawPoly.GetVertex(prev, out v1);
                current = v1.Prev;
                while (prev != bound.Right)
                {
                    drawPoly.GetVertex(current, out v2);

                    vec1 = Vector3.Transform(v1.Coord, drawMatrix);
                    vec2 = Vector3.Transform(v2.Coord, drawMatrix);

                    MyRenderProxy.DebugDrawLine3D(vec1, vec2, Color.Green, Color.Green, false);

                    prev = current;
                    v1 = v2;
                    current = v1.Prev;
                }

                if (bound.RightIsPrecededByHorizontal)
                {
                    MyRenderProxy.DebugDrawSphere(vec2, 0.03f, Color.Red, 1.0f, false);
                }
            }
            return drawMatrix;
        }
    }
}
