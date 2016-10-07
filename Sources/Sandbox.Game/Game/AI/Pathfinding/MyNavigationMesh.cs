using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Algorithms;
using VRage.Generics;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Utils;

namespace Sandbox.Game.AI.Pathfinding
{
    public abstract class MyNavigationMesh : MyPathFindingSystem<MyNavigationPrimitive>, IMyNavigationGroup
    {
        private class Funnel
        {
            private enum PointTestResult { LEFT, INSIDE, RIGHT }

            private Vector3 m_end;
            private int m_endIndex;

            private MyPath<MyNavigationPrimitive> m_input;
            private List<Vector4D> m_output;

            private Vector3 m_apex;
            private Vector3 m_apexNormal;

            private Vector3 m_leftPoint;
            private Vector3 m_rightPoint;
            private int m_leftIndex;
            private int m_rightIndex;

            private Vector3 m_leftPlaneNormal;
            private Vector3 m_rightPlaneNormal;
            private float m_leftD;
            private float m_rightD;

            private bool m_funnelConstructed;
            private bool m_segmentDangerous;

            private static float SAFE_DISTANCE = 0.7f;
            private static float SAFE_DISTANCE_SQ = SAFE_DISTANCE*SAFE_DISTANCE;
            private static float SAFE_DISTANCE2_SQ = (SAFE_DISTANCE + SAFE_DISTANCE) * (SAFE_DISTANCE + SAFE_DISTANCE);

            public void Calculate(MyPath<MyNavigationPrimitive> inputPath, List<Vector4D> refinedPath, ref Vector3 start, ref Vector3 end, int startIndex, int endIndex)
            {
                m_debugFunnel.Clear();
                m_debugPointsLeft.Clear();
                m_debugPointsRight.Clear();

                m_end = end;
                m_endIndex = endIndex;
                m_input = inputPath;
                m_output = refinedPath;
                m_apex = start;
                m_funnelConstructed = false;
                m_segmentDangerous = false;

                int i = startIndex;
                while (i < endIndex)
                {
                    i = AddTriangle(i);

                    if (i == endIndex)
                    {
                        PointTestResult result = TestPoint(end);
                        if (result == PointTestResult.LEFT)
                        {
                            m_apex = m_leftPoint;
                            m_funnelConstructed = false;
                            ConstructFunnel(m_leftIndex);
                            i = m_leftIndex + 1;
                        }
                        else if (result == PointTestResult.RIGHT)
                        {
                            m_apex = m_rightPoint;
                            m_funnelConstructed = false;
                            ConstructFunnel(m_rightIndex);
                            i = m_rightIndex + 1;
                        }

                        if (result == PointTestResult.INSIDE || i == endIndex)
                        {
                            AddPoint(ProjectEndOnTriangle(i));
                        }
                    }

                }

                if (startIndex == endIndex)
                {
                    AddPoint(ProjectEndOnTriangle(i));
                }

                m_input = null;
                m_output = null;
            }

            private void AddPoint(Vector3D point)
            {
                float radius = m_segmentDangerous ? 0.5f : 2.0f;
                m_output.Add(new Vector4D(point, radius));

                int previous = m_output.Count - 1;
                if (previous >= 0)
                {
                    var prevPoint = m_output[previous];
                    if (prevPoint.W > radius)
                    {
                        prevPoint.W = radius;
                        m_output[previous] = prevPoint;
                    }
                }

                m_segmentDangerous = false;
            }

            private Vector3 ProjectEndOnTriangle(int i)
            {
                MyPath<MyNavigationPrimitive>.PathNode tri = m_input[i];

                var triangle = tri.Vertex as MyNavigationTriangle;
                Debug.Assert(triangle != null, "Path did not consist of triangles only!");

                return triangle.ProjectLocalPoint(m_end);
            }

            private int AddTriangle(int index)
            {
                if (!m_funnelConstructed)
                {
                    ConstructFunnel(index);
                }
                else
                {                    
                    MyPath<MyNavigationPrimitive>.PathNode tri = m_input[index];

                    Vector3 left, right;
                    var triangle = tri.Vertex as MyNavigationTriangle;
                    Debug.Assert(triangle != null, "Path did not consist of triangles only!");

                    var edge = triangle.GetNavigationEdge(tri.nextVertex);
                    GetEdgeVerticesSafe(triangle, tri.nextVertex, out left, out right);

                    PointTestResult leftResult = TestPoint(left);
                    PointTestResult rightResult = TestPoint(right);

                    if (leftResult == PointTestResult.INSIDE)
                        NarrowFunnel(left, index, left: true);
                    if (rightResult == PointTestResult.INSIDE)
                        NarrowFunnel(right, index, left: false);

                    if (leftResult == PointTestResult.RIGHT)
                    {
                        m_apex = m_rightPoint;
                        m_funnelConstructed = false;
                        ConstructFunnel(m_rightIndex + 1);
                        return m_rightIndex + 1;
                    }
                    if (rightResult == PointTestResult.LEFT)
                    {
                        m_apex = m_leftPoint;
                        m_funnelConstructed = false;
                        ConstructFunnel(m_leftIndex + 1);
                        return m_leftIndex + 1;
                    }
                    if (leftResult == PointTestResult.INSIDE || rightResult == PointTestResult.INSIDE)
                    {
                        m_debugFunnel.Add(new FunnelState() { Apex = m_apex, Left = m_leftPoint, Right = m_rightPoint });
                    }
                }

                return index + 1;
            }

            private void GetEdgeVerticesSafe(MyNavigationTriangle triangle, int edgeIndex, out Vector3 left, out Vector3 right)
            {
                triangle.GetEdgeVertices(edgeIndex, out left, out right);

                float d = (left - right).LengthSquared();

                bool leftDangerous = triangle.IsEdgeVertexDangerous(edgeIndex, predVertex: true);
                bool rightDangerous = triangle.IsEdgeVertexDangerous(edgeIndex, predVertex: false);

                m_segmentDangerous |= leftDangerous | rightDangerous;

                if (leftDangerous)
                {
                    if (rightDangerous)
                    {
                        if (SAFE_DISTANCE2_SQ > d)
                        {
                            left = (left + right) * 0.5f;
                            right = left;
                        }
                        else
                        {
                            float t = SAFE_DISTANCE / (float)Math.Sqrt(d);
                            Vector3 newLeft = right * t + left * (1.0f - t);
                            right = left * t + right * (1.0f - t);
                            left = newLeft;
                        }
                    }
                    else
                    {
                        if (SAFE_DISTANCE_SQ > d)
                        {
                            left = right;
                        }
                        else
                        {
                            float t = SAFE_DISTANCE / (float)Math.Sqrt(d);
                            left = right * t + left * (1.0f - t);
                        }
                    }
                }
                else
                {
                    if (rightDangerous)
                    {
                        if (SAFE_DISTANCE_SQ > d)
                        {
                            right = left;
                        }
                        else
                        {
                            float t = SAFE_DISTANCE / (float)Math.Sqrt(d);
                            right = left * t + right * (1.0f - t);
                        }
                    }
                }

                m_debugPointsLeft.Add(left);
                m_debugPointsRight.Add(right);
            }

            private void NarrowFunnel(Vector3 point, int index, bool left)
            {
                if (left)
                {
                    m_leftPoint = point;
                    m_leftIndex = index;
                    RecalculateLeftPlane();
                }
                else
                {
                    m_rightPoint = point;
                    m_rightIndex = index;
                    RecalculateRightPlane();
                }
            }

            private void ConstructFunnel(int index)
            {
                System.Diagnostics.Debug.Assert(index >= 0 && index < m_input.Count);
                if (index >= m_endIndex)
                {
                    AddPoint(m_apex);
                    return;
                }

                MyPath<MyNavigationPrimitive>.PathNode tri = m_input[index];
                var triangle = tri.Vertex as MyNavigationTriangle;
                Debug.Assert(triangle != null, "The path node does not contain a triangle!");

                var edge = triangle.GetNavigationEdge(tri.nextVertex);
                GetEdgeVerticesSafe(triangle, tri.nextVertex, out m_leftPoint, out m_rightPoint);

                if (Vector3.IsZero(m_leftPoint - m_apex))
                {
                    m_apex = triangle.Center;
                    return;
                }
                if (Vector3.IsZero(m_rightPoint - m_apex))
                {
                    m_apex = triangle.Center;
                    return;
                }

                m_apexNormal = triangle.Normal;
                float d = m_leftPoint.Dot(m_apexNormal);
                m_apex = m_apex - m_apexNormal * (m_apex.Dot(m_apexNormal) - d);

                m_leftIndex = m_rightIndex = index;
                RecalculateLeftPlane();
                RecalculateRightPlane();

                m_funnelConstructed = true;

                AddPoint(m_apex);
                m_debugFunnel.Add(new FunnelState() { Apex = m_apex, Left = m_leftPoint, Right = m_rightPoint });
            }

            private PointTestResult TestPoint(Vector3 point)
            {
                if (point.Dot(m_leftPlaneNormal) < -m_leftD) return PointTestResult.LEFT;
                if (point.Dot(m_rightPlaneNormal) < -m_rightD) return PointTestResult.RIGHT;
                return PointTestResult.INSIDE;
            }

            private void RecalculateLeftPlane()
            {
                Vector3 v = m_leftPoint - m_apex;
                v.Normalize();
                m_leftPlaneNormal = Vector3.Cross(v, m_apexNormal);
                m_leftPlaneNormal.Normalize();
                m_leftD = -m_leftPoint.Dot(m_leftPlaneNormal);
            }

            private void RecalculateRightPlane()
            {
                Vector3 v = m_rightPoint - m_apex;
                v.Normalize();
                m_rightPlaneNormal = Vector3.Cross(m_apexNormal, v);
                m_rightPlaneNormal.Normalize();
                m_rightD = -m_rightPoint.Dot(m_rightPlaneNormal);
            }
        }

        private MyDynamicObjectPool<MyNavigationTriangle> m_triPool;

        private MyWingedEdgeMesh m_mesh;
        public MyWingedEdgeMesh Mesh { get { return m_mesh; } }

        private MyNavgroupLinks m_externalLinks;


        // DEBUG DRAW STUFF:
        private Vector3 m_vertex;
        private Vector3 m_left;
        private Vector3 m_right;
        private Vector3 m_normal;
        private List<Vector3> m_vertexList = new List<Vector3>();
        private static List<Vector3> m_debugPointsLeft = new List<Vector3>();
        private static List<Vector3> m_debugPointsRight = new List<Vector3>();
        private static List<Vector3> m_path = new List<Vector3>();
        private static List<Vector3> m_path2;
        private static List<FunnelState> m_debugFunnel = new List<FunnelState>();
        public static int m_debugFunnelIdx = 0;
        public struct FunnelState
        {
            public Vector3 Apex;
            public Vector3 Left;
            public Vector3 Right;
        }

        public MyNavigationMesh(MyNavgroupLinks externalLinks, int trianglePrealloc = 16, Func<long> timestampFunction = null)
            : base(128, timestampFunction)
        {
            m_triPool = new MyDynamicObjectPool<MyNavigationTriangle>(trianglePrealloc);
            m_mesh = new MyWingedEdgeMesh();
            m_externalLinks = externalLinks;
        }

        // Adds a triangle and connects it to the other triangles in the mesh by the given edges.
        // Because connecting by vertices would produce non-manifold meshes, we connect triangles by their edges.
        // When a triangle is added that connects to another triangle only by a vertex, the two touching vertices will be regarded
        // as two separate vertices and they will only be merged when another triangle is added that shares two edges with the two
        // original triangles.
        //
        // When the method returns, edgeAB, edgeBC and edgeCA will contain the indices for the new edges.
        // The positions of vertices A, B and C will be unmodified and might not correspond to the real positions of the resulting
        // triangle's vertices (because the positions of the original edge vertices will sometimes be used due to edge merging).
        //
        // Note: The triangle's vertices and edges must be ordered clockwise:
        //                 B
        //                / \
        //               /   \
        //             AB     BC
        //             /       \
        //            /         \
        //           A -- CA --- C
        protected MyNavigationTriangle AddTriangle(ref Vector3 A, ref Vector3 B, ref Vector3 C, ref int edgeAB, ref int edgeBC, ref int edgeCA)
        {
            MyNavigationTriangle newTri = m_triPool.Allocate();

            // There are several cases that need to be handled and they can be distinguished by the number of
            // existing edges and their shared vertices.

            int newEdgeCount = 0;
            newEdgeCount += edgeAB == -1 ? 1 : 0;
            newEdgeCount += edgeBC == -1 ? 1 : 0;
            newEdgeCount += edgeCA == -1 ? 1 : 0;

            int newTriIndex = -1;
            if (newEdgeCount == 3)
            {
                newTriIndex = m_mesh.MakeNewTriangle(newTri, ref A, ref B, ref C, out edgeAB, out edgeBC, out edgeCA);
            }
            else if (newEdgeCount == 2)
            {
                if (edgeAB != -1)
                    newTriIndex = m_mesh.ExtrudeTriangleFromEdge(ref C, edgeAB, newTri, out edgeBC, out edgeCA);
                else if (edgeBC != -1)
                    newTriIndex = m_mesh.ExtrudeTriangleFromEdge(ref A, edgeBC, newTri, out edgeCA, out edgeAB);
                else
                    newTriIndex = m_mesh.ExtrudeTriangleFromEdge(ref B, edgeCA, newTri, out edgeAB, out edgeBC);
            }
            else if (newEdgeCount == 1)
            {
                if (edgeAB == -1)
                    newTriIndex = GetTriangleOneNewEdge(ref edgeAB, ref edgeBC, ref edgeCA, newTri);
                else if (edgeBC == -1)
                    newTriIndex = GetTriangleOneNewEdge(ref edgeBC, ref edgeCA, ref edgeAB, newTri);
                else
                    newTriIndex = GetTriangleOneNewEdge(ref edgeCA, ref edgeAB, ref edgeBC, newTri);
            }
            else
            {
                var entryAB = m_mesh.GetEdge(edgeAB);
                var entryBC = m_mesh.GetEdge(edgeBC);
                var entryCA = m_mesh.GetEdge(edgeCA);
                int sharedA = entryCA.TryGetSharedVertex(ref entryAB);
                int sharedB = entryAB.TryGetSharedVertex(ref entryBC);
                int sharedC = entryBC.TryGetSharedVertex(ref entryCA);

                int sharedVertCount = 0;
                sharedVertCount += sharedA == -1 ? 0 : 1;
                sharedVertCount += sharedB == -1 ? 0 : 1;
                sharedVertCount += sharedC == -1 ? 0 : 1;

                if (sharedVertCount == 3)
                {
                    newTriIndex = m_mesh.MakeFace(newTri, edgeAB);
                }
                else if (sharedVertCount == 2)
                {
                    if (sharedA == -1)
                        newTriIndex = GetTriangleTwoSharedVertices(edgeAB, edgeBC, ref edgeCA, sharedB, sharedC, newTri);
                    else if (sharedB == -1)
                        newTriIndex = GetTriangleTwoSharedVertices(edgeBC, edgeCA, ref edgeAB, sharedC, sharedA, newTri);
                    else
                        newTriIndex = GetTriangleTwoSharedVertices(edgeCA, edgeAB, ref edgeBC, sharedA, sharedB, newTri);
                }
                else if (sharedVertCount == 1)
                {
                    if (sharedA != -1)
                        newTriIndex = GetTriangleOneSharedVertex(edgeCA, edgeAB, ref edgeBC, sharedA, newTri);
                    else if (sharedB != -1)
                        newTriIndex = GetTriangleOneSharedVertex(edgeAB, edgeBC, ref edgeCA, sharedB, newTri);
                    else
                        newTriIndex = GetTriangleOneSharedVertex(edgeBC, edgeCA, ref edgeAB, sharedC, newTri);
                }
                else
                {
                    int next, prev;
                    newTriIndex = m_mesh.ExtrudeTriangleFromEdge(ref C, edgeAB, newTri, out next, out prev);
                    m_mesh.MergeEdges(prev, edgeCA);
                    m_mesh.MergeEdges(next, edgeBC);
                }
            }

            newTri.Init(this, newTriIndex);
            return newTri;
        }

        protected void RemoveTriangle(MyNavigationTriangle tri)
        {
            m_mesh.RemoveFace(tri.Index);
            m_triPool.Deallocate(tri);
        }

        private int GetTriangleOneNewEdge(ref int newEdge, ref int succ, ref int pred, MyNavigationTriangle newTri)
        {
            var edgePred = m_mesh.GetEdge(pred);
            var edgeSucc = m_mesh.GetEdge(succ);

            int sharedVertex = edgePred.TryGetSharedVertex(ref edgeSucc);
            if (sharedVertex == -1)
            {
                int formerSucc = succ;
                Vector3 extrudePos = m_mesh.GetVertexPosition(edgeSucc.GetFacePredVertex(-1));
                int faceIndex = m_mesh.ExtrudeTriangleFromEdge(ref extrudePos, pred, newTri, out newEdge, out succ);
                m_mesh.MergeEdges(formerSucc, succ);
                return faceIndex;
            }
            else
            {
                int vertPred = edgePred.OtherVertex(sharedVertex);
                int vertSucc = edgeSucc.OtherVertex(sharedVertex);
                return m_mesh.MakeEdgeFace(vertPred, vertSucc, pred, succ, newTri, out newEdge);
            }
        }

        private int GetTriangleOneSharedVertex(int edgeCA, int edgeAB, ref int edgeBC, int sharedA, MyNavigationTriangle newTri)
        {
            int vertB = m_mesh.GetEdge(edgeAB).OtherVertex(sharedA);
            int vertC = m_mesh.GetEdge(edgeCA).OtherVertex(sharedA);
            int formerBC = edgeBC;
            int face = m_mesh.MakeEdgeFace(vertB, vertC, edgeAB, edgeCA, newTri, out edgeBC);
            m_mesh.MergeEdges(formerBC, edgeBC);
            return face;
        }

        private int GetTriangleTwoSharedVertices(int edgeAB, int edgeBC, ref int edgeCA, int sharedB, int sharedC, MyNavigationTriangle newTri)
        {
            int vertA = m_mesh.GetEdge(edgeAB).OtherVertex(sharedB);
            int formerCA = edgeCA;
            int face = m_mesh.MakeEdgeFace(sharedC, vertA, edgeBC, edgeAB, newTri, out edgeCA);
            m_mesh.MergeAngle(formerCA, edgeCA, sharedC);
            return face;
        }

        public MyNavigationTriangle GetTriangle(int index)
        {
            return m_mesh.GetFace(index).GetUserData<MyNavigationTriangle>();
        }

        protected MyNavigationTriangle GetEdgeTriangle(int edgeIndex)
        {
            var edge = m_mesh.GetEdge(edgeIndex);
            if (edge.LeftFace == -1)
                return GetTriangle(edge.RightFace);
            else
            {
                System.Diagnostics.Debug.Assert(edge.RightFace == -1);
                return GetTriangle(edge.LeftFace);
            }
        }

        protected List<Vector4D> FindRefinedPath(MyNavigationTriangle start, MyNavigationTriangle end, ref Vector3 startPoint, ref Vector3 endPoint)
        {
            MyPath<MyNavigationPrimitive> triPath = FindPath(start, end);
            if (triPath == null) return null;

            // Path made of triangle centers
            //List<Vector3> path = new List<Vector3>();
            //path.Add(startPoint);
            //for (int j = 1; j < triPath.Count - 1; ++j)
            //{
            //    path.Add((triPath[j].Vertex as MyNavigationTriangle).Center);
            //}
            //path.Add(endPoint);
            //m_path2 = path;
            //return path;

            // Refined path smoothed by the funnel algorithm
            List<Vector4D> refinedPath = new List<Vector4D>();
            refinedPath.Add(new Vector4D(startPoint, 1.0f));

            Funnel funnel = new Funnel();
            funnel.Calculate(triPath, refinedPath, ref startPoint, ref endPoint, 0, triPath.Count - 1);

            m_path.Clear();
            foreach (var p in refinedPath) m_path.Add(new Vector3D(p));

            return refinedPath;
        }

        // Output is Vector4D, because the first three coords specify path node center and the fourth defines the radius
        public void RefinePath(MyPath<MyNavigationPrimitive> path, List<Vector4D> output, ref Vector3 startPoint, ref Vector3 endPoint, int begin, int end)
        {
            Funnel funnel = new Funnel();
            funnel.Calculate(path, output, ref startPoint, ref endPoint, begin, end);
        }

        public abstract Vector3 GlobalToLocal(Vector3D globalPos);
        public abstract Vector3D LocalToGlobal(Vector3 localPos);

        public abstract MyHighLevelGroup HighLevelGroup { get; }
        public abstract MyHighLevelPrimitive GetHighLevelPrimitive(MyNavigationPrimitive myNavigationTriangle);
        public abstract IMyHighLevelComponent GetComponent(MyHighLevelPrimitive highLevelPrimitive);

        public abstract MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, ref double closestDistanceSq);

        /// <summary>
        /// Gets rid of the vertex and edge preallocation pools. You MUST NOT add any more triangles or edges after calling this method.
        /// It is here only to save memory when the mesh won't be modified any more.
        /// </summary>
        public void ErasePools()
        {
            m_triPool = null;
        }

        [Conditional("DEBUG")]
        public virtual void DebugDraw(ref Matrix drawMatrix)
        {
            if (!MyDebugDrawSettings.ENABLE_DEBUG_DRAW) return;

            if (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES != MyWEMDebugDrawMode.NONE)
            {
                m_mesh.DebugDraw(ref drawMatrix, MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES);
                m_mesh.CustomDebugDrawFaces(ref drawMatrix, MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES, (obj) => (obj as MyNavigationTriangle).Index.ToString());
            }

            //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "Navmesh size approximation: " + ApproximateMemoryFootprint() + "B", Color.Yellow, 1.0f);

            if (MyFakes.DEBUG_DRAW_FUNNEL)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(m_vertex, drawMatrix), 0.05f, Color.Yellow.ToVector3(), 1.0f, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(m_vertex + m_normal, drawMatrix), 0.05f, Color.Orange.ToVector3(), 1.0f, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(m_left, drawMatrix), 0.05f, Color.Red.ToVector3(), 1.0f, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(m_right, drawMatrix), 0.05f, Color.Green.ToVector3(), 1.0f, false);

                foreach (var point in m_debugPointsLeft)
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(point, drawMatrix), 0.03f, Color.Red.ToVector3(), 1.0f, false);
                }
                foreach (var point in m_debugPointsRight)
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(Vector3.Transform(point, drawMatrix), 0.04f, Color.Green.ToVector3(), 1.0f, false);
                }

                Vector3? prevPoint = null;
                if (m_path != null)
                {
                    foreach (var point in m_path)
                    {
                        Vector3 pointWorld = Vector3.Transform(point, drawMatrix);
                        VRageRender.MyRenderProxy.DebugDrawSphere(pointWorld + Vector3.Up * 0.2f, 0.02f, Color.Orange.ToVector3(), 1.0f, false);
                        if (prevPoint.HasValue)
                        {
                            VRageRender.MyRenderProxy.DebugDrawLine3D(prevPoint.Value + Vector3.Up * 0.2f, pointWorld + Vector3.Up * 0.2f, Color.Orange, Color.Orange, true);
                        }

                        prevPoint = pointWorld;
                    }
                }
                prevPoint = null;
                if (m_path2 != null)
                {
                    foreach (var point in m_path2)
                    {
                        Vector3 pointWorld = Vector3.Transform(point, drawMatrix);
                        if (prevPoint.HasValue)
                        {
                            VRageRender.MyRenderProxy.DebugDrawLine3D(prevPoint.Value + Vector3.Up * 0.1f, pointWorld + Vector3.Up * 0.1f, Color.Violet, Color.Violet, true);
                        }

                        prevPoint = pointWorld;
                    }
                }

                if (m_debugFunnel.Count > 0)
                {
                    var section = m_debugFunnel[m_debugFunnelIdx % m_debugFunnel.Count];
                    var a = Vector3.Transform(section.Apex, drawMatrix);
                    var l = Vector3.Transform(section.Left, drawMatrix);
                    var r = Vector3.Transform(section.Right, drawMatrix);
                    l = a + (l - a) * 10.0f;
                    r = a + (r - a) * 10.0f;
                    Color c = Color.Cyan;
                    VRageRender.MyRenderProxy.DebugDrawLine3D(a + Vector3.Up * 0.1f, l + Vector3.Up * 0.1f, c, c, true);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(a + Vector3.Up * 0.1f, r + Vector3.Up * 0.1f, c, c, true);
                }
            }
        }

        public void RemoveFace(int index)
        {
            m_mesh.RemoveFace(index);
        }

        public virtual MatrixD GetWorldMatrix()
        {
            return MatrixD.Identity;
        }

        [Conditional("DEBUG")]
        public void CheckMeshConsistency()
        {
            m_mesh.CheckMeshConsistency();
        }

        public int ApproximateMemoryFootprint()
        {
            return
                m_mesh.ApproximateMemoryFootprint() +
#if XB1
                m_triPool.Count * 88;
#else // !XB1
                m_triPool.Count * (Environment.Is64BitProcess ? 88 : 56);
#endif // !XB1
        }

        public int GetExternalNeighborCount(MyNavigationPrimitive primitive)
        {
            return m_externalLinks == null ? 0 : m_externalLinks.GetLinkCount(primitive);
        }

        public MyNavigationPrimitive GetExternalNeighbor(MyNavigationPrimitive primitive, int index)
        {
            return m_externalLinks == null ? null : m_externalLinks.GetLinkedNeighbor(primitive, index);
        }

        public IMyPathEdge<MyNavigationPrimitive> GetExternalEdge(MyNavigationPrimitive primitive, int index)
        {
            return m_externalLinks == null ? null : m_externalLinks.GetEdge(primitive, index);
        }
    }
}
