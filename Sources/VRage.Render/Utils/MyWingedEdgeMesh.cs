using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Library;
using VRage.Utils;
using VRageMath;

namespace VRageRender.Utils
{
    [Flags]
    public enum MyWEMDebugDrawMode
    {
        NONE = 0,
        LINES = 1 << 0,
        EDGES = 1 << 1,
        LINES_DEPTH = 1 << 2,
        FACES = 1 << 3,
        VERTICES = 1 << 4,
        VERTICES_DETAILED = 1 << 5,
        NORMALS = 1 << 6,
    }

    public class MyWingedEdgeMesh
    {
        public static bool BASIC_CONSISTENCY_CHECKS = false;
        public static bool ADVANCED_CONSISTENCY_CHECKS = false;
        public static int INVALID_INDEX = -1;

        // For consistency checking purposes
        private static HashSet<int> m_tmpFreeEdges = new HashSet<int>();
        private static HashSet<int> m_tmpFreeFaces = new HashSet<int>();
        private static HashSet<int> m_tmpFreeVertices = new HashSet<int>();
        private static HashSet<int> m_tmpVisitedIndices = new HashSet<int>();
        private static HashSet<int> m_tmpDebugDrawFreeIndices = new HashSet<int>();

        private static List<int> m_tmpIndexList = new List<int>();

        // This is explanation of the indices in the edge table entry. Face's edges are always ordered clockwise (i.e. clockwise winding).
        // It also means that normal of the face is obtained if you point with the fingers of your LEFT hand in the direction of the
        // face's winding and look at where your thumb is pointing.
        // Face traversal is done using pointers Pred and Succ in the respective face.
        //
        //          ___            -._       _.-
        //             \    L.pred    `-V.2-'     R.succ      .->
        // Order of the |                ^                   /  Order of the
        // left face's  |     Left face  |  Right face      |   right face's
        // edges        |                |                  |   edges
        //            _/    L.succ   _.-V.1-._    R.pred     \__
        //         <-'             -'         '-
        private struct EdgeTableEntry
        {
            //public int EdgeIndex; // Do I need this?
            public int Vertex1;
            public int Vertex2;
            public int LeftFace;
            public int RightFace;
            public int LeftPred;
            public int LeftSucc;
            public int RightPred;
            public int RightSucc;

            /// <summary>
            /// Only valid for empty (deallocated) table entries. In that case, it points to the next free table entry.
            /// If this is -1, this entry is the last free entry.
            /// </summary>
            public int NextFreeEntry
            {
                get
                {
                    return Vertex1;
                }
                set
                {
                    Vertex1 = value;
                }
            }

            public void Init()
            {
                Vertex1   = INVALID_INDEX;
                Vertex2   = INVALID_INDEX;
                LeftFace  = INVALID_INDEX;
                RightFace = INVALID_INDEX;
                LeftPred  = INVALID_INDEX;
                LeftSucc  = INVALID_INDEX;
                RightPred = INVALID_INDEX;
                RightSucc = INVALID_INDEX;
            }

            #region Vertex access methods

            public int OtherVertex(int vert)
            {
                Debug.Assert(vert == Vertex1 || vert == Vertex2, "Vertex does not belong to the edge!");
                return vert == Vertex1 ? Vertex2 : Vertex1;
            }

            public void GetFaceVertices(int face, out int predVertex, out int succVertex)
            {
                Debug.Assert(face == LeftFace || face == RightFace, "Accessing vertices of a face in an incorrect edge entry!");
                if (face == LeftFace)
                {
                    predVertex = Vertex2;
                    succVertex = Vertex1;
                }
                else
                {
                    predVertex = Vertex1;
                    succVertex = Vertex2;
                }
            }

            public int GetFaceSuccVertex(int face)
            {
                Debug.Assert(face == LeftFace || face == RightFace, "Getting vertex of a face in an incorrect edge entry!");
                if (face == LeftFace) return Vertex1;
                return Vertex2;
            }

            public int GetFacePredVertex(int face)
            {
                Debug.Assert(face == LeftFace || face == RightFace, "Getting vertex of a face in an incorrect edge entry!");
                if (face == LeftFace) return Vertex2;
                return Vertex1;
            }

            public void SetFaceSuccVertex(int face, int vertex)
            {
                Debug.Assert(face == LeftFace || face == RightFace, "Setting vertex of a face in an incorrect edge entry!");
                if (face == LeftFace) Vertex1 = vertex;
                else Vertex2 = vertex;
            }

            public void SetFacePredVertex(int face, int vertex)
            {
                Debug.Assert(face == LeftFace || face == RightFace, "Setting vertex of a face in an incorrect edge entry!");
                if (face == LeftFace) Vertex2 = vertex;
                else Vertex1 = vertex;
            }

            /// <summary>
            /// Returns -1 if there is no shared edge
            /// </summary>
            public int TryGetSharedVertex(ref EdgeTableEntry otherEdge)
            {
                // Assert if there are two shared vertices
                Debug.Assert((Vertex1 != otherEdge.Vertex1 || Vertex2 != otherEdge.Vertex2) && (Vertex2 != otherEdge.Vertex1 || Vertex1 != otherEdge.Vertex2));
                if (Vertex1 == otherEdge.Vertex1) return Vertex1;
                if (Vertex1 == otherEdge.Vertex2) return Vertex1;
                if (Vertex2 == otherEdge.Vertex1) return Vertex2;
                if (Vertex2 == otherEdge.Vertex2) return Vertex2;
                return INVALID_INDEX;
            }

            public void ChangeVertex(int oldVertex, int newVertex)
            {
                Debug.Assert(Vertex1 == oldVertex || Vertex2 == oldVertex, "Changing vertex in an edge which does not contain it!");
                if (oldVertex == Vertex1) Vertex1 = newVertex;
                else Vertex2 = newVertex;
            }

            #endregion

            #region Face access methods

            public int OtherFace(int face)
            {
                Debug.Assert(face == LeftFace || face == RightFace, "Accessing face in an incorrect edge entry!");
                if (face == LeftFace) return RightFace;
                else return LeftFace;
            }

            /// <summary>
            /// Returns a face to the left when going towards the given vertex
            /// </summary>
            public int VertexLeftFace(int vertex)
            {
                Debug.Assert(Vertex1 == vertex || Vertex2 == vertex, "Accessing face in an incorrect edge entry!");
                return vertex == Vertex1 ? RightFace : LeftFace;
            }

            /// <summary>
            /// Returns a face to the right when going towards the given vertex
            /// </summary>
            public int VertexRightFace(int vertex)
            {
                Debug.Assert(Vertex1 == vertex || Vertex2 == vertex, "Accessing face in an incorrect edge entry!");
                return vertex == Vertex1 ? LeftFace : RightFace;
            }

            public void AddFace(int face)
            {
                Debug.Assert(LeftFace == INVALID_INDEX || RightFace == INVALID_INDEX, "Cannot add face to an edge. Both faces of an edge were not empty!");
                if (LeftFace == INVALID_INDEX) LeftFace = face;
                else RightFace = face;
            }

            public void ChangeFace(int previousFace, int newFace)
            {
                Debug.Assert(previousFace == LeftFace || previousFace == RightFace, "Changing face in an incorrect edge entry!");
                if (previousFace == LeftFace) LeftFace = newFace;
                else RightFace = newFace;
            }

            #endregion

            #region Edge access methods

            /// <summary>
            /// Returns the successor of this edge in the given face
            /// </summary>
            public int FaceSucc(int faceIndex)
            {
                Debug.Assert(faceIndex == LeftFace || faceIndex == RightFace, "Iterating over a face in an incorrect edge table entry!");
                if (faceIndex == LeftFace) return LeftSucc;
                else return RightSucc;
            }

            /// <summary>
            /// Sets the successor of this edge in the given face
            /// </summary>
            public void SetFaceSucc(int faceIndex, int newSucc)
            {
                Debug.Assert(faceIndex == LeftFace || faceIndex == RightFace, "Setting face successor in an incorrect edge table entry!");
                if (faceIndex == LeftFace) LeftSucc = newSucc;
                else RightSucc = newSucc;
            }

            /// <summary>
            /// Returns the predecessor of this edge in the given face
            /// </summary>
            public int FacePred(int faceIndex)
            {
                Debug.Assert(faceIndex == LeftFace || faceIndex == RightFace, "Iterating over a face in an incorrect edge table entry!");
                if (faceIndex == LeftFace) return LeftPred;
                else return RightPred;
            }

            /// <summary>
            /// Sets the predecessor of this edge in the given face
            /// </summary>
            public void SetFacePred(int faceIndex, int newPred)
            {
                Debug.Assert(faceIndex == LeftFace || faceIndex == RightFace, "Setting face predecessor in an incorrect edge table entry!");
                if (faceIndex == LeftFace) LeftPred = newPred;
                else RightPred = newPred;
            }

            /// <summary>
            /// Gets the successor around the given vertex.
            /// </summary>
            public int VertexSucc(int vertexIndex)
            {
                Debug.Assert(vertexIndex == Vertex1 || vertexIndex == Vertex2, "Getting successor around a vertex in an incorrect edge table entry!");
                return (vertexIndex == Vertex1) ? LeftSucc : RightSucc;
            }

            /// <summary>
            /// Sets the successor around the given vertex.
            /// </summary>
            /// <returns>The old successor value</returns>
            public int SetVertexSucc(int vertexIndex, int newSucc)
            {
                int retval = INVALID_INDEX;

                Debug.Assert(vertexIndex == Vertex1 || vertexIndex == Vertex2, "Setting successor around a vertex in an incorrect edge table entry!");
                if (vertexIndex == Vertex1)
                {
                    retval = LeftSucc;
                    LeftSucc = newSucc;
                }
                else
                {
                    retval = RightSucc;
                    RightSucc = newSucc;
                }

                return retval;
            }

            /// <summary>
            /// Gets the predecessor around the given vertex
            /// </summary>
            public int VertexPred(int vertexIndex)
            {
                Debug.Assert(vertexIndex == Vertex1 || vertexIndex == Vertex2, "Getting predecessor around a vertex in an incorrect edge table entry!");
                return (vertexIndex == Vertex1) ? RightPred : LeftPred;
            }

            /// <summary>
            /// Sets the predecessor around the given vertex.
            /// </summary>
            /// <returns>The old predecessor value</returns>
            public int SetVertexPred(int vertexIndex, int newPred)
            {
                int retval = INVALID_INDEX;

                Debug.Assert(vertexIndex == Vertex1 || vertexIndex == Vertex2, "Setting predecessor around a vertex in an incorrect edge table entry!");
                if (vertexIndex == Vertex1)
                {
                    retval = RightPred;
                    RightPred = newPred;
                }
                else
                {
                    retval = LeftPred;
                    LeftPred = newPred;
                }

                return retval;
            }

            #endregion

            // Useful for debug
            public override string ToString()
            {
                return "V: " + Vertex1 + ", " + Vertex2 +
                    "; Left (Pred, Face, Succ): " + LeftPred + ", " + LeftFace + ", " + LeftSucc +
                    "; Right (Pred, Face, Succ): " + RightPred + ", " + RightFace + ", " + RightSucc;
            }
        }

        // Each entry contains one of the incident edges of this vertex. We can get to the others by using the edge table
        private struct VertexTableEntry
        {
            public int IncidentEdge;
            public Vector3 VertexPosition;
            
            /// <summary>
            /// Only valid for empty (deallocated) table entries. In that case, it points to the next free table entry.
            /// If this is -1, this entry is the last free entry.
            /// </summary>
            public int NextFreeEntry
            {
                get
                {
                    return IncidentEdge;
                }
                set
                {
                    IncidentEdge = value;
                }
            }

            public override string ToString()
            {
                return VertexPosition.ToString() + " -> " + IncidentEdge;
            }
        }

        private struct FaceTableEntry
        {
            public int IncidentEdge;
            public object UserData;

            /// <summary>
            /// Only valid for empty (deallocated) table entries. In that case, it points to the next free table entry.
            /// If this is -1, this entry is the last free entry.
            /// </summary>
            public int NextFreeEntry
            {
                get
                {
                    return IncidentEdge;
                }
                set
                {
                    IncidentEdge = value;
                }
            }

            public override string ToString()
            {
                return "-> " + IncidentEdge.ToString();
            }
        }

        /// <summary>
        /// Note: This is invalid after the mesh changes!
        /// </summary>
        public struct Edge
        {
            private EdgeTableEntry m_entry;
            private int m_index;

            public int LeftFace
            {
                get
                {
                    return m_entry.LeftFace;
                }
            }

            public int RightFace
            {
                get
                {
                    return m_entry.RightFace;
                }
            }

            public int Vertex1
            {
                get
                {
                    return m_entry.Vertex1;
                }
            }

            public int Vertex2
            {
                get
                {
                    return m_entry.Vertex2;
                }
            }

            public int Index
            {
                get
                {
                    return m_index;
                }
            }

            public Edge(MyWingedEdgeMesh mesh, int index)
            {
                Debug.Assert(mesh.m_edgeTable.Count > index, "Index overflow when creating MyWingedEdgeMesh.Edge");
                m_entry = mesh.GetEdgeEntry(index);
                m_index = index;
            }

            public int TryGetSharedVertex(ref Edge other)
            {
                return m_entry.TryGetSharedVertex(ref other.m_entry);
            }

            public int GetFacePredVertex(int face)
            {
                return m_entry.GetFacePredVertex(face);
            }

            public int GetFaceSuccVertex(int face)
            {
                return m_entry.GetFaceSuccVertex(face);
            }

            public int OtherVertex(int vertex)
            {
                return m_entry.OtherVertex(vertex);
            }

            public int OtherFace(int face)
            {
                return m_entry.OtherFace(face);
            }

            public int GetPreviousFaceEdge(int faceIndex)
            {
                return m_entry.FacePred(faceIndex);
            }

            public int GetNextFaceEdge(int faceIndex)
            {
                return m_entry.FaceSucc(faceIndex);
            }

            public int GetNextVertexEdge(int vertexIndex)
            {
                // This is correct. NEXT edge around a vertex is predecessor around the vertex in one of the faces
                return m_entry.VertexPred(vertexIndex);
            }

            public int VertexLeftFace(int vertexIndex)
            {
                return m_entry.VertexLeftFace(vertexIndex);
            }

            public void ToRay(MyWingedEdgeMesh mesh, ref Ray output)
            {
                Vector3 v1 = mesh.GetVertexPosition(Vertex1);
                Vector3 v2 = mesh.GetVertexPosition(Vertex2);
                output.Position = v1;
                output.Direction = v2 - v1;
            }
        }

        /// <summary>
        /// Note: This is invalid after the mesh changes!
        /// </summary>
        public struct Face
        {
            private int m_faceIndex;
            private MyWingedEdgeMesh m_mesh;

            public Face(MyWingedEdgeMesh mesh, int index)
            {
                Debug.Assert(mesh.m_faceTable.Count > index, "Index overflow when creating MyWingedEdgeMesh.Face");
                m_mesh = mesh;
                m_faceIndex = index;
            }

            public FaceEdgeEnumerator GetEnumerator()
            {
                return new FaceEdgeEnumerator(m_mesh, m_faceIndex);
            }

            public FaceVertexEnumerator GetVertexEnumerator()
            {
                return new FaceVertexEnumerator(m_mesh, m_faceIndex);
            }

            public T GetUserData<T>() where T : class
            {
                return m_mesh.GetFaceEntry(m_faceIndex).UserData as T;
            }
        }

        /// <summary>
        /// Note: This is invalid after the mesh changes!
        /// </summary>
        public struct VertexEdgeEnumerator
        {
            private int m_vertexIndex;
            private int m_startingEdge;
            private int m_currentEdgeIndex;
            private MyWingedEdgeMesh m_mesh;
            private Edge m_currentEdge;

            public int CurrentIndex
            {
                get
                {
                    return m_currentEdgeIndex;
                }
            }

            public Edge Current
            {
                get
                {
                    return m_currentEdge;
                }
            }

            public VertexEdgeEnumerator(MyWingedEdgeMesh mesh, int index)
            {
                Debug.Assert(mesh.m_vertexTable.Count > index, "Index overflow when creating MyWingedEdgeMesh.VertexFaceEnumerator");
                m_vertexIndex = index;
                var vEntry = mesh.GetVertexEntry(m_vertexIndex);
                m_startingEdge = vEntry.IncidentEdge;
                m_mesh = mesh;
                m_currentEdgeIndex = INVALID_INDEX;
                m_currentEdge = new Edge();
            }

            public bool MoveNext()
            {
                if (m_currentEdgeIndex == INVALID_INDEX)
                {
                    m_currentEdgeIndex = m_startingEdge;
                    m_currentEdge = m_mesh.GetEdge(m_startingEdge);
                    return true;
                }

                int nextEdge = m_currentEdge.GetNextVertexEdge(m_vertexIndex);
                if (nextEdge == m_startingEdge) return false;

                m_currentEdgeIndex = nextEdge;
                m_currentEdge = m_mesh.GetEdge(m_currentEdgeIndex);
                return true;
            }
        }

        /// <summary>
        /// Note: This is invalid after the mesh changes!
        /// </summary>
        public struct FaceEdgeEnumerator
        {
            private MyWingedEdgeMesh m_mesh;
            private int m_faceIndex;
            private int m_currentEdge;
            private int m_startingEdge;

            public int Current { get { return m_currentEdge; } }

            public FaceEdgeEnumerator(MyWingedEdgeMesh mesh, int faceIndex)
            {
                m_mesh         = mesh;
                m_faceIndex    = faceIndex;
                m_currentEdge  = INVALID_INDEX;
                m_startingEdge = m_mesh.GetFaceEntry(faceIndex).IncidentEdge;
            }

            public bool MoveNext()
            {
                if (m_currentEdge == INVALID_INDEX)
                {
                    m_currentEdge = m_startingEdge;
                    Debug.Assert(m_currentEdge != -1, "Inconsistent navmesh. Call Cestmir!");
                    return true;
                }

                m_currentEdge = m_mesh.GetEdgeEntry(m_currentEdge).FaceSucc(m_faceIndex);
                Debug.Assert(m_currentEdge != -1, "Inconsistent navmesh. Call Cestmir!");
                return (m_currentEdge != m_startingEdge);
            }
        }

        /// <summary>
        /// Note: This is invalid after the mesh changes!
        /// </summary>
        public struct FaceVertexEnumerator
        {
            private MyWingedEdgeMesh m_mesh;
            private int m_faceIndex;
            private int m_currentEdge;
            private int m_startingEdge;

            public Vector3 Current
            {
                get
                {
                    var edgeEntry = m_mesh.GetEdgeEntry(m_currentEdge);
                    if (m_faceIndex == edgeEntry.LeftFace)
                    {
                        return m_mesh.m_vertexTable[edgeEntry.Vertex2].VertexPosition;
                    }
                    else
                    {
                        Debug.Assert(m_faceIndex == edgeEntry.RightFace);
                        return m_mesh.m_vertexTable[edgeEntry.Vertex1].VertexPosition;
                    }
                }
            }

            public int CurrentIndex
            {
                get
                {
                    var edgeEntry = m_mesh.GetEdgeEntry(m_currentEdge);
                    if (m_faceIndex == edgeEntry.LeftFace)
                    {
                        return edgeEntry.Vertex2;
                    }
                    else
                    {
                        Debug.Assert(m_faceIndex == edgeEntry.RightFace);
                        return edgeEntry.Vertex1;
                    }
                }
            }

            public FaceVertexEnumerator(MyWingedEdgeMesh mesh, int faceIndex)
            {
                m_mesh         = mesh;
                m_faceIndex    = faceIndex;
                m_currentEdge  = INVALID_INDEX;
                m_startingEdge = m_mesh.GetFaceEntry(faceIndex).IncidentEdge;
            }

            public bool MoveNext()
            {
                if (m_currentEdge == INVALID_INDEX)
                {
                    m_currentEdge = m_startingEdge;
                    return true;
                }

                m_currentEdge = m_mesh.GetEdgeEntry(m_currentEdge).FaceSucc(m_faceIndex);
                return (m_currentEdge != m_startingEdge);
            }
        }

        /// <summary>
        /// Note: This is invalid after the mesh changes!
        /// </summary>
        public struct EdgeEnumerator
        {
            private int m_currentEdge;
            private HashSet<int> m_freeEdges;
            private MyWingedEdgeMesh m_mesh;

            public EdgeEnumerator(MyWingedEdgeMesh mesh, HashSet<int> preallocatedHelperHashSet = null)
            {
                m_currentEdge = -1;
                m_freeEdges = preallocatedHelperHashSet ?? new HashSet<int>();
                m_mesh = mesh;
                
                m_freeEdges.Clear();

                int freeEdge = mesh.m_freeEdges;
                while (freeEdge != INVALID_INDEX)
                {
                    m_freeEdges.Add(freeEdge);
                    freeEdge = m_mesh.m_edgeTable[freeEdge].NextFreeEntry;
                }
            }

            public int CurrentIndex
            {
                get
                {
                    return m_currentEdge;
                }
            }

            public Edge Current
            {
                get
                {
                    return new Edge(m_mesh, m_currentEdge);
                }
            }

            public bool MoveNext()
            {
                int edgeCount = m_mesh.m_edgeTable.Count;
                do
                {
                    m_currentEdge++;
                }
                while (m_freeEdges.Contains(m_currentEdge) && m_currentEdge < edgeCount);

                return m_currentEdge < edgeCount;
            }

            public void Dispose()
            {
                m_freeEdges.Clear();
                m_freeEdges = null;
            }
        }

        private List<EdgeTableEntry> m_edgeTable;
        private List<VertexTableEntry> m_vertexTable;
        private List<FaceTableEntry> m_faceTable;
        private int m_freeEdges;
        private int m_freeVertices;
        private int m_freeFaces;

        private static HashSet<int> m_debugDrawEdges = null;
        public static void DebugDrawEdgesReset()
        {
            if (m_debugDrawEdges == null)
            {
                m_debugDrawEdges = new HashSet<int>();
            }
            else if (m_debugDrawEdges.Count == 0)
            {
                m_debugDrawEdges = null;
            }
            else
            {
                m_debugDrawEdges.Clear();
            }
        }

        public static void DebugDrawEdgesAdd(int edgeIndex)
        {
            if (m_debugDrawEdges == null) return;
            m_debugDrawEdges.Add(edgeIndex);
        }

        private EdgeTableEntry GetEdgeEntry(int index)
        {
            CheckEdgeIndexValid(index);
            return m_edgeTable[index];
        }

        private void SetEdgeEntry(int index, ref EdgeTableEntry entry)
        {
            CheckEdgeIndexValid(index);
            m_edgeTable[index] = entry;
        }

        private FaceTableEntry GetFaceEntry(int index)
        {
            CheckFaceIndexValid(index);
            return m_faceTable[index];
        }

        private void SetFaceEntry(int index, FaceTableEntry entry)
        {
            CheckFaceIndexValid(index);
            m_faceTable[index] = entry;
        }

        private VertexTableEntry GetVertexEntry(int index)
        {
            CheckVertexIndexValid(index);
            return m_vertexTable[index];
        }

        public MyWingedEdgeMesh()
        {
            m_edgeTable    = new List<EdgeTableEntry>();
            m_vertexTable  = new List<VertexTableEntry>();
            m_faceTable    = new List<FaceTableEntry>();
            m_freeEdges    = INVALID_INDEX;
            m_freeVertices = INVALID_INDEX;
            m_freeFaces    = INVALID_INDEX;
        }

        /// <summary>
        /// For testing purposes only! The copy is only a shallow copy (i.e. userdata is not copied)
        /// </summary>
        public MyWingedEdgeMesh Copy()
        {
            MyWingedEdgeMesh copy = new MyWingedEdgeMesh();
            copy.m_freeEdges      = m_freeEdges;
            copy.m_freeFaces      = m_freeFaces;
            copy.m_freeVertices   = m_freeVertices;
            copy.m_edgeTable      = m_edgeTable.ToList();
            copy.m_vertexTable    = m_vertexTable.ToList();
            copy.m_faceTable      = m_faceTable.ToList();
            return copy;
        }

        public void Transform(Matrix transformation)
        {
            m_tmpFreeVertices.Clear();
            int i = m_freeVertices;
            while (i != INVALID_INDEX)
            {
                m_tmpFreeVertices.Add(i);
                i = m_vertexTable[i].NextFreeEntry;
            }

            for (i = 0; i < m_vertexTable.Count; ++i)
            {
                if (m_tmpFreeVertices.Contains(i)) continue;

                VertexTableEntry entry = m_vertexTable[i];
                Vector3.Transform(ref entry.VertexPosition, ref transformation, out entry.VertexPosition);
                m_vertexTable[i] = entry;
            }
        }

        public Edge GetEdge(int edgeIndex)
        {
            return new Edge(this, edgeIndex);
        }

        public EdgeEnumerator GetEdges(HashSet<int> preallocatedHelperHashset = null)
        {
            return new EdgeEnumerator(this, preallocatedHelperHashset);
        }

        public Face GetFace(int faceIndex)
        {
            return new Face(this, faceIndex);
        }

        public VertexEdgeEnumerator GetVertexEdges(int vertexIndex)
        {
            return new VertexEdgeEnumerator(this, vertexIndex);
        }

        public Vector3 GetVertexPosition(int vertexIndex)
        {
            return m_vertexTable[vertexIndex].VertexPosition;
        }

        /// <summary>
        /// Creates a new face by closing the gap between vertices vert1 and vert2 by a new edge
        /// </summary>
        /// <param name="vert1">Point that will be shared by the new edge and edge1</param>
        /// <param name="vert2">Point that will be shared by the new edge and edge2</param>
        /// <param name="edge1">Predecessor of the new edge</param>
        /// <param name="edge2">Successor of the new edge</param>
        /// <param name="faceUserData">User data for the newly created face</param>
        public int MakeEdgeFace(int vert1, int vert2, int edge1, int edge2, object faceUserData, out int newEdge)
        {
            Debug.Assert(vert1 != vert2, "Making edge between one vertex!");
            Debug.Assert(edge1 != edge2, "Creating a two-edged face!");

            newEdge  = AllocateEdge();
            int face = AllocateAndInsertFace(faceUserData, newEdge);

            EdgeTableEntry edge1Entry = GetEdgeEntry(edge1);
            EdgeTableEntry edge2Entry = GetEdgeEntry(edge2);

            // Set face and vertex of the new edge
            EdgeTableEntry newEdgeEntry = new EdgeTableEntry(); newEdgeEntry.Init();
            newEdgeEntry.Vertex1        = vert1;
            newEdgeEntry.Vertex2        = vert2;
            newEdgeEntry.RightFace      = face;

            // Add face to edge 1 and 2
            edge1Entry.AddFace(face);
            edge2Entry.AddFace(face);

            // Add face to the remaining edges
            int itVert = edge2Entry.OtherVertex(vert2);
            int itEdge = edge2Entry.VertexSucc(itVert);
            while (itEdge != edge1)
            {
                EdgeTableEntry itEntry = GetEdgeEntry(itEdge);
                itEntry.AddFace(face);
                SetEdgeEntry(itEdge, ref itEntry);

                itVert = itEntry.OtherVertex(itVert);
                itEdge = itEntry.VertexSucc(itVert);
            }

            // Connect the new edge with edges 1 and 2 inside the face
            newEdgeEntry.SetVertexSucc(vert2, edge2);
            int e2OldPred = edge2Entry.SetVertexPred(vert2, newEdge);
            newEdgeEntry.SetVertexPred(vert1, edge1);
            int e1OldSucc = edge1Entry.SetVertexSucc(vert1, newEdge);

            // Connect the previous neighbors of edges 1 and 2 to the new edge
            EdgeTableEntry e1OldSuccEntry = GetEdgeEntry(e1OldSucc);
            EdgeTableEntry e2OldPredEntry = default(EdgeTableEntry);
            // Actually, those neighbors could be the same edge...
            if (e1OldSucc != e2OldPred)
                e2OldPredEntry = GetEdgeEntry(e2OldPred);

            e1OldSuccEntry.SetVertexPred(vert1, newEdge);
            newEdgeEntry.SetVertexSucc(vert1, e1OldSucc);
            if (e1OldSucc != e2OldPred)
                e2OldPredEntry.SetVertexSucc(vert2, newEdge);
            else
                e1OldSuccEntry.SetVertexSucc(vert2, newEdge);

            newEdgeEntry.SetVertexPred(vert2, e2OldPred);

            // Finally, save all the new connections into the edge table
            SetEdgeEntry(e1OldSucc, ref e1OldSuccEntry);
            if (e1OldSucc != e2OldPred)
                SetEdgeEntry(e2OldPred, ref e2OldPredEntry);
            SetEdgeEntry(newEdge, ref newEdgeEntry);
            SetEdgeEntry(edge1, ref edge1Entry);
            SetEdgeEntry(edge2, ref edge2Entry);

            return face;
        }

        /// <summary>
        /// Merges two edges together into one. These edges have to border on the edge of the mesh (i.e. face -1)
        /// Note that this also merges the corresponding vertices!
        /// </summary>
        /// <param name="edge1">The edge that will be merged</param>
        /// <param name="edge2">The edge that will be kept</param>
        public void MergeEdges(int edge1, int edge2)
        {
            Debug.Assert(edge1 != edge2, "Merging the edge with itself!");

            // Load the data from the old edges
            EdgeTableEntry entry1 = GetEdgeEntry(edge1);
            EdgeTableEntry entry2 = GetEdgeEntry(edge2);

            int e1vp, e1vs, e2vp, e2vs;
            entry1.GetFaceVertices(INVALID_INDEX, out e1vp, out e1vs);
            entry2.GetFaceVertices(INVALID_INDEX, out e2vp, out e2vs);
            int e1f = entry1.OtherFace(INVALID_INDEX);

            int succ1 = entry1.FaceSucc(INVALID_INDEX);
            int pred1 = entry1.FacePred(INVALID_INDEX);
            int succ2 = entry2.FaceSucc(INVALID_INDEX);
            int pred2 = entry2.FacePred(INVALID_INDEX);
            int succ1Inner = entry1.FaceSucc(e1f);
            int pred1Inner = entry1.FacePred(e1f);

            // Get the neighboring edges
            EdgeTableEntry entrySucc1 = GetEdgeEntry(succ1);
            EdgeTableEntry entryPred1 = default(EdgeTableEntry);
            if (succ1 != pred1) // an edge could be our predecessor and successor at the same time (in case of a two-edged loop)
                entryPred1 = GetEdgeEntry(pred1);
            EdgeTableEntry entrySucc2 = GetEdgeEntry(succ2);
            EdgeTableEntry entryPred2 = default(EdgeTableEntry);
            if (succ2 != pred2) // an edge could be our predecessor and successor at the same time (in case of a two-edged loop)
                entryPred2 = GetEdgeEntry(pred2);

            // Connect them with edge 1 and 2 outside of the two faces
            entrySucc1.SetFacePred(INVALID_INDEX, pred2);
            entrySucc2.SetFacePred(INVALID_INDEX, pred1);
            if (succ1 != pred1)
                entryPred1.SetFaceSucc(INVALID_INDEX, succ2);
            else
                entrySucc1.SetFaceSucc(INVALID_INDEX, succ2);
            if (succ2 != pred2)
                entryPred2.SetFaceSucc(INVALID_INDEX, succ1);
            else
                entrySucc2.SetFaceSucc(INVALID_INDEX, succ1);


            // Merge edge 1 with edge 2 and set the indices accordingly
            entry2.AddFace(e1f);
            entry2.SetFacePred(e1f, pred1Inner);
            entry2.SetFaceSucc(e1f, succ1Inner);
            entrySucc1.SetFacePredVertex(INVALID_INDEX, e2vp);
            if (succ1 != pred1)
                entryPred1.SetFaceSuccVertex(INVALID_INDEX, e2vs);
            else
                entrySucc1.SetFaceSuccVertex(INVALID_INDEX, e2vs);
            if (pred1Inner == succ1)
            {
                entrySucc1.SetFaceSucc(e1f, edge2);
            }
            else
            {
                // The predecessor of edge 1 in the inner face is different from the successor in the outer (-1) edge 
                // We have to update its index to the edge1 and also update all vertex indices that point to the e1vs
                EdgeTableEntry entryPred1Inner = GetEdgeEntry(pred1Inner);
                entryPred1Inner.SetFaceSucc(e1f, edge2);
                entryPred1Inner.SetFaceSuccVertex(e1f, e2vp);
                SetEdgeEntry(pred1Inner, ref entryPred1Inner);

                pred1Inner = entryPred1Inner.VertexPred(e2vp);
                while (pred1Inner != succ1)
                {
                    entryPred1Inner = GetEdgeEntry(pred1Inner);
                    entryPred1Inner.ChangeVertex(e1vs, e2vp);
                    SetEdgeEntry(pred1Inner, ref entryPred1Inner);
                    pred1Inner = entryPred1Inner.VertexPred(e2vp);
                }
            }
            if (succ1Inner == pred1)
            {
                if (succ1 != pred1)
                    entryPred1.SetFacePred(e1f, edge2);
                else
                    entrySucc1.SetFacePred(e1f, edge2);
            }
            else
            {
                EdgeTableEntry entrySucc1Inner = GetEdgeEntry(succ1Inner);
                entrySucc1Inner.SetFacePred(e1f, edge2);
                entrySucc1Inner.SetFacePredVertex(e1f, e2vs);
                SetEdgeEntry(succ1Inner, ref entrySucc1Inner);

                succ1Inner = entrySucc1Inner.VertexSucc(e2vs);
                while (succ1Inner != pred1)
                {
                    entrySucc1Inner = GetEdgeEntry(succ1Inner);
                    entrySucc1Inner.ChangeVertex(e1vp, e2vs);
                    SetEdgeEntry(succ1Inner, ref entrySucc1Inner);
                    succ1Inner = entrySucc1Inner.VertexSucc(e2vs);
                }
            }

            // Update the face entry to avoid removing its referenced edge
            FaceTableEntry entryE1f = GetFaceEntry(e1f);
            entryE1f.IncidentEdge = edge2;
            SetFaceEntry(e1f, entryE1f);

            // Remove the merged edge and vertices
            DeallocateVertex(e1vp);
            DeallocateVertex(e1vs);
            DeallocateEdge(edge1);

            // Update the edge table
            SetEdgeEntry(edge2, ref entry2);
            SetEdgeEntry(succ1, ref entrySucc1);
            if (succ1 != pred1)
                SetEdgeEntry(pred1, ref entryPred1);
            SetEdgeEntry(succ2, ref entrySucc2);
            if (succ2 != pred2)
                SetEdgeEntry(pred2, ref entryPred2);
        }

        /// <summary>
        /// Creates a new triangle by adding a vertex to an existing edge
        /// </summary>
        /// <param name="newVertex">Position of the new vertex</param>
        /// <param name="edge">The edge from which we want to extrude</param>
        /// <param name="faceUserData">User data that will be saved in the face</param>
        /// <param name="newEdgeS">Index of the new edge that follows edge "edge" in the new triangle.</param>
        /// <param name="newEdgeP">Index of the new edge that precedes edge "edge" in the new triangle.</param>
        /// <returns></returns>
        public int ExtrudeTriangleFromEdge(ref Vector3 newVertex, int edge, object faceUserData, out int newEdgeS, out int newEdgeP)
        {
            EdgeTableEntry edgeEntry = GetEdgeEntry(edge);
            Debug.Assert(edgeEntry.LeftFace == INVALID_INDEX || edgeEntry.RightFace == INVALID_INDEX, "The face to be extruded is surrounded by faces on both sides!");

            newEdgeP = AllocateEdge();
            newEdgeS = AllocateEdge();
            int edgeP = edgeEntry.FacePred(INVALID_INDEX);
            int edgeS = edgeEntry.FaceSucc(INVALID_INDEX);
            int vertP, vertS;
            edgeEntry.GetFaceVertices(INVALID_INDEX, out vertP, out vertS);

            EdgeTableEntry edgePEntry = GetEdgeEntry(edgeP);
            EdgeTableEntry edgeSEntry = GetEdgeEntry(edgeS);
            EdgeTableEntry newEdgePEntry = new EdgeTableEntry(); newEdgePEntry.Init();
            EdgeTableEntry newEdgeSEntry = new EdgeTableEntry(); newEdgeSEntry.Init();

            int newFace = AllocateAndInsertFace(faceUserData, newEdgeP);
            int newVert = AllocateAndInsertVertex(ref newVertex, newEdgeP);

            newEdgePEntry.AddFace(newFace);
            newEdgePEntry.SetFacePredVertex(newFace, newVert);
            newEdgePEntry.SetFacePred(newFace, newEdgeS);
            newEdgePEntry.SetFaceSuccVertex(newFace, vertP);
            newEdgePEntry.SetFaceSucc(newFace, edge);
            newEdgePEntry.SetFacePred(INVALID_INDEX, edgeP);
            newEdgePEntry.SetFaceSucc(INVALID_INDEX, newEdgeS);

            newEdgeSEntry.AddFace(newFace);
            newEdgeSEntry.SetFacePredVertex(newFace, vertS);
            newEdgeSEntry.SetFacePred(newFace, edge);
            newEdgeSEntry.SetFaceSuccVertex(newFace, newVert);
            newEdgeSEntry.SetFaceSucc(newFace, newEdgeP);
            newEdgeSEntry.SetFacePred(INVALID_INDEX, newEdgeP);
            newEdgeSEntry.SetFaceSucc(INVALID_INDEX, edgeS);

            edgeEntry.AddFace(newFace);
            edgeEntry.SetFacePred(newFace, newEdgeP);
            edgeEntry.SetFaceSucc(newFace, newEdgeS);

            edgePEntry.SetFaceSucc(INVALID_INDEX, newEdgeP);
            edgeSEntry.SetFacePred(INVALID_INDEX, newEdgeS);

            SetEdgeEntry(newEdgeP, ref newEdgePEntry);
            SetEdgeEntry(newEdgeS, ref newEdgeSEntry);
            SetEdgeEntry(edgeP, ref edgePEntry);
            SetEdgeEntry(edgeS, ref edgeSEntry);
            SetEdgeEntry(edge, ref edgeEntry);
            return newFace;
        }

        public void MergeAngle(int leftEdge, int rightEdge, int commonVert)
        {
            Debug.Assert(leftEdge != rightEdge, "Merging angle between the same edge!");

            EdgeTableEntry leftEntry = GetEdgeEntry(leftEdge);
            EdgeTableEntry rightEntry = GetEdgeEntry(rightEdge);

            Debug.Assert(leftEntry.Vertex1 == commonVert || leftEntry.Vertex2 == commonVert, "Common vertex is not contained in the left edge!");
            Debug.Assert(rightEntry.Vertex1 == commonVert || rightEntry.Vertex2 == commonVert, "Common vertex is not contained in the right edge!");
            Debug.Assert(leftEntry.OtherFace(INVALID_INDEX) != rightEntry.OtherFace(INVALID_INDEX), "Cannot merge angle outside a single face!");

            // Get needed indices
            int leftSucc      = leftEntry.FaceSucc(INVALID_INDEX);
            int rightPred     = rightEntry.FacePred(INVALID_INDEX);
            int leftVert      = leftEntry.OtherVertex(commonVert);
            int rightVert     = rightEntry.OtherVertex(commonVert);
            int leftFace      = leftEntry.OtherFace(INVALID_INDEX);
            int leftInnerSucc = leftEntry.FaceSucc(leftFace);
            int leftInnerPred = leftEntry.FacePred(leftFace);

            EdgeTableEntry leftInnerSuccEntry = GetEdgeEntry(leftInnerSucc);
            EdgeTableEntry leftSuccEntry      = GetEdgeEntry(leftSucc);
            EdgeTableEntry rightPredEntry     = GetEdgeEntry(rightPred);

            // Connect the edges together
            leftSuccEntry.SetFacePredVertex(INVALID_INDEX, rightVert);
            leftSuccEntry.SetFacePred(INVALID_INDEX, rightPred);
            rightPredEntry.SetFaceSucc(INVALID_INDEX, leftSucc);

            // There could be some inner edges between the left edge and leftSucc edge, so we have to check for this case
            if (leftInnerPred == leftSucc)
            {
                leftSuccEntry.SetFaceSucc(leftFace, rightEdge);
            }
            else
            {
                EdgeTableEntry innerEdgeEntry = GetEdgeEntry(leftInnerPred);
                innerEdgeEntry.SetFaceSucc(leftFace, rightEdge);
                innerEdgeEntry.ChangeVertex(leftVert, rightVert);
                SetEdgeEntry(leftInnerPred, ref innerEdgeEntry);

                int nextInnerEdge = innerEdgeEntry.VertexPred(rightVert);
                while (nextInnerEdge != leftSucc)
                {
                    innerEdgeEntry = GetEdgeEntry(nextInnerEdge);
                    innerEdgeEntry.ChangeVertex(leftVert, rightVert);
                    SetEdgeEntry(nextInnerEdge, ref innerEdgeEntry);
                    nextInnerEdge = innerEdgeEntry.VertexPred(rightVert);
                }
            }

            rightEntry.AddFace(leftFace);
            rightEntry.SetFacePred(leftFace, leftInnerPred);

            rightEntry.SetFaceSucc(leftFace, leftInnerSucc);
            leftInnerSuccEntry.SetFacePred(leftFace, rightEdge);

            // Change incident edge of the left edge's inner face and common vertex in case the left edge was their incident edge
            VertexTableEntry commonVertexEntry = m_vertexTable[commonVert];
            commonVertexEntry.IncidentEdge     = rightEdge;
            m_vertexTable[commonVert]          = commonVertexEntry;

            FaceTableEntry leftFaceEntry = GetFaceEntry(leftFace);
            leftFaceEntry.IncidentEdge = rightEdge;
            SetFaceEntry(leftFace, leftFaceEntry);

            // Now we can safely remove left edge (and left vertex)
            DeallocateEdge(leftEdge);
            DeallocateVertex(leftVert);

            // Save everything into the tables
            SetEdgeEntry(rightEdge, ref rightEntry);
            SetEdgeEntry(leftSucc, ref leftSuccEntry);
            SetEdgeEntry(leftInnerSucc, ref leftInnerSuccEntry);
            SetEdgeEntry(rightPred, ref rightPredEntry);
        }

        /// <summary>
        /// Makes a face by filling in the empty edge loop incident to incidentEdge
        /// </summary>
        /// <param name="userData"></param>
        /// <param name="incidentEdge"></param>
        /// <returns></returns>
        public int MakeFace(object userData, int incidentEdge)
        {
            int face = AllocateAndInsertFace(userData, incidentEdge);

            // All indices will remain the same, we just have to update face indices on the edge loop in the previously empty face
            int edge = incidentEdge;
            do
            {
                EdgeTableEntry edgeEntry = GetEdgeEntry(edge);
                Debug.Assert(edgeEntry.FacePred(-1) != edgeEntry.FaceSucc(-1), "Closing a two-edged face! Only empty faces can have two edges!");
                edgeEntry.AddFace(face);
                SetEdgeEntry(edge, ref edgeEntry);

                edge = edgeEntry.FaceSucc(face);
            } while (edge != incidentEdge);

            return face;
        }

        public int MakeNewTriangle(object userData, ref Vector3 A, ref Vector3 B, ref Vector3 C, out int edgeAB, out int edgeBC, out int edgeCA)
        {
            edgeAB = AllocateEdge();
            edgeBC = AllocateEdge();
            edgeCA = AllocateEdge();
            int vertA = AllocateAndInsertVertex(ref A, edgeAB);
            int vertB = AllocateAndInsertVertex(ref B, edgeBC);
            int vertC = AllocateAndInsertVertex(ref C, edgeCA);
            int face = AllocateAndInsertFace(userData, edgeAB);

            EdgeTableEntry teAB = new EdgeTableEntry(); teAB.Init();
            EdgeTableEntry teBC = new EdgeTableEntry(); teBC.Init();
            EdgeTableEntry teCA = new EdgeTableEntry(); teCA.Init();

            teAB.AddFace(face);
            teBC.AddFace(face);
            teCA.AddFace(face);

            teAB.SetFaceSuccVertex(face, vertB);
            teBC.SetFaceSuccVertex(face, vertC);
            teCA.SetFaceSuccVertex(face, vertA);

            teAB.SetFacePredVertex(face, vertA);
            teBC.SetFacePredVertex(face, vertB);
            teCA.SetFacePredVertex(face, vertC);

            teAB.SetFaceSucc(face, edgeBC);
            teBC.SetFaceSucc(face, edgeCA);
            teCA.SetFaceSucc(face, edgeAB);

            teAB.SetFacePred(face, edgeCA);
            teBC.SetFacePred(face, edgeAB);
            teCA.SetFacePred(face, edgeBC);

            teAB.SetFaceSucc(INVALID_INDEX, edgeCA);
            teBC.SetFaceSucc(INVALID_INDEX, edgeAB);
            teCA.SetFaceSucc(INVALID_INDEX, edgeBC);

            teAB.SetFacePred(INVALID_INDEX, edgeBC);
            teBC.SetFacePred(INVALID_INDEX, edgeCA);
            teCA.SetFacePred(INVALID_INDEX, edgeAB);

            SetEdgeEntry(edgeAB, ref teAB);
            SetEdgeEntry(edgeBC, ref teBC);
            SetEdgeEntry(edgeCA, ref teCA);

            return face;
        }

        public int MakeNewPoly(object userData, List<Vector3> points, List<int> outEdges)
        {
            Debug.Assert(outEdges.Count == 0, "Output list of edge indices was not empty in winged edge mesh!");
            Debug.Assert(points.Count >= 3, "Input list of points has less than three points!");

            if (outEdges.Count != 0 || points.Count < 3) return INVALID_INDEX;

            m_tmpIndexList.Clear();
            Vector3 point;
            int edge = INVALID_INDEX;
            for (int i = 0; i < points.Count; ++i)
            {
                point = points[i];
                edge = AllocateEdge();
                outEdges.Add(edge);
                m_tmpIndexList.Add(AllocateAndInsertVertex(ref point, edge));
            }

            int face = AllocateAndInsertFace(userData, edge);
            
            int succEdge, predEdge, succVert, vert;

            predEdge = outEdges[points.Count - 1];
            edge = outEdges[0];
            vert = m_tmpIndexList[0];

            for (int i = 0; i < points.Count; ++i)
            {
                if (i != points.Count - 1)
                {
                    succEdge = outEdges[i + 1];
                    succVert = m_tmpIndexList[i + 1];
                }
                else
                {
                    succEdge = outEdges[0];
                    succVert = m_tmpIndexList[0];
                }

                EdgeTableEntry entry = new EdgeTableEntry(); entry.Init();
                entry.AddFace(face);
                entry.SetFacePred(face, predEdge);
                entry.SetFaceSucc(face, succEdge);
                entry.SetFacePred(INVALID_INDEX, succEdge);
                entry.SetFaceSucc(INVALID_INDEX, predEdge);
                entry.SetFacePredVertex(face, vert);
                entry.SetFaceSuccVertex(face, succVert);

                SetEdgeEntry(edge, ref entry);

                predEdge = edge;
                edge = succEdge;
                vert = succVert;
            }

            return face;
        }

        public void RemoveFace(int faceIndex)
        {
            FaceTableEntry faceEntry = GetFaceEntry(faceIndex);

            int edge = faceEntry.IncidentEdge;
            int firstEdge = edge;
            bool firstDeallocated = false;
            EdgeTableEntry edgeEntry = GetEdgeEntry(edge);
            EdgeTableEntry firstEntry = GetEdgeEntry(firstEdge); // Save the first entry for the last iteration in the loop

            int v1, v2;
            edgeEntry.GetFaceVertices(faceIndex, out v1, out v2);

            // Start with an edge entry and go clockwise around the face checking for neighboring faces that are already removed
            //
            //               edge     v1
            //          o-------------o
            //          |            /
            //          |  removed  /
            //         ...  face   / nextEdge
            //                    /
            //                   /
            //             ...--o v2
            //
            //
            // According to neighboring faces of the current and next edge, we have four possible scenarios:
            // A) Both current and next edge have empty face as neighbor:
            //        In this case, the shared vertex should be removed, because it means that the vertex has only two faces
            // B) Current edge has empty face, next edge non-empty:
            //        In this case, we are not creating a new empty face, we are just extending the old one.
            // C) Current edge has non-empty face, next edge empty:
            //        Analogous to B - we are just extending an existing empty face.
            // D) Both edges have non-empty neighbors:
            //        This seemingly easy situation has one quirk - there could be another empty face along the shared vertex triangle fan
            //        In this case, we have to split the vertex into two (otherwise, we'd have a vertex with two empty faces in its fan
            //        and that is forbidden).
            do
            {
                int nextEdge = edgeEntry.FaceSucc(faceIndex);
                v1 = v2;
                // If the next edge is the first edge, which could have been deallocated, we use the saved first entry
                EdgeTableEntry nextEntry = nextEdge == firstEdge && firstDeallocated ? firstEntry : GetEdgeEntry(nextEdge);
                v2 = nextEntry.OtherVertex(v2);

                if (edgeEntry.VertexLeftFace(v1) == INVALID_INDEX)
                {
                    if (edge == firstEdge)
                        firstDeallocated = true;
                    DeallocateEdge(edge);
                    // Case A)
                    if (nextEntry.VertexLeftFace(v2) == INVALID_INDEX)
                    {
                        if (nextEntry.VertexSucc(v1) == edge)
                            DeallocateVertex(v1);
                        else
                        {
                            // This should happen only in the case when a vertex has just two faces: the removed one and INVALID_INDEX
                            Debug.Assert(false, "FOOBAR");
                            int nextSucc = nextEntry.VertexSucc(v1);
                            int edgePred = edgeEntry.VertexPred(v1);
                            EdgeTableEntry entryNextSucc = GetEdgeEntry(nextSucc);
                            EdgeTableEntry entryEdgePred = GetEdgeEntry(edgePred);

                            entryNextSucc.SetVertexPred(v1, edgePred);
                            entryEdgePred.SetVertexSucc(v1, nextSucc);
                            SetEdgeEntry(edgePred, ref entryEdgePred);
                            SetEdgeEntry(nextSucc, ref entryNextSucc);

                            VertexTableEntry vertexEntry = m_vertexTable[v1];
                            vertexEntry.IncidentEdge = edgePred;
                            m_vertexTable[v1] = vertexEntry;
                        }
                    }
                    // Case B)
                    else
                    {
                        int lateralEdge = edgeEntry.FacePred(INVALID_INDEX);
                        EdgeTableEntry lateralEntry = GetEdgeEntry(lateralEdge);

                        lateralEntry.SetFaceSucc(INVALID_INDEX, nextEdge);
                        if (nextEdge != firstEdge)
                            nextEntry.SetFacePred(faceIndex, lateralEdge);
                        else
                            nextEntry.SetFacePred(INVALID_INDEX, lateralEdge); // If the next edge is the first edge, it will have an already erased face

                        SetEdgeEntry(lateralEdge, ref lateralEntry);
                        SetEdgeEntry(nextEdge, ref nextEntry);

                        VertexTableEntry vertexEntry = m_vertexTable[v1];
                        vertexEntry.IncidentEdge = nextEdge;
                        m_vertexTable[v1] = vertexEntry;
                    }
                }
                else
                {
                    // Case C)
                    if (nextEntry.VertexLeftFace(v2) == INVALID_INDEX)
                    {
                        int lateralEdge = nextEntry.FaceSucc(INVALID_INDEX);
                        EdgeTableEntry lateralEntry = GetEdgeEntry(lateralEdge);

                        lateralEntry.SetFacePred(INVALID_INDEX, edge);
                        edgeEntry.SetFaceSucc(faceIndex, lateralEdge);

                        edgeEntry.ChangeFace(faceIndex, INVALID_INDEX);

                        SetEdgeEntry(lateralEdge, ref lateralEntry);
                        SetEdgeEntry(edge, ref edgeEntry);

                        VertexTableEntry vertexEntry = m_vertexTable[v1];
                        vertexEntry.IncidentEdge = edge;
                        m_vertexTable[v1] = vertexEntry;
                    }
                    // Case D)
                    else
                    {
                        int edge2 = nextEntry.VertexSucc(v1);
                        while (edge2 != edge)
                        {
                            EdgeTableEntry edge2Entry = GetEdgeEntry(edge2);
                            if (edge2Entry.VertexRightFace(v1) == INVALID_INDEX)
                            {
                                int edge3 = edge2Entry.VertexSucc(v1);
                                Debug.Assert(edge3 != edge, "First edge should not neighbor with empty face in this branch (should be case B) )!");

                                VertexTableEntry v1Entry = m_vertexTable[v1];
                                Vector3 vertPos = v1Entry.VertexPosition;

                                // Fix the old vertex in case its incident edge lies in the newly created fan.
                                v1Entry.IncidentEdge = nextEdge;
                                m_vertexTable[v1] = v1Entry;

                                int newV = AllocateAndInsertVertex(ref vertPos, edge3);
                                
                                EdgeTableEntry edge3Entry = GetEdgeEntry(edge3);
                                edge3Entry.SetVertexPred(v1, edge);
                                edgeEntry.SetVertexSucc(v1, edge3);
                                edgeEntry.ChangeVertex(v1, newV);

                                while (true)
                                {
                                    edge3Entry.ChangeVertex(v1, newV);
                                    SetEdgeEntry(edge3, ref edge3Entry);
                                    edge3 = edge3Entry.VertexSucc(newV);
                                    if (edge3 == edge) break;

                                    edge3Entry = GetEdgeEntry(edge3);
                                }

                                edge2Entry.SetVertexSucc(v1, nextEdge);
                                nextEntry.SetVertexPred(v1, edge2);
                                SetEdgeEntry(edge2, ref edge2Entry);
                                SetEdgeEntry(nextEdge, ref nextEntry);

                                break;
                            }
                            edge2 = edge2Entry.VertexSucc(v1);
                        }

                        edgeEntry.ChangeFace(faceIndex, INVALID_INDEX);
                        SetEdgeEntry(edge, ref edgeEntry);
                    }
                }

                edge = nextEdge;
                edgeEntry = nextEntry;
            } while (edge != firstEdge);

            DeallocateFace(faceIndex);
        }

        public bool IntersectEdge(ref MyWingedEdgeMesh.Edge edge, ref Plane p, out Vector3 intersection)
        {
            intersection = default(Vector3);
            Ray r = default(Ray);
            edge.ToRay(this, ref r);

            float? result = r.Intersects(p);

            if (!result.HasValue) return false;
            float dist = result.Value;

            if (dist < 0.0f || dist > 1.0f) return false;

            intersection = r.Position + dist * r.Direction;
            return true;
        }

        /// <summary>
        /// Sorts the list of free faces. This ensures that subsequent face allocations will return increasing sequence of face indices,
        /// unless interrupted by face deallocation. This can be useful in some algorithms that rely on ordering of the face indices.
        /// </summary>
        public void SortFreeFaces()
        {
            CheckFreeEntryConsistency();
            if (m_freeFaces == INVALID_INDEX)
            {
                return;
            }

            m_tmpIndexList.Clear();

            int freeFaceIndex = m_freeFaces;
            while (freeFaceIndex != INVALID_INDEX)
            {
                m_tmpIndexList.Add(freeFaceIndex);
                freeFaceIndex = m_faceTable[freeFaceIndex].NextFreeEntry;
            }
            m_tmpIndexList.Sort();

            m_freeFaces = m_tmpIndexList[0];
            for (int i = 0; i < m_tmpIndexList.Count - 1; ++i)
            {
                FaceTableEntry entry = m_faceTable[m_tmpIndexList[i]];
                entry.NextFreeEntry = m_tmpIndexList[i + 1];
                m_faceTable[m_tmpIndexList[i]] = entry;
            }
            FaceTableEntry lastEntry = m_faceTable[m_tmpIndexList[m_tmpIndexList.Count - 1]];
            lastEntry.NextFreeEntry = INVALID_INDEX;
            m_faceTable[m_tmpIndexList[m_tmpIndexList.Count - 1]] = lastEntry;

            m_tmpIndexList.Clear();

            CheckFreeEntryConsistency();
        }

        private int AllocateAndInsertFace(object userData, int incidentEdge)
        {
            CheckEdgeIndexValid(incidentEdge);
            CheckFreeEntryConsistency();
            var entry = new FaceTableEntry() { IncidentEdge = incidentEdge, UserData = userData };

            if (m_freeFaces == INVALID_INDEX)
            {
                int index = m_faceTable.Count;
                m_faceTable.Add(entry);
                CheckFreeEntryConsistency();
                return index;
            }
            else
            {
                int index = m_freeFaces;
                m_freeFaces = m_faceTable[m_freeFaces].NextFreeEntry;
                m_faceTable[index] = entry;
                CheckFreeEntryConsistency();
                return index;
            }
        }

        private int AllocateAndInsertVertex(ref Vector3 position, int incidentEdge)
        {
            CheckEdgeIndexValid(incidentEdge);
            CheckFreeEntryConsistency();
            var entry = new VertexTableEntry() { IncidentEdge = incidentEdge, VertexPosition = position };

            if (m_freeVertices == INVALID_INDEX)
            {
                int index = m_vertexTable.Count;
                m_vertexTable.Add(entry);
                CheckFreeEntryConsistency();
                return index;
            }
            else
            {
                int index = m_freeVertices;

                // CH: TODO: This should not be needed. It's here only to avoid a nasty crash
                if (index < 0 || index >= m_vertexTable.Count)
                {
                    m_freeVertices = -1;
                    return AllocateAndInsertVertex(ref position, incidentEdge);
                }

                m_freeVertices = m_vertexTable[index].NextFreeEntry;
                m_vertexTable[index] = entry;
                CheckFreeEntryConsistency();
                return index;
            }
        }

        private int AllocateEdge()
        {
            CheckFreeEntryConsistency();
            EdgeTableEntry tableEntry = new EdgeTableEntry(); tableEntry.Init();
            if (m_freeEdges == INVALID_INDEX)
            {
                int index = m_edgeTable.Count;
                m_edgeTable.Add(tableEntry);
                CheckFreeEntryConsistency();
                return index;
            }
            else
            {
                int index = m_freeEdges;
                m_freeEdges = m_edgeTable[m_freeEdges].NextFreeEntry;
                m_edgeTable[index] = tableEntry;
                CheckFreeEntryConsistency();
                return index;
            }
        }

        private void DeallocateFace(int faceIndex)
        {
            CheckFaceIndexValid(faceIndex);
            CheckFreeEntryConsistency();

            FaceTableEntry entry   = new FaceTableEntry();
            entry.NextFreeEntry    = m_freeFaces;
            m_faceTable[faceIndex] = entry;
            m_freeFaces            = faceIndex;

            CheckFreeEntryConsistency();
        }

        private void DeallocateVertex(int vertexIndex)
        {
            Debug.Assert(vertexIndex < m_vertexTable.Count);
            CheckFreeEntryConsistency();

            VertexTableEntry entry     = new VertexTableEntry();
            entry.NextFreeEntry        = m_freeVertices;
            m_vertexTable[vertexIndex] = entry;
            m_freeVertices             = vertexIndex;

            CheckFreeEntryConsistency();
        }

        private void DeallocateEdge(int edgeIndex)
        {
            CheckEdgeIndexValid(edgeIndex);
            CheckFreeEntryConsistency();

            EdgeTableEntry entry   = new EdgeTableEntry();
            entry.NextFreeEntry    = m_freeEdges;
            m_edgeTable[edgeIndex] = entry;
            m_freeEdges            = edgeIndex;

            CheckFreeEntryConsistency();
        }

        public void DebugDraw(ref Matrix drawMatrix, MyWEMDebugDrawMode draw)
        {
            m_tmpDebugDrawFreeIndices.Clear();
            int i = m_freeEdges;
            while (i != INVALID_INDEX)
            {
                m_tmpDebugDrawFreeIndices.Add(i);
                i = m_edgeTable[i].NextFreeEntry;
            }

            for (int j = 0; j < m_edgeTable.Count; ++j)
            {
                if (m_tmpDebugDrawFreeIndices.Contains(j)) continue;

                if (m_debugDrawEdges != null && !m_debugDrawEdges.Contains(j)) continue;

                var entry = GetEdgeEntry(j);
                Vector3 V1 = Vector3.Transform(m_vertexTable[entry.Vertex1].VertexPosition, drawMatrix);
                Vector3 V2 = Vector3.Transform(m_vertexTable[entry.Vertex2].VertexPosition, drawMatrix);
                Vector3 C = (V1+V2) * 0.5f;

                /*if (draw.HasFlag(MyWEMDebugDrawMode.EDGES))
                {
                    V1 = V1 + (V2 - V1) * 0.1f;
                    V2 = V2 + (V1 - V2) * 0.1f;
                }*/

                /*V1 += Vector3.Up * 0.1f;
                V2 += Vector3.Up * 0.1f;*/

                var entry1Left = GetEdgeEntry(entry.LeftSucc);
                var entry1Right = GetEdgeEntry(entry.RightPred);
                var entry2Left = GetEdgeEntry(entry.LeftPred);
                var entry2Right = GetEdgeEntry(entry.RightSucc);

                Vector3 V1L = Vector3.Transform(m_vertexTable[entry1Left.OtherVertex(entry.Vertex1)].VertexPosition, drawMatrix);
                Vector3 V1R = Vector3.Transform(m_vertexTable[entry1Right.OtherVertex(entry.Vertex1)].VertexPosition, drawMatrix);
                Vector3 V2L = Vector3.Transform(m_vertexTable[entry2Left.OtherVertex(entry.Vertex2)].VertexPosition, drawMatrix);
                Vector3 V2R = Vector3.Transform(m_vertexTable[entry2Right.OtherVertex(entry.Vertex2)].VertexPosition, drawMatrix);

                if ((draw & MyWEMDebugDrawMode.LINES) != 0 || (draw & MyWEMDebugDrawMode.EDGES) != 0)
                {
                    bool edgeOfTheMesh = entry.LeftFace == INVALID_INDEX || entry.RightFace == INVALID_INDEX;
                    Color c1 = (draw & MyWEMDebugDrawMode.LINES) != 0 ? (edgeOfTheMesh ? Color.Red : Color.DarkSlateBlue) : Color.Black;
                    Color c2 = (draw & MyWEMDebugDrawMode.LINES) != 0 ? (edgeOfTheMesh ? Color.Red : Color.DarkSlateBlue) : Color.White;
                    MyRenderProxy.DebugDrawLine3D(V1, V2, c1, c2, (draw & MyWEMDebugDrawMode.LINES_DEPTH) != 0);
                }

                if ((draw & MyWEMDebugDrawMode.EDGES) != 0)
                {
                    if (entry.RightFace == INVALID_INDEX)
                    {
                        Vector3 axis = (V2 - V1);
                        axis.Normalize();
                        V2R = V2L - axis * Vector3.Dot(V2L - V2, axis);
                        V2R = V2 + (V2 - V2R);
                        V1R = V1L - axis * Vector3.Dot(V1L - V1, axis);
                        V1R = V1 + (V1 - V1R);
                    }
                    if (entry.LeftFace == INVALID_INDEX)
                    {
                        Vector3 axis = (V1 - V2);
                        axis.Normalize();
                        V2L = V2R - axis * Vector3.Dot(V2R - V2, axis);
                        V2L = V2 + (V2 - V2L);
                        V1L = V1R - axis * Vector3.Dot(V1R - V1, axis);
                        V1L = V1 + (V1 - V1L);
                    }

                    V1L = (V1 * 0.8f + V1L * 0.2f) * 0.5f + C * 0.5f;
                    V1R = (V1 * 0.8f + V1R * 0.2f) * 0.5f + C * 0.5f;
                    V2L = (V2 * 0.8f + V2L * 0.2f) * 0.5f + C * 0.5f;
                    V2R = (V2 * 0.8f + V2R * 0.2f) * 0.5f + C * 0.5f;

                    MyRenderProxy.DebugDrawLine3D(C, V1L, Color.Black, Color.Gray, false);
                    MyRenderProxy.DebugDrawLine3D(C, V1R, Color.Black, Color.Gray, false);
                    MyRenderProxy.DebugDrawLine3D(C, V2L, Color.Black, Color.Gray, false);
                    MyRenderProxy.DebugDrawLine3D(C, V2R, Color.Black, Color.Gray, false);

                    MyRenderProxy.DebugDrawLine3D(C, (V2R + V1R) * 0.5f, Color.Black, Color.Gray, false);
                    MyRenderProxy.DebugDrawLine3D(C, (V2L + V1L) * 0.5f, Color.Black, Color.Gray, false);

                    MyRenderProxy.DebugDrawText3D(C, j.ToString(), Color.Yellow, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    MyRenderProxy.DebugDrawText3D(V1L, entry.LeftSucc.ToString(), Color.LightYellow, 0.4f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    MyRenderProxy.DebugDrawText3D(V1R, entry.RightPred.ToString(), Color.LightYellow, 0.4f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    MyRenderProxy.DebugDrawText3D(V2L, entry.LeftPred.ToString(), Color.LightYellow, 0.4f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    MyRenderProxy.DebugDrawText3D(V2R, entry.RightSucc.ToString(), Color.LightYellow, 0.4f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                    if (entry.RightFace != INVALID_INDEX)
                        MyRenderProxy.DebugDrawText3D((V2R + V1R) * 0.5f, entry.RightFace.ToString(), Color.LightBlue, 0.4f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    else
                        MyRenderProxy.DebugDrawText3D((V2R + V1R) * 0.5f, entry.RightFace.ToString(), Color.Red, 0.8f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    if (entry.LeftFace != INVALID_INDEX)
                        MyRenderProxy.DebugDrawText3D((V2L + V1L) * 0.5f, entry.LeftFace.ToString(), Color.LightBlue, 0.4f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    else
                        MyRenderProxy.DebugDrawText3D((V2L + V1L) * 0.5f, entry.LeftFace.ToString(), Color.Red, 0.8f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                    MyRenderProxy.DebugDrawText3D(V1 * 0.05f + V2 * 0.95f, entry.Vertex2.ToString(), Color.LightGreen, 0.4f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    MyRenderProxy.DebugDrawText3D(V1 * 0.95f + V2 * 0.05f, entry.Vertex1.ToString(), Color.LightGreen, 0.4f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                }
            }

            /*if ((draw & MyWEMDebugDrawMode.FACES) != 0)
            {
                m_tmpDebugDrawFreeIndices.Clear();
                i = m_freeFaces;
                while (i != INVALID_INDEX)
                {
                    m_tmpDebugDrawFreeIndices.Add(i);
                    i = m_faceTable[i].NextFreeEntry;
                }

                for (int j = 0; j < m_faceTable.Count; ++j)
                {
                    if (m_tmpDebugDrawFreeIndices.Contains(j)) continue;

                    Vector3 center = Vector3.Zero;
                    int c = 0;

                    var face = GetFace(j);
                    var vEnum = face.GetVertexEnumerator();
                    while (vEnum.MoveNext())
                    {
                        center += vEnum.Current;
                        c++;
                    }

                    center /= c;
                    center = Vector3.Transform(center, drawMatrix);

                    MyRenderProxy.DebugDrawText3D(center, j.ToString(), Color.CadetBlue, 0.6f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                }
            }*/

            if ((draw & MyWEMDebugDrawMode.VERTICES) != 0 || (draw & MyWEMDebugDrawMode.VERTICES_DETAILED) != 0)
            {
                m_tmpDebugDrawFreeIndices.Clear();
                i = m_freeVertices;
                while (i != INVALID_INDEX)
                {
                    m_tmpDebugDrawFreeIndices.Add(i);
                    i = m_vertexTable[i].NextFreeEntry;
                }

                for (int j = 0; j < m_vertexTable.Count; ++j)
                {
                    if (m_tmpDebugDrawFreeIndices.Contains(j)) continue;

                    var pos = Vector3.Transform(m_vertexTable[j].VertexPosition, drawMatrix);

                    if ((draw & MyWEMDebugDrawMode.VERTICES_DETAILED) != 0)
                        MyRenderProxy.DebugDrawText3D(pos, m_vertexTable[j].ToString(), Color.LightGreen, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    else
                        MyRenderProxy.DebugDrawText3D(pos, j.ToString(), Color.LightGreen, 0.5f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                }
            }

            m_tmpDebugDrawFreeIndices.Clear();
        }

        public void CustomDebugDrawFaces(ref Matrix drawMatrix, MyWEMDebugDrawMode draw, Func<object, string> drawFunction)
        {
            if ((draw & MyWEMDebugDrawMode.FACES) != 0)
            {
                m_tmpDebugDrawFreeIndices.Clear();
                int i = m_freeFaces;
                while (i != INVALID_INDEX)
                {
                    m_tmpDebugDrawFreeIndices.Add(i);
                    i = m_faceTable[i].NextFreeEntry;
                }

                for (int j = 0; j < m_faceTable.Count; ++j)
                {
                    if (m_tmpDebugDrawFreeIndices.Contains(j)) continue;

                    Vector3 center = Vector3.Zero;
                    int c = 0;

                    var face = GetFace(j);
                    var vEnum = face.GetVertexEnumerator();
                    while (vEnum.MoveNext())
                    {
                        center += vEnum.Current;
                        c++;
                    }

                    center /= c;
                    center = Vector3.Transform(center, drawMatrix);

                    MyRenderProxy.DebugDrawText3D(center, drawFunction(face.GetUserData<object>()), Color.CadetBlue, 0.6f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                }
            }
        }

        [Conditional("DEBUG")]
        private void CheckVertexLoopConsistency(int vertexIndex)
        {
            // TODO
        }

        /// <summary>
        /// Checks for loops in the meshe's tables' freed entries
        /// </summary>
        [Conditional("DEBUG")]
        private void CheckFreeEntryConsistency()
        {
            if (!BASIC_CONSISTENCY_CHECKS) return;

            m_tmpVisitedIndices.Clear();
            int i = m_freeVertices;
            while (i != INVALID_INDEX)
            {
                i = m_vertexTable[i].NextFreeEntry;
                Debug.Assert(!m_tmpVisitedIndices.Contains(i));
                m_tmpVisitedIndices.Add(i);
            }
            m_tmpVisitedIndices.Clear();

            i = m_freeEdges;
            while (i != INVALID_INDEX)
            {
                i = m_edgeTable[i].NextFreeEntry;
                Debug.Assert(!m_tmpVisitedIndices.Contains(i));
                m_tmpVisitedIndices.Add(i);
            }
            m_tmpVisitedIndices.Clear();

            i = m_freeFaces;
            while (i != INVALID_INDEX)
            {
                i = m_faceTable[i].NextFreeEntry;
                Debug.Assert(!m_tmpVisitedIndices.Contains(i));
                m_tmpVisitedIndices.Add(i);
            }
            m_tmpVisitedIndices.Clear();
        }

        [Conditional("DEBUG")]
        private void CheckEdgeIndexValid(int index)
        {
            if (!BASIC_CONSISTENCY_CHECKS) return;
            Debug.Assert(index >= 0 && index < m_edgeTable.Count);
            int i = m_freeEdges;
            while (i != INVALID_INDEX)
            {
                Debug.Assert(i != index);
                i = m_edgeTable[i].NextFreeEntry;
            }
        }

        [Conditional("DEBUG")]
        private void CheckFaceIndexValid(int index)
        {
            if (!BASIC_CONSISTENCY_CHECKS) return;
            Debug.Assert(index >= 0 && index < m_faceTable.Count);
            int i = m_freeFaces;
            while (i != INVALID_INDEX)
            {
                Debug.Assert(i != index);
                i = m_faceTable[i].NextFreeEntry;
            }
        }

        [Conditional("DEBUG")]
        private void CheckVertexIndexValid(int index)
        {
            if (!BASIC_CONSISTENCY_CHECKS) return;
            Debug.Assert(index >= 0 && index < m_vertexTable.Count);
            int i = m_freeVertices;
            while (i != INVALID_INDEX)
            {
                Debug.Assert(i != index);
                i = m_vertexTable[i].NextFreeEntry;
            }
        }

        [Conditional("DEBUG")]
        public void CheckFaceIndexValidQuick(int index)
        {
            if (!BASIC_CONSISTENCY_CHECKS) return;
            Debug.Assert(index >= 0 && index < m_faceTable.Count);
            Debug.Assert(!m_tmpFreeFaces.Contains(index));
        }

        [Conditional("DEBUG")]
        public void CheckEdgeIndexValidQuick(int index)
        {
            if (!BASIC_CONSISTENCY_CHECKS) return;
            Debug.Assert(index >= 0 && index < m_edgeTable.Count);
            Debug.Assert(!m_tmpFreeEdges.Contains(index));
        }

        [Conditional("DEBUG")]
        public void CheckVertexIndexValidQuick(int index)
        {
            if (!BASIC_CONSISTENCY_CHECKS) return;
            Debug.Assert(index >= 0 && index < m_vertexTable.Count);
            Debug.Assert(!m_tmpFreeVertices.Contains(index));
        }

        [Conditional("DEBUG")]
        public void PrepareFreeEdgeHashset()
        {
            m_tmpFreeEdges.Clear();
            int i = m_freeEdges;
            while (i != INVALID_INDEX)
            {
                m_tmpFreeEdges.Add(i);
                i = m_edgeTable[i].NextFreeEntry;
                Debug.Assert(!m_tmpFreeEdges.Contains(i));
            }
        }

        [Conditional("DEBUG")]
        public void PrepareFreeFaceHashset()
        {
            m_tmpFreeFaces.Clear();
            int i = m_freeFaces;
            while (i != INVALID_INDEX)
            {
                m_tmpFreeFaces.Add(i);
                i = m_faceTable[i].NextFreeEntry;
                Debug.Assert(!m_tmpFreeFaces.Contains(i));
            }
        }

        [Conditional("DEBUG")]
        public void PrepareFreeVertexHashset()
        {
            m_tmpFreeVertices.Clear();
            int i = m_freeVertices;
            while (i != INVALID_INDEX)
            {
                m_tmpFreeVertices.Add(i);
                i = m_vertexTable[i].NextFreeEntry;
                Debug.Assert(!m_tmpFreeVertices.Contains(i));
            }
        }

        [Conditional("DEBUG")]
        public void CheckMeshConsistency()
        {
            if (!ADVANCED_CONSISTENCY_CHECKS) return;

            // Check for loops in free edges, faces and verts. Also, save sets of free indices for further checking
            PrepareFreeEdgeHashset();
            PrepareFreeFaceHashset();
            PrepareFreeVertexHashset();

            for (int j = 0; j < m_edgeTable.Count; ++j)
            {
                if (m_tmpFreeEdges.Contains(j)) continue;

                // Basic sanity checks for all edge indices
                EdgeTableEntry entry = m_edgeTable[j];
                if (entry.LeftFace != INVALID_INDEX) CheckFaceIndexValidQuick(entry.LeftFace);
                if (entry.RightFace != INVALID_INDEX) CheckFaceIndexValidQuick(entry.RightFace);
                CheckVertexIndexValidQuick(entry.Vertex1);
                CheckVertexIndexValidQuick(entry.Vertex2);
                CheckEdgeIndexValidQuick(entry.LeftPred);
                CheckEdgeIndexValidQuick(entry.RightPred);
                CheckEdgeIndexValidQuick(entry.LeftSucc);
                CheckEdgeIndexValidQuick(entry.RightSucc);

                Debug.Assert(entry.LeftFace != entry.RightFace);
                Debug.Assert(entry.LeftSucc != j);
                Debug.Assert(entry.LeftPred != j);
                Debug.Assert(entry.RightSucc != j);
                Debug.Assert(entry.RightPred != j);

                // Forbid Moebius-like geometry or two isolated edges pointing at each other
                Debug.Assert(entry.LeftPred != entry.RightPred);
                Debug.Assert(entry.LeftSucc != entry.RightSucc);

                // Check whether neighbouring edges share the correct vertex or vertices
                if (entry.LeftPred == entry.LeftSucc)
                {
                    Debug.Assert(
                        (m_edgeTable[entry.LeftPred].Vertex1 == entry.Vertex2 && m_edgeTable[entry.LeftPred].Vertex2 == entry.Vertex1) ||
                        (m_edgeTable[entry.LeftPred].Vertex1 == entry.Vertex1 && m_edgeTable[entry.LeftPred].Vertex2 == entry.Vertex2)
                    );
                    Debug.Assert(entry.LeftFace == INVALID_INDEX); // Two-edged faces can be only the empty ones
                }
                else
                {
                    Debug.Assert(m_edgeTable[entry.LeftPred].TryGetSharedVertex(ref entry) == entry.Vertex2);
                    Debug.Assert(m_edgeTable[entry.LeftSucc].TryGetSharedVertex(ref entry) == entry.Vertex1);
                }

                if (entry.RightPred == entry.RightSucc)
                {
                    Debug.Assert(
                        (m_edgeTable[entry.RightPred].Vertex1 == entry.Vertex2 && m_edgeTable[entry.RightPred].Vertex2 == entry.Vertex1) ||
                        (m_edgeTable[entry.RightPred].Vertex1 == entry.Vertex1 && m_edgeTable[entry.RightPred].Vertex2 == entry.Vertex2)
                    );
                    Debug.Assert(entry.RightFace == INVALID_INDEX); // Two-edged faces can be only the empty ones
                }
                else
                {
                    Debug.Assert(m_edgeTable[entry.RightPred].TryGetSharedVertex(ref entry) == entry.Vertex1);
                    Debug.Assert(m_edgeTable[entry.RightSucc].TryGetSharedVertex(ref entry) == entry.Vertex2);
                }

                // Check whether neighbouring edges share the correct face
                Debug.Assert(m_edgeTable[entry.LeftPred].VertexRightFace(entry.Vertex2) == entry.LeftFace);
                Debug.Assert(m_edgeTable[entry.RightPred].VertexRightFace(entry.Vertex1) == entry.RightFace);
                Debug.Assert(m_edgeTable[entry.LeftSucc].VertexLeftFace(entry.Vertex1) == entry.LeftFace);
                Debug.Assert(m_edgeTable[entry.RightSucc].VertexLeftFace(entry.Vertex2) == entry.RightFace);
            }

            // Check that every vertex points to an existing edge
            for (int i = 0; i < m_vertexTable.Count; ++i)
            {
                if (m_tmpFreeVertices.Contains(i)) continue;

                VertexTableEntry v = m_vertexTable[i];

                int emptyFaceCount = 0;

                int incidentEdge = v.IncidentEdge;
                CheckEdgeIndexValidQuick(incidentEdge);
                EdgeTableEntry e = m_edgeTable[incidentEdge];
                Debug.Assert(e.Vertex1 == i || e.Vertex2 == i, "Incident edge of vertex not pointing back at the vertex!");
                if (e.VertexLeftFace(i) == INVALID_INDEX)
                {
                    emptyFaceCount++;
                }

                // Check triangle fans around vertices for free faces (there can only be one)
                int nextEdge = e.VertexSucc(i);
                while (nextEdge != incidentEdge)
                {
                    e = m_edgeTable[nextEdge];
                    if (e.VertexLeftFace(i) == INVALID_INDEX)
                    {
                        emptyFaceCount++;
                    }
                    nextEdge = e.VertexSucc(i);
                }

                Debug.Assert(emptyFaceCount <= 1, "Every vertex triangle fan can only have one free face!");
            }


            // Check that every face points to an existing edge
            for (int i = 0; i < m_faceTable.Count; ++i)
            {
                if (m_tmpFreeFaces.Contains(i)) continue;

                FaceTableEntry face = m_faceTable[i];
                CheckEdgeIndexValidQuick(face.IncidentEdge);
            }
        }

        public int ApproximateMemoryFootprint()
        {
            int listOverhead = MyEnvironment.Is64BitProcess ? 32 : 20; // Counting with 1 ptr, 2 ints and 8/16B internal GC data
            int navTriOverhead = MyEnvironment.Is64BitProcess ? 88 : 56; // Approximation of the navigation triangle size plus its pathfinding data and heap item
            int selfOverhead = MyEnvironment.Is64BitProcess ? 52 : 32; // 3*int, 3*ptr, 8/16 GC data
            int vSize = 16;
            int eSize = 32;
            int fSize = (MyEnvironment.Is64BitProcess ? 8 : 12) + navTriOverhead;
            return selfOverhead + 3 * listOverhead + m_edgeTable.Capacity * eSize + m_faceTable.Capacity * fSize + m_vertexTable.Capacity * vSize;
        }
    }
}
