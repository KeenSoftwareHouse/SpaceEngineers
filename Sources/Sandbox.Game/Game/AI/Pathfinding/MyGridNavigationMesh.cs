using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Utils;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyGridNavigationMesh : MyNavigationMesh
    {
        private struct EdgeIndex: IEquatable<EdgeIndex>
        {
            public Vector3I A;
            public Vector3I B;

            public EdgeIndex(Vector3I PointA, Vector3I PointB)
            {
                A = PointA;
                B = PointB;
            }

            public EdgeIndex(ref Vector3I PointA, ref Vector3I PointB)
            {
                A = PointA;
                B = PointB;
            }

            public override int GetHashCode()
            {
                return A.GetHashCode() * 1610612741 + B.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                Debug.Assert(false, "Equals on struct does allocation!");
                if (!(obj is EdgeIndex)) return false;
                return this.Equals((EdgeIndex)obj);
            }

            public override string ToString()
            {
                return "(" + A.ToString() + ", " + B.ToString() + ")";
            }

            public bool Equals(EdgeIndex other)
            {
                return A == other.A && B == other.B;
            }
        }

        public class Component : IMyHighLevelComponent
        {
            private MyGridNavigationMesh m_parent;
            private int m_componentIndex;

            public Component(MyGridNavigationMesh parent, int componentIndex)
            {
                m_parent = parent;
                m_componentIndex = componentIndex;
            }

            public bool Contains(MyNavigationPrimitive primitive)
            {
                if (primitive.Group != m_parent) return false;

                var triangle = primitive as MyNavigationTriangle;
                if (triangle == null) return false;

                return triangle.ComponentIndex == m_componentIndex;
            }

            public bool FullyExplored
            {
                get { return true; }
            }
        }

        private MyCubeGrid m_grid;
        private Dictionary<Vector3I, List<int>> m_smallTriangleRegistry; // This could be optimized to take less space
        private MyVector3ISet m_cubeSet;

        private Dictionary<EdgeIndex, int> m_connectionHelper;
        private MyNavmeshCoordinator m_coordinator;

        private MyHighLevelGroup m_higherLevel;
        private MyGridHighLevelHelper m_higherLevelHelper;
        private Component m_component;

        private static HashSet<Vector3I> m_mergeHelper;
        private static List<KeyValuePair<MyNavigationTriangle, Vector3I>> m_tmpTriangleList;

        // Whether the mesh can be modified
        private bool m_static;

        public bool HighLevelDirty
        {
            get
            {
                return m_higherLevelHelper.IsDirty;
            }
        }

        static MyGridNavigationMesh()
        {
            m_mergeHelper = new HashSet<Vector3I>();
            m_tmpTriangleList = new List<KeyValuePair<MyNavigationTriangle, Vector3I>>();
        }

        public MyGridNavigationMesh(MyCubeGrid grid, MyNavmeshCoordinator coordinator, int triPrealloc = 32, Func<long> timestampFunction = null)
            : base(coordinator != null ? coordinator.Links : null, triPrealloc, timestampFunction)
        {
            m_connectionHelper = new Dictionary<EdgeIndex, int>();
            m_smallTriangleRegistry = new Dictionary<Vector3I, List<int>>();
            m_cubeSet = new MyVector3ISet();

            m_coordinator = coordinator;

            m_static = false;
            if (grid != null)
            {
                m_higherLevel = new MyHighLevelGroup(this, coordinator.HighLevelLinks, timestampFunction);
                m_higherLevelHelper = new MyGridHighLevelHelper(this, m_smallTriangleRegistry, new Vector3I(8, 8, 8));

                m_grid = grid;
                grid.OnBlockAdded += grid_OnBlockAdded;
                grid.OnBlockRemoved += grid_OnBlockRemoved;

                float divisor = 1.0f / grid.CubeBlocks.Count;
                Vector3 center = Vector3.Zero;

                foreach (var block in grid.CubeBlocks)
                {
                    OnBlockAddedInternal(block);
                    center += block.Position * grid.GridSize * divisor;
                }
            }
        }

        public override string ToString()
        {
            return "Grid NavMesh: " + m_grid.DisplayName;
        }

        public void UpdateHighLevel()
        {
            m_higherLevelHelper.ProcessChangedCellComponents();
        }

        public MyNavigationTriangle AddTriangle(ref Vector3 a, ref Vector3 b, ref Vector3 c)
        {
            Debug.Assert(m_grid == null, "Triangles can be added to the grid navigation mesh from the outside only if it's a mesh for block navigation definition!");
            if (m_grid != null)
            {
                return null;
            }

            return AddTriangleInternal(a, b, c);
        }

        private MyNavigationTriangle AddTriangleInternal(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3I positionA = Vector3I.Round(a * 256);
            Vector3I positionB = Vector3I.Round(b * 256);
            Vector3I positionC = Vector3I.Round(c * 256);

            // The positions are rounded so that we can transform the triangle and still get the correct integer positions
            Vector3 roundedA = positionA; roundedA /= 256;
            Vector3 roundedB = positionB; roundedB /= 256;
            Vector3 roundedC = positionC; roundedC /= 256;

            int edgeAB, edgeBC, edgeCA;
            if (!m_connectionHelper.TryGetValue(new EdgeIndex(ref positionB, ref positionA), out edgeAB)) edgeAB = -1;
            if (!m_connectionHelper.TryGetValue(new EdgeIndex(ref positionC, ref positionB), out edgeBC)) edgeBC = -1;
            if (!m_connectionHelper.TryGetValue(new EdgeIndex(ref positionA, ref positionC), out edgeCA)) edgeCA = -1;

            int formerAB = edgeAB;
            int formerBC = edgeBC;
            int formerCA = edgeCA;

            // B and C are flipped to fix the fact that the game uses flipped coords (Z axis is flipped)
            var tri = AddTriangle(ref roundedA, ref roundedC, ref roundedB, ref edgeCA, ref edgeBC, ref edgeAB);

            // Triangle edges are registered counter-clockwise, and the original coordinates are ordered counter-clockwise)

            if (formerAB == -1)
                m_connectionHelper.Add(new EdgeIndex(ref positionA, ref positionB), edgeAB);
            else
                m_connectionHelper.Remove(new EdgeIndex(ref positionB, ref positionA));

            if (formerBC == -1)
                m_connectionHelper.Add(new EdgeIndex(ref positionB, ref positionC), edgeBC);
            else
                m_connectionHelper.Remove(new EdgeIndex(ref positionC, ref positionB));

            if (formerCA == -1)
                m_connectionHelper.Add(new EdgeIndex(ref positionC, ref positionA), edgeCA);
            else
                m_connectionHelper.Remove(new EdgeIndex(ref positionA, ref positionC));

            return tri;
        }

        public void RegisterTriangle(MyNavigationTriangle tri, ref Vector3I gridPos)
        {
            Debug.Assert(m_grid == null, "Triangles can be registered in the grid navigation mesh from the outside only if it's a mesh for block navigation definition!");
            if (m_grid != null)
            {
                return;
            }

            RegisterTriangleInternal(tri, ref gridPos);
        }

        private void RegisterTriangleInternal(MyNavigationTriangle tri, ref Vector3I gridPos)
        {
            Debug.Assert(!tri.Registered); // So far, triangles can only be registered in one cube. This can change in the future with optimizations

            List<int> list = null;
            if (!m_smallTriangleRegistry.TryGetValue(gridPos, out list))
            {
                list = new List<int>();
                m_smallTriangleRegistry.Add(gridPos, list);
            }

            list.Add(tri.Index);

            tri.Registered = true;
        }

        public MyVector3ISet.Enumerator GetCubes()
        {
            return m_cubeSet.GetEnumerator();
        }

        public void GetCubeTriangles(Vector3I gridPos, List<MyNavigationTriangle> trianglesOut)
        {
            List<int> triList = null;
            if (m_smallTriangleRegistry.TryGetValue(gridPos, out triList))
            {
                for (int i = 0; i < triList.Count; ++i)
                    trianglesOut.Add(GetTriangle(triList[i]));
            }
        }

        private void MergeFromAnotherMesh(MyGridNavigationMesh otherMesh, ref MatrixI transform)
        {
            ProfilerShort.Begin("MergeFromAnotherMesh");
            m_mergeHelper.Clear();

            // Save the cubes from the other mesh that are touching cubes of this mesh into a helper set.
            // Also put the touched cubes from this mesh into the set.
            foreach (var position in otherMesh.m_smallTriangleRegistry.Keys)
            {
                bool add = false;
                foreach (var direction in Base6Directions.IntDirections)
                {
                    // CH: TODO: We query the grid so far, but in the future, we should make sure that the access is thread-safe
                    Vector3I pos = Vector3I.Transform(position + direction, transform);
                    if (m_cubeSet.Contains(ref pos)) // Test the transformed position...
                    {
                        m_mergeHelper.Add(position + direction); // ... but add the original one
                        add = true;
                    }
                }
                if (add) m_mergeHelper.Add(position);
            }

            foreach (var entry in otherMesh.m_smallTriangleRegistry)
            {
                Vector3I originalCube = entry.Key;
                Vector3I tformedCube; Vector3I.Transform(ref originalCube, ref transform, out tformedCube);

                // If the cube is one of the touching cubes, we have to intersect the touching triangles
                if (m_mergeHelper.Contains(originalCube))
                {
                    // Take the touching pairs one by one and calculate triangulation of the disjoint union of the opposing faces
                    // Remove the opposing faces from the old block
                    // Add the triangulation to the mesh
                    // Add the rest of the navmesh from this block to the mesh

                    m_tmpTriangleList.Clear();

                    // CH: TODO. Just remove the triangles now
                    foreach (var direction in Base6Directions.EnumDirections)
                    {
                        Vector3I directionVec = Base6Directions.GetIntVector((int)direction);
                        Base6Directions.Direction tformedDirection = transform.GetDirection(direction);
                        Vector3I tformedFlippedVec = Base6Directions.GetIntVector((int)Base6Directions.GetFlippedDirection(tformedDirection));

                        // Remove face triangles from this mesh
                        if (m_mergeHelper.Contains(originalCube + directionVec))
                        {
                            List<int> triList = null;
                            if (m_smallTriangleRegistry.TryGetValue(tformedCube - tformedFlippedVec, out triList))
                            {
                                foreach (var index in triList)
                                {
                                    var triangle = GetTriangle(index);
                                    // CH: TODO: This will probably be expensive. Could we precalculate it?
                                    if (IsFaceTriangle(triangle, tformedCube - tformedFlippedVec, tformedFlippedVec))
                                        m_tmpTriangleList.Add(new KeyValuePair<MyNavigationTriangle, Vector3I>(triangle, tformedCube - tformedFlippedVec));
                                }
                            }
                        }
                    }

                    foreach (var triangle in m_tmpTriangleList)
                    {
                        RemoveTriangle(triangle.Key, triangle.Value);
                    }
                    m_tmpTriangleList.Clear();

                    int debugCounter = 0;

                    // CH: TODO: optimize this (actually whole this method)
                    foreach (var triangleIndex in entry.Value)
                    {
                        var triangle = otherMesh.GetTriangle(triangleIndex);
                        Vector3I pos = entry.Key;
                        bool addTriangle = true;
                        foreach (var direction in Base6Directions.EnumDirections)
                        {
                            Vector3I dirvec = Base6Directions.GetIntVector((int)direction);
                            if (m_mergeHelper.Contains(pos + dirvec) && IsFaceTriangle(triangle, pos, dirvec))
                            {
                                addTriangle = false;
                                break;
                            }
                        }

                        if (addTriangle)
                        {
                            if (debugCounter == 5) { }
                            CopyTriangle(triangle, pos, ref transform);
                            debugCounter++;
                        }
                    }
                }
                // Otherwise, we just transform the triangles from the other mesh and add them to this mesh
                else
                {
                    foreach (var triangleIndex in entry.Value)
                    {
                        var triangle = otherMesh.GetTriangle(triangleIndex);
                        CopyTriangle(triangle, entry.Key, ref transform);
                        //if (triangleIndex > 1) break;
                    }
                }
            }

            m_mergeHelper.Clear();
            ProfilerShort.End();
        }

        private bool IsFaceTriangle(MyNavigationTriangle triangle, Vector3I cubePosition, Vector3I direction)
        {
            var e = triangle.GetVertexEnumerator();

            // Quantize the point positions to 1/256ths inside each cube (but multiplied by 256 :-) )
            e.MoveNext();
            Vector3I positionA = Vector3I.Round(e.Current * 256);
            e.MoveNext();
            Vector3I positionB = Vector3I.Round(e.Current * 256);
            e.MoveNext();
            Vector3I positionC = Vector3I.Round(e.Current * 256);
            cubePosition = cubePosition * 256;

            // Change the point coordinates to be relative to the face center
            Vector3I faceCenter = cubePosition + direction * 128;
            positionA -= faceCenter;
            positionB -= faceCenter;
            positionC -= faceCenter;

            // Discard all triangles whose points are not on the plane of the face
            if (positionA * direction != Vector3I.Zero) return false;
            if (positionB * direction != Vector3I.Zero) return false;
            if (positionC * direction != Vector3I.Zero) return false;

            // Discard all triangles that are not contained inside the face's square
            return (positionA.AbsMax() <= 128 && positionB.AbsMax() <= 128 && positionC.AbsMax() <= 128);
        }

        private void RemoveTriangle(MyNavigationTriangle triangle, Vector3I cube)
        {
            var e = triangle.GetVertexEnumerator();
            e.MoveNext();
            Vector3I positionA = Vector3I.Round(e.Current * 256);
            e.MoveNext();
            Vector3I positionC = Vector3I.Round(e.Current * 256); // This is not a mistake! C is swapped with B (see also: AddTriangle...)
            e.MoveNext();
            Vector3I positionB = Vector3I.Round(e.Current * 256);

            // This should be AB, BC and CA, but after swapping it's AC, CB and BA
            int triCA = triangle.GetEdgeIndex(0);
            int triBC = triangle.GetEdgeIndex(1);
            int triAB = triangle.GetEdgeIndex(2);

            int edgeAB, edgeBC, edgeCA;
            if (!m_connectionHelper.TryGetValue(new EdgeIndex(ref positionA, ref positionB), out edgeAB)) edgeAB = -1;
            if (!m_connectionHelper.TryGetValue(new EdgeIndex(ref positionB, ref positionC), out edgeBC)) edgeBC = -1;
            if (!m_connectionHelper.TryGetValue(new EdgeIndex(ref positionC, ref positionA), out edgeCA)) edgeCA = -1;

            // Register those edges that were not registered in the connection helper.
            // They are registered counter-clockwise, because we are in fact registering the other triangle across the edges
            if (edgeAB != -1 && triAB == edgeAB)
                m_connectionHelper.Remove(new EdgeIndex(ref positionA, ref positionB));
            else
                m_connectionHelper.Add(new EdgeIndex(positionB, positionA), triAB);

            if (edgeBC != -1 && triBC == edgeBC)
                m_connectionHelper.Remove(new EdgeIndex(ref positionB, ref positionC));
            else
                m_connectionHelper.Add(new EdgeIndex(positionC, positionB), triBC);

            if (edgeCA != -1 && triCA == edgeCA)
                m_connectionHelper.Remove(new EdgeIndex(ref positionC, ref positionA));
            else
                m_connectionHelper.Add(new EdgeIndex(positionA, positionC), triCA);

            List<int> list = null;
            m_smallTriangleRegistry.TryGetValue(cube, out list);
            Debug.Assert(list != null, "Removed triangle was not registered in the registry!");
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] == triangle.Index)
                {
                    list.RemoveAtFast(i);
                    break;
                }
            }
            if (list.Count == 0)
                m_smallTriangleRegistry.Remove(cube);

            RemoveTriangle(triangle);

            if (edgeAB != -1 && triAB != edgeAB)
                RemoveAndAddTriangle(ref positionA, ref positionB, edgeAB);
            if (edgeBC != -1 && triBC != edgeBC)
                RemoveAndAddTriangle(ref positionB, ref positionC, edgeBC);
            if (edgeCA != -1 && triCA != edgeCA)
                RemoveAndAddTriangle(ref positionC, ref positionA, edgeCA);
        }

        private void RemoveAndAddTriangle(ref Vector3I positionA, ref Vector3I positionB, int registeredEdgeIndex)
        {
            // This edge was registered by another triangle, so re-adding that triangle should help
            Vector3 removedA, removedB, removedC;

            var removedTriangle = GetEdgeTriangle(registeredEdgeIndex);
            var vertices = removedTriangle.GetVertexEnumerator();
            vertices.MoveNext(); removedA = vertices.Current;
            vertices.MoveNext(); removedC = vertices.Current; // This is not a mistake! C and B are swapped (see also: AddTriangle...)
            vertices.MoveNext(); removedB = vertices.Current;
            Debug.Assert(vertices.MoveNext() == false);

            // We don't save the registered cube for the triangles, so we have to search the registry
            Vector3I cube = FindTriangleCube(removedTriangle.Index, ref positionA, ref positionB);
            RemoveTriangle(removedTriangle, cube);
            var addedTriangle = AddTriangleInternal(removedA, removedB, removedC);
            RegisterTriangleInternal(addedTriangle, ref cube);
        }

        private Vector3I FindTriangleCube(int triIndex, ref Vector3I edgePositionA, ref Vector3I edgePositionB)
        {
            Vector3I min, max;
            Vector3I.Min(ref edgePositionA, ref edgePositionB, out min);
            Vector3I.Max(ref edgePositionA, ref edgePositionB, out max);
            min = Vector3I.Round(new Vector3(min) / 256.0f - Vector3.Half);
            max = Vector3I.Round(new Vector3(max) / 256.0f + Vector3.Half);

            for (var it = new Vector3I_RangeIterator(ref min, ref max); it.IsValid(); it.GetNext(out min))
            {
                List<int> list;
                m_smallTriangleRegistry.TryGetValue(min, out list);
                if (list == null) continue;

                if (list.Contains(triIndex))
                    return (min);
            }

            Debug.Assert(false, "Could not find navmesh triangle cube. Shouldn't get here!");
            return Vector3I.Zero;
        }

        private void CopyTriangle(MyNavigationTriangle otherTri, Vector3I triPosition, ref MatrixI transform)
        {
            Vector3 newA, newB, newC;
            otherTri.GetTransformed(ref transform, out newA, out newB, out newC);

            if (MyPerGameSettings.NavmeshPresumesDownwardGravity)
            {
                Vector3 n = Vector3.Cross(newC - newA, newB - newA);
                n.Normalize();
                if (Vector3.Dot(n, Base6Directions.GetVector(Base6Directions.Direction.Up)) < 0.7f) return; // Slightly lower than sqrt(2)/2 = 45 deg
            }

            Vector3I.Transform(ref triPosition, ref transform, out triPosition);

            // This is not an error - we need to swap C and B from the navmesh,
            // because they will be swapped again when the triangle is added
            var tri = AddTriangleInternal(newA, newC, newB);
            RegisterTriangleInternal(tri, ref triPosition);
        }

        public void MakeStatic()
        {
            if (m_static == true)
            {
                return;
            }

            m_static = true;
            m_connectionHelper = null;
            m_cubeSet = null;
        }

        /// <summary>
        /// All coords should be in the grid local coordinates
        /// </summary>
        public List<Vector4D> FindPath(Vector3 start, Vector3 end)
        {
            start /= m_grid.GridSize;
            end /= m_grid.GridSize;

            float closestDistSq = float.PositiveInfinity;
            MyNavigationTriangle startTri = GetClosestNavigationTriangle(ref start, ref closestDistSq);
            if (startTri == null) return null;

            closestDistSq = float.PositiveInfinity;
            MyNavigationTriangle endTri = GetClosestNavigationTriangle(ref end, ref closestDistSq);
            if (endTri == null) return null;

            var path = FindRefinedPath(startTri, endTri, ref start, ref end);

            if (path != null)
            {
                for (int i = 0; i < path.Count; ++i)
                {
                    var point = path[i];
                    point *= m_grid.GridSize;
                    path[i] = point;
                }
            }

            return path;
        }

        private MyNavigationTriangle GetClosestNavigationTriangle(ref Vector3 point, ref float closestDistSq)
        {
            Vector3I cube; Vector3I.Round(ref point, out cube);

            MyNavigationTriangle closest = null;

            Vector3I pos = cube - new Vector3I(4, 4, 4);
            Vector3I end = cube + new Vector3I(4, 4, 4);
            for (var it = new Vector3I_RangeIterator(ref pos, ref end); it.IsValid(); it.GetNext(out pos))
            {
                List<int> list; m_smallTriangleRegistry.TryGetValue(pos, out list);
                if (list == null) continue;

                foreach (var triIndex in list)
                {
                    var tri = GetTriangle(triIndex);

                    var vertices = tri.GetVertexEnumerator();
                    vertices.MoveNext(); Vector3 v1 = vertices.Current;
                    vertices.MoveNext(); Vector3 v2 = vertices.Current;
                    vertices.MoveNext(); Vector3 v3 = vertices.Current;

                    Vector3 center = (v1 + v2 + v3) / 3.0f;

                    Vector3 v12 = (v2 - v1);
                    Vector3 v23 = (v3 - v2);

                    float dsq = Vector3.DistanceSquared(center, point);

                    // If (point - center)^2 >= v12^2 + v13^2, the point is certainly outside the tri
                    // For proof, ask Cestmir :-)
                    // Otherwise, we have to calculate the exact distance
                    if (dsq < (v12.LengthSquared() + v23.LengthSquared()))
                    {
                        Vector3 v31 = (v1 - v3);

                        Vector3 triN = Vector3.Cross(v12, v23);
                        triN.Normalize();

                        v12 = Vector3.Cross(v12, triN);
                        v23 = Vector3.Cross(v23, triN);
                        v31 = Vector3.Cross(v31, triN);

                        float d1 = -Vector3.Dot(v12, v1);
                        float d2 = -Vector3.Dot(v23, v2);
                        float d3 = -Vector3.Dot(v31, v3);

                        float p1 = Vector3.Dot(v12, point) + d1;
                        float p2 = Vector3.Dot(v23, point) + d2;
                        float p3 = Vector3.Dot(v31, point) + d3;

                        dsq = Vector3.Dot(triN, point) - Vector3.Dot(triN, center);
                        dsq = dsq * dsq;
                        if (p1 > 0)
                        {
                            if (p2 > 0)
                            {
                                if (p3 < 0)
                                    dsq += p3 * p3;
                            }
                            else
                            {
                                if (p3 > 0)
                                    dsq += p2 * p2;
                                else
                                    dsq += Vector3.DistanceSquared(v3, point);
                            }
                        }
                        else
                        {
                            if (p2 > 0)
                            {
                                if (p3 > 0)
                                    dsq += p1 * p1;
                                else
                                    dsq += Vector3.DistanceSquared(v1, point);
                            }
                            else
                            {
                                if (p3 > 0)
                                    dsq += Vector3.DistanceSquared(v2, point);
                            }
                        }
                    }

                    if (dsq < closestDistSq)
                    {
                        closest = tri;
                        closestDistSq = dsq;
                    }
                }
            }

            return closest;
        }

        public override MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, ref double closestDistanceSq)
        {
            // CH: TODO: Not supported yet
            if (highLevel == true)
            {
                return null;
            }

            Vector3 localPoint = Vector3D.Transform(point, m_grid.PositionComp.WorldMatrixNormalizedInv);
            localPoint /= m_grid.GridSize;
            float closestDistSq = (float)closestDistanceSq / m_grid.GridSize;

            MyNavigationTriangle tri = GetClosestNavigationTriangle(ref localPoint, ref closestDistSq);
            if (tri != null)
                closestDistanceSq = closestDistSq * m_grid.GridSize;

            return tri;
        }

        private void grid_OnBlockAdded(MySlimBlock block)
        {
            OnBlockAddedInternal(block);
        }

        private void OnBlockAddedInternal(MySlimBlock block)
        {
            var existingBlock = m_grid.GetCubeBlock(block.Position);
            var compound = existingBlock.FatBlock as MyCompoundCubeBlock;

            // Ignore blocks without navigation info (i.e. decorations and such)
            if (!(block.FatBlock is MyCompoundCubeBlock) && block.BlockDefinition.NavigationDefinition == null) return;

            bool noEntry = false;
            bool meshFound = false;
            if (compound != null)
            {
                var blocks = compound.GetBlocks();

                if (blocks.Count == 0) return;

                foreach (var subBlock in blocks)
                {
                    if (subBlock.BlockDefinition.NavigationDefinition == null) continue;
                    if (subBlock.BlockDefinition.NavigationDefinition.NoEntry || meshFound)
                    {
                        meshFound = false;
                        noEntry = true;
                        break;
                    }
                    else
                    {
                        block = subBlock;
                        meshFound = true;
                    }
                }
            }
            else
            {
                if (block.BlockDefinition.NavigationDefinition != null)
                {
                    if (block.BlockDefinition.NavigationDefinition.NoEntry)
                    {
                        meshFound = false;
                        noEntry = true;
                    }
                    else
                    {
                        meshFound = true;
                    }
                }
            }

            // Ignore compounds with blocks without navigation info
            if (!noEntry && !meshFound) return;

            if (noEntry)
            {
                if (m_cubeSet.Contains(block.Position))
                    RemoveBlock(block.Min, block.Max, true);

                Vector3I pos = default(Vector3I);
                for (pos.X = block.Min.X; pos.X <= block.Max.X; ++pos.X)
                {
                    for (pos.Y = block.Min.Y; pos.Y <= block.Max.Y; ++pos.Y)
                    {
                        pos.Z = block.Min.Z - 1;
                        if (m_cubeSet.Contains(ref pos))
                        {
                            EraseFaceTriangles(pos, Base6Directions.Direction.Backward);
                        }

                        pos.Z = block.Max.Z + 1;
                        if (m_cubeSet.Contains(ref pos))
                        {
                            EraseFaceTriangles(pos, Base6Directions.Direction.Forward);
                        }
                    }

                    for (pos.Z = block.Min.Z; pos.Z <= block.Max.Z; ++pos.Z)
                    {
                        pos.Y = block.Min.Y - 1;
                        if (m_cubeSet.Contains(ref pos))
                        {
                            EraseFaceTriangles(pos, Base6Directions.Direction.Up);
                        }

                        pos.Y = block.Max.Y + 1;
                        if (m_cubeSet.Contains(ref pos))
                        {
                            EraseFaceTriangles(pos, Base6Directions.Direction.Down);
                        }
                    }
                }

                for (pos.Y = block.Min.Y; pos.Y <= block.Max.Y; ++pos.Y)
                {
                    for (pos.Z = block.Min.Z; pos.Z <= block.Max.Z; ++pos.Z)
                    {
                        pos.X = block.Min.X - 1;
                        if (m_cubeSet.Contains(ref pos))
                        {
                            EraseFaceTriangles(pos, Base6Directions.Direction.Right);
                        }

                        pos.X = block.Max.X + 1;
                        if (m_cubeSet.Contains(ref pos))
                        {
                            EraseFaceTriangles(pos, Base6Directions.Direction.Left);
                        }
                    }
                }

                pos = block.Min;
                for (var it = new Vector3I_RangeIterator(ref pos, ref block.Max); it.IsValid(); it.GetNext(out pos))
                {
                    m_cubeSet.Add(pos);
                }
            }
            else
            {
                if (m_cubeSet.Contains(block.Position))
                {
                    RemoveBlock(block.Min, block.Max, eraseCubeSet: true);
                }
                AddBlock(block);
            }

            BoundingBoxD bbox;
            block.GetWorldBoundingBox(out bbox);
            bbox.Inflate(5.1f);

            m_coordinator.InvalidateVoxelsBBox(ref bbox);

            MarkBlockChanged(block);
        }

        private void grid_OnBlockRemoved(MySlimBlock block)
        {
            bool ignore = true;
            bool noEntry = false;
            bool meshFound = false;

            var existingBlock = m_grid.GetCubeBlock(block.Position);
            var compound = existingBlock == null ? null : existingBlock.FatBlock as MyCompoundCubeBlock;

            if (!(block.FatBlock is MyCompoundCubeBlock) && block.BlockDefinition.NavigationDefinition == null) return;

            // Ignore blocks without navigation info (i.e. decorations and such)
            if (compound == null)
            {
                Debug.Assert(existingBlock == null, "Removed a block from position, but there still is another block and it's not a compound!");

                ignore = false;
                if (existingBlock != null)
                {
                    if (block.BlockDefinition.NavigationDefinition.NoEntry)
                        noEntry = true;
                    else
                        meshFound = true;
                }
            }
            else
            {
                var blocks = compound.GetBlocks();

                if (blocks.Count != 0)
                {
                    foreach (var subBlock in blocks)
                    {
                        if (subBlock.BlockDefinition.NavigationDefinition == null) continue;
                        if (subBlock.BlockDefinition.NavigationDefinition.NoEntry || meshFound)
                        {
                            ignore = false;
                            noEntry = true;
                            break;
                        }
                        else
                        {
                            ignore = false;
                            meshFound = true;
                            block = subBlock;
                        }
                    }
                }
            }

            BoundingBoxD bbox;
            block.GetWorldBoundingBox(out bbox);
            bbox.Inflate(5.1f);

            //m_coordinator.RemoveGridNavmeshLinks(m_grid);
            m_coordinator.InvalidateVoxelsBBox(ref bbox);

            MarkBlockChanged(block);
            MyCestmirPathfindingShorts.Pathfinding.GridPathfinding.MarkHighLevelDirty();

            if (ignore)
            {
                RemoveBlock(block.Min, block.Max, eraseCubeSet: true);
                FixBlockFaces(block);
            }
            else if (noEntry)
            {
                RemoveBlock(block.Min, block.Max, eraseCubeSet: false);
            }
            else if (meshFound)
            {
                RemoveBlock(block.Min, block.Max, eraseCubeSet: true);
                AddBlock(block);
            }
            else
            {
                if (m_cubeSet.Contains(block.Position))
                {
                    RemoveBlock(block.Min, block.Max, eraseCubeSet: true);
                    FixBlockFaces(block);
                }
            }
        }

        private void MarkBlockChanged(MySlimBlock block)
        {
            m_higherLevelHelper.MarkBlockChanged(block);
            MyCestmirPathfindingShorts.Pathfinding.GridPathfinding.MarkHighLevelDirty();
        }

        private void AddBlock(MySlimBlock block)
        {
            Vector3I start = block.Min;
            Vector3I end = block.Max;
            for (var it = new Vector3I_RangeIterator(ref start, ref end); it.IsValid(); it.GetNext(out start))
            {
                Debug.Assert(!m_cubeSet.Contains(ref start));
                m_cubeSet.Add(ref start);
            }

            MatrixI transform = new MatrixI(block.Position, block.Orientation.Forward, block.Orientation.Up);
            MergeFromAnotherMesh(block.BlockDefinition.NavigationDefinition.Mesh, ref transform);
        }

        private void RemoveBlock(Vector3I min, Vector3I max, bool eraseCubeSet)
        {
            Vector3I pos = min;
            for (var it = new Vector3I_RangeIterator(ref pos, ref max); it.IsValid(); it.GetNext(out pos))
            {
                Debug.Assert(m_cubeSet.Contains(ref pos));

                if (eraseCubeSet)
                    m_cubeSet.Remove(ref pos);

                EraseCubeTriangles(pos);
            }
        }

        private void EraseCubeTriangles(Vector3I pos)
        {
            List<int> list;
            if (m_smallTriangleRegistry.TryGetValue(pos, out list))
            {
                m_tmpTriangleList.Clear();

                foreach (var triIndex in list)
                {
                    var triangle = GetTriangle(triIndex);
                    m_tmpTriangleList.Add(new KeyValuePair<MyNavigationTriangle, Vector3I>(triangle, pos));
                }

                foreach (var entry in m_tmpTriangleList)
                    RemoveTriangle(entry.Key, entry.Value);

                m_tmpTriangleList.Clear();

                m_smallTriangleRegistry.Remove(pos);
            }
        }

        private void EraseFaceTriangles(Vector3I pos, Base6Directions.Direction direction)
        {
            m_tmpTriangleList.Clear();

            Vector3I directionVec = Base6Directions.GetIntVector((int)direction);

            List<int> triList = null;
            if (m_smallTriangleRegistry.TryGetValue(pos, out triList))
            {
                foreach (var index in triList)
                {
                    var triangle = GetTriangle(index);
                    if (IsFaceTriangle(triangle, pos, directionVec))
                        m_tmpTriangleList.Add(new KeyValuePair<MyNavigationTriangle, Vector3I>(triangle, pos));
                }
            }

            foreach (var triangle in m_tmpTriangleList)
            {
                RemoveTriangle(triangle.Key, triangle.Value);
            }
            m_tmpTriangleList.Clear();
        }

        private void FixBlockFaces(MySlimBlock block)
        {
            Vector3I pos;
            Vector3I dir;
            for (pos.X = block.Min.X; pos.X <= block.Max.X; pos.X++)
            {
                for (pos.Y = block.Min.Y; pos.Y <= block.Max.Y; pos.Y++)
                {
                    dir = Vector3I.Backward; pos.Z = block.Min.Z - 1;
                    FixCubeFace(ref pos, ref dir);
                    dir = Vector3I.Forward; pos.Z = block.Max.Z + 1;
                    FixCubeFace(ref pos, ref dir);
                }
            }
            for (pos.X = block.Min.X; pos.X <= block.Max.X; pos.X++)
            {
                for (pos.Z = block.Min.Z; pos.Z <= block.Max.Z; pos.Z++)
                {
                    dir = Vector3I.Up; pos.Y = block.Min.Y - 1;
                    FixCubeFace(ref pos, ref dir);
                    dir = Vector3I.Down; pos.Y = block.Max.Y + 1;
                    FixCubeFace(ref pos, ref dir);
                }
            }
            for (pos.Y = block.Min.Y; pos.Y <= block.Max.Y; pos.Y++)
            {
                for (pos.Z = block.Min.Z; pos.Z <= block.Max.Z; pos.Z++)
                {
                    dir = Vector3I.Right; pos.X = block.Min.X - 1;
                    FixCubeFace(ref pos, ref dir);
                    dir = Vector3I.Left; pos.X = block.Max.X + 1;
                    FixCubeFace(ref pos, ref dir);
                }
            }
        }

        private void FixCubeFace(ref Vector3I pos, ref Vector3I dir)
        {
            if (m_cubeSet.Contains(ref pos))
            {
                // TODO: In the future, we'll have to remove the face's triangles
                // TODO: Cash the neighboring blocks in real-time and then perform the mesh modification on another thread
                var cubeBlock = m_grid.GetCubeBlock(pos);

                var compound = cubeBlock.FatBlock as MyCompoundCubeBlock;
                if (compound != null)
                {
                    var blocks = compound.GetBlocks();
                    MySlimBlock firstBlock = null;
                    foreach (var subblock in blocks)
                    {
                        if (subblock.BlockDefinition.NavigationDefinition != null)
                        {
                            firstBlock = subblock;
                            break;
                        }
                    }

                    if (firstBlock != null)
                        cubeBlock = firstBlock;
                }

                if (cubeBlock.BlockDefinition.NavigationDefinition == null) return;

                MatrixI transform = new MatrixI(cubeBlock.Position, cubeBlock.Orientation.Forward, cubeBlock.Orientation.Up);
                MatrixI invTform; MatrixI.Invert(ref transform, out invTform);
                Vector3I meshPos; Vector3I.Transform(ref pos, ref invTform, out meshPos);
                Vector3I faceDir; Vector3I.TransformNormal(ref dir, ref invTform, out faceDir);

                var neighborMesh = cubeBlock.BlockDefinition.NavigationDefinition.Mesh;
                if (neighborMesh == null) return;

                List<int> list;
                if (neighborMesh.m_smallTriangleRegistry.TryGetValue(meshPos, out list))
                {
                    foreach (var triIndex in list)
                    {
                        var triangle = neighborMesh.GetTriangle(triIndex);
                        if (IsFaceTriangle(triangle, meshPos, faceDir))
                        {
                            CopyTriangle(triangle, meshPos, ref transform);
                        }
                    }
                }
            }
        }

        public override MatrixD GetWorldMatrix()
        {
            MatrixD m = m_grid.WorldMatrix;
            MatrixD.Rescale(ref m, m_grid.GridSize);
            return m;
        }

        public override Vector3 GlobalToLocal(Vector3D globalPos)
        {
            Vector3 local = Vector3D.Transform(globalPos, m_grid.PositionComp.WorldMatrixNormalizedInv);
            local /= m_grid.GridSize;
            return local;
        }

        public override Vector3D LocalToGlobal(Vector3 localPos)
        {
            localPos *= m_grid.GridSize;
            return Vector3D.Transform(localPos, m_grid.WorldMatrix);
        }

        public override MyHighLevelGroup HighLevelGroup
        {
            get { return m_higherLevel; }
        }

        public override MyHighLevelPrimitive GetHighLevelPrimitive(MyNavigationPrimitive myNavigationTriangle)
        {
            return m_higherLevelHelper.GetHighLevelNavigationPrimitive(myNavigationTriangle as MyNavigationTriangle);
        }

        public override IMyHighLevelComponent GetComponent(MyHighLevelPrimitive highLevelPrimitive)
        {
            return new Component(this, highLevelPrimitive.Index);
        }

        public override void DebugDraw(ref Matrix drawMatrix)
        {
            if (!MyDebugDrawSettings.ENABLE_DEBUG_DRAW) return;

            base.DebugDraw(ref drawMatrix);

            if ((MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.EDGES) != 0)
            {
                if (m_connectionHelper != null)
                {
                    foreach (var edge in m_connectionHelper)
                    {
                        Vector3 A = Vector3.Transform(edge.Key.A / 256.0f, drawMatrix);
                        Vector3 B = Vector3.Transform(edge.Key.B / 256.0f, drawMatrix);
                        MyRenderProxy.DebugDrawLine3D(A, B, Color.Red, Color.Yellow, false);
                    }
                }
            }

            if ((MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.NORMALS) != 0)
            {
                foreach (var entry in m_smallTriangleRegistry)
                {
                    var list = entry.Value;
                    foreach (var triIndex in list)
                    {
                        var tri = GetTriangle(triIndex);
                        Vector3 normal = Vector3.Transform(tri.Center + tri.Normal * 0.2f, drawMatrix);
                        Vector3 center = Vector3.Transform(tri.Center, drawMatrix);
                        MyRenderProxy.DebugDrawLine3D(center, normal, Color.Blue, Color.Blue, true);
                    }
                }
            }

            if (MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY)
            {
                if (m_higherLevel != null)
                {
                    m_higherLevel.DebugDraw(lite: MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY_LITE);
                }
            }

            /*foreach (var entry in m_smallTriangleRegistry)
            {
                var list = entry.Value;
                foreach (var triIndex in list)
                {
                    var tri = GetTriangle(triIndex);
                    Vector3 normal = Vector3.Transform(tri.Center + tri.Normal * 0.2f, drawMatrix);
                    Vector3 center = Vector3.Transform(tri.Center, drawMatrix);
                    MyRenderProxy.DebugDrawText3D(center, entry.Key.ToString(), Color.Blue, 0.7f, true);
                }
            }*/

            /*
            if (m_cubeSet != null)
            {
                foreach (var entry in m_cubeSet)
                {
                    Vector3 pos = Vector3.Transform(entry, drawMatrix);
                    MyRenderProxy.DebugDrawSphere(pos, 0.1f, Color.Red, 1.0f, false);
                    MyRenderProxy.DebugDrawText3D(pos, entry.ToString(), Color.Red, 1.0f, false);
                }
            }*/
        }
    }
}
