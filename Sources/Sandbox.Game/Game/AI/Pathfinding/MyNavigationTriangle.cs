using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Algorithms;
using VRage.Generics;
using VRage.Utils;
using VRageMath;
using VRageRender.Utils;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyNavigationTriangle : MyNavigationPrimitive
    {
        private MyNavigationMesh m_navMesh;
        public MyNavigationMesh Parent
        {
            get
            {
                return m_navMesh;
            }
        }

        private int m_triIndex;

        /// <summary>
        /// Face index of this triangle in the winged-edge mesh
        /// </summary>
        public int Index { get { return m_triIndex; } }

        public int ComponentIndex { get; set; }

        // CH: Just for debug here (to disallow registering triangles in more than one cube)
        public bool Registered;

        // CH: TODO: When needed, optimize this by saving it in the triangle.
        public Vector3 Center
        {
            get
            {
                int i = 0;
                Vector3 acc = Vector3.Zero;
                var e = m_navMesh.Mesh.GetFace(m_triIndex).GetVertexEnumerator();
                while (e.MoveNext())
                {
                    acc += e.Current;
                    i++;
                }
                return acc / i;
            }
        }

        public Vector3 Normal
        {
            get
            {
                var e = m_navMesh.Mesh.GetFace(m_triIndex).GetVertexEnumerator();
                e.MoveNext(); Vector3 a = e.Current;
                e.MoveNext(); Vector3 b = e.Current;
                e.MoveNext(); Vector3 c = e.Current;
                Debug.Assert(e.MoveNext() == false, "Triangle has more vertices than three!");

                Vector3 normal = (c - a).Cross(b - a);
                Debug.Assert(!Vector3.IsZero(normal), "Triangle is invalid!");
                normal.Normalize();

                return normal;
            }
        }

        public void Init(MyNavigationMesh mesh, int triangleIndex)
        {
            m_navMesh = mesh;
            m_triIndex = triangleIndex;
            ComponentIndex = -1;
            Registered = false;
            HasExternalNeighbors = false;
        }

        public override string ToString()
        {
            return m_navMesh.ToString() + "; Tri: " + Index;
        }

        public void GetVertices(out Vector3 a, out Vector3 b, out Vector3 c)
        {
            var e = m_navMesh.Mesh.GetFace(m_triIndex).GetVertexEnumerator();

            e.MoveNext();
            a = e.Current;
            e.MoveNext();
            b = e.Current;
            e.MoveNext();
            c = e.Current;

            Debug.Assert(e.MoveNext() == false);
        }

        public void GetVertices(out int indA, out int indB, out int indC, out Vector3 a, out Vector3 b, out Vector3 c)
        {
            var e = m_navMesh.Mesh.GetFace(m_triIndex).GetVertexEnumerator();

            e.MoveNext();
            indA = e.CurrentIndex;
            a = e.Current;
            e.MoveNext();
            indB = e.CurrentIndex;
            b = e.Current;
            e.MoveNext();
            indC = e.CurrentIndex;
            c = e.Current;

            Debug.Assert(e.MoveNext() == false);
        }

        public void GetTransformed(ref MatrixI tform, out Vector3 newA, out Vector3 newB, out Vector3 newC)
        {
            var e = m_navMesh.Mesh.GetFace(m_triIndex).GetVertexEnumerator();

            e.MoveNext();
            newA = e.Current;
            Vector3.Transform(ref newA, ref tform, out newA);

            e.MoveNext();
            newB = e.Current;
            Vector3.Transform(ref newB, ref tform, out newB);

            e.MoveNext();
            newC = e.Current;
            Vector3.Transform(ref newC, ref tform, out newC);

            Debug.Assert(e.MoveNext() == false);
        }

        public MyWingedEdgeMesh.FaceVertexEnumerator GetVertexEnumerator()
        {
            return m_navMesh.Mesh.GetFace(m_triIndex).GetVertexEnumerator();
        }

        #region Edges

        public MyNavigationEdge GetNavigationEdge(int index)
        {
            MyWingedEdgeMesh mesh = m_navMesh.Mesh;

            int i = GetEdgeIndex(index);
            MyWingedEdgeMesh.Edge edge = mesh.GetEdge(i);
            MyNavigationTriangle tri1 = null;
            MyNavigationTriangle tri2 = null;
            if (edge.LeftFace != -1)
                tri1 = mesh.GetFace(edge.LeftFace).GetUserData<MyNavigationTriangle>();
            if (edge.RightFace != -1)
                tri2 = mesh.GetFace(edge.RightFace).GetUserData<MyNavigationTriangle>();
            MyNavigationEdge.Static.Init(tri1, tri2, i);
            return MyNavigationEdge.Static;
        }

        public void GetEdgeVertices(int index, out Vector3 pred, out Vector3 succ)
        {
            MyWingedEdgeMesh mesh = m_navMesh.Mesh;

            int i = GetEdgeIndex(index);
            MyWingedEdgeMesh.Edge edge = mesh.GetEdge(i);
            pred = mesh.GetVertexPosition(edge.GetFacePredVertex(m_triIndex));
            succ = mesh.GetVertexPosition(edge.GetFaceSuccVertex(m_triIndex));
        }

        /// <summary>
        /// Whether it's dangerous for the bot to navigate close to this edge
        /// </summary>
        public bool IsEdgeVertexDangerous(int index, bool predVertex)
        {
            MyWingedEdgeMesh mesh = m_navMesh.Mesh;
            int i = GetEdgeIndex(index);
            int e = i;
            MyWingedEdgeMesh.Edge edge = mesh.GetEdge(e);
            int v = predVertex ? edge.GetFacePredVertex(m_triIndex) : edge.GetFaceSuccVertex(m_triIndex);

            do
            {
                if (IsTriangleDangerous(edge.VertexLeftFace(v))) return true;
                e = edge.GetNextVertexEdge(v);
                edge = mesh.GetEdge(e);
            } while (e != i);

            return false;
        }

        public void FindDangerousVertices(List<int> output)
        {
            var e = m_navMesh.Mesh.GetFace(m_triIndex).GetVertexEnumerator();

            int a, b, c;

            e.MoveNext();
            a = e.CurrentIndex;
            e.MoveNext();
            b = e.CurrentIndex;
            e.MoveNext();
            c = e.CurrentIndex;
        }

        public int GetEdgeIndex(int index)
        {
            var enumerator = new MyWingedEdgeMesh.FaceEdgeEnumerator(m_navMesh.Mesh, m_triIndex);
            enumerator.MoveNext();
            while (index != 0)
            {
                enumerator.MoveNext();
                index--;
            }
            return enumerator.Current;
        }

        #endregion

        private static bool IsTriangleDangerous(int triIndex)
        {
            return triIndex == -1;
        }

        #region MyNavigationPrimitive overrides

        public override Vector3 Position { get { return Center; } }

        public override Vector3D WorldPosition
        {
            get
            {
                MatrixD mWorld = m_navMesh.GetWorldMatrix();
                Vector3D center = Center;
                Vector3D retval;
                Vector3D.Transform(ref center, ref mWorld, out retval);
                return retval;
            }
        }

        public override Vector3 ProjectLocalPoint(Vector3 point)
        {
            Vector3 a, b, c, ab, ac, ae, triCross, bCross, cCross;

            GetVertices(out a, out b, out c);
            Vector3.Subtract(ref b, ref a, out ab);
            Vector3.Subtract(ref c, ref a, out ac);
            Vector3.Subtract(ref point, ref a, out ae);
            Vector3.Cross(ref ab, ref ac, out triCross);
            Vector3.Cross(ref ab, ref ae, out bCross);
            Vector3.Cross(ref ae, ref ac, out cCross);

            float denom = 1.0f / triCross.LengthSquared();
            float bSize2 = Vector3.Dot(bCross, triCross);
            float cSize2 = Vector3.Dot(cCross, triCross);

            float bb = cSize2 * denom;
            float bc = bSize2 * denom;
            float ba = 1.0f - bb - bc;

            Debug.Assert(Math.Abs(ba + bb + bc - 1.0f) < 0.001f);

            if (ba < 0.0f)
            {
                if (bb < 0.0f)
                {
                    return c;
                }
                else if (bc < 0.0f)
                {
                    return b;
                }
                else
                {
                    float mult = 1.0f / (1.0f - ba);
                    ba = 0.0f;
                    bb = bb * mult;
                    bc = bc * mult;
                }
            }
            else if (bb < 0.0f)
            {
                if (bc < 0.0f)
                {
                    return a;
                }
                else
                {
                    float mult = 1.0f / (1.0f - bb);
                    bb = 0.0f;
                    ba = ba * mult;
                    bc = bc * mult;
                }
            }
            else if (bc < 0.0f)
            {
                float mult = 1.0f / (1.0f - bc);
                bc = 0.0f;
                ba = ba * mult;
                bb = bb * mult;
            }

            Debug.Assert(Math.Abs(ba + bb + bc - 1.0f) < 0.001f);

            return a * ba + b * bb + c * bc;
        }

        public override IMyNavigationGroup Group { get { return m_navMesh; } }

        public override int GetOwnNeighborCount()
        {
            return 3;
        }

        public override IMyPathVertex<MyNavigationPrimitive> GetOwnNeighbor(int index)
        {
            int i = GetEdgeIndex(index);
            MyWingedEdgeMesh.Edge edge = m_navMesh.Mesh.GetEdge(i);
            int neighborIndex = edge.OtherFace(m_triIndex);
            if (neighborIndex != -1)
                return m_navMesh.Mesh.GetFace(neighborIndex).GetUserData<MyNavigationPrimitive>();
            else
                return null;
        }

        public override IMyPathEdge<MyNavigationPrimitive> GetOwnEdge(int index)
        {
            return GetNavigationEdge(index);
        }

        public override MyHighLevelPrimitive GetHighLevelPrimitive()
        {
            return m_navMesh.GetHighLevelPrimitive(this);
        }

        #endregion
    }
}
