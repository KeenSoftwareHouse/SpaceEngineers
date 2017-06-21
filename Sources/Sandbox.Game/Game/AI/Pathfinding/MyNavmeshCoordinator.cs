using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyNavmeshCoordinator
    {
        private static List<MyEntity> m_tmpEntityList = new List<MyEntity>();
        private static List<MyGridPathfinding.CubeId> m_tmpLinkCandidates = new List<MyGridPathfinding.CubeId>();
        private static List<MyNavigationTriangle> m_tmpNavTris = new List<MyNavigationTriangle>();
        private static List<MyNavigationPrimitive> m_tmpNavPrims = new List<MyNavigationPrimitive>(4);

        private MyGridPathfinding m_gridPathfinding;
        private MyVoxelPathfinding m_voxelPathfinding;
        private MyDynamicObstacles m_obstacles;

        // List of all linked triangles per a voxel mesh cell
        private Dictionary<MyVoxelPathfinding.CellId, List<MyNavigationPrimitive>> m_voxelLinkDictionary = new Dictionary<MyVoxelPathfinding.CellId, List<MyNavigationPrimitive>>();
        private Dictionary<MyGridPathfinding.CubeId, int> m_gridLinkCounter = new Dictionary<MyGridPathfinding.CubeId, int>();

        private MyNavgroupLinks m_links;
        private MyNavgroupLinks m_highLevelLinks;
        public MyNavgroupLinks Links { get { return m_links; } }
        public MyNavgroupLinks HighLevelLinks { get { return m_highLevelLinks; } }

        public MyNavmeshCoordinator(MyDynamicObstacles obstacles)
        {
            m_links = new MyNavgroupLinks();
            m_highLevelLinks = new MyNavgroupLinks();
            m_obstacles = obstacles;
        }

        public void SetGridPathfinding(MyGridPathfinding gridPathfinding)
        {
            m_gridPathfinding = gridPathfinding;
        }

        public void SetVoxelPathfinding(MyVoxelPathfinding myVoxelPathfinding)
        {
            m_voxelPathfinding = myVoxelPathfinding;
        }

        public void PrepareVoxelTriangleTests(BoundingBoxD cellBoundingBox, List<MyCubeGrid> gridsToTestOutput)
        {
            ProfilerShort.Begin("PrepareVoxelTriangleTests");

            m_tmpEntityList.Clear();

            // Each triangle will be tested with grids up to one largest cube further away from them, so we have to reflect this in the bounding box.
            float largeCubeSize = MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large);
            cellBoundingBox.Inflate(largeCubeSize);

            // Furthermore, a triangle cannot lie in a cube under existing block, so we have to extend the bbox even further
            if (MyPerGameSettings.NavmeshPresumesDownwardGravity)
            {
                var min = cellBoundingBox.Min;
                min.Y -= largeCubeSize;
                cellBoundingBox.Min = min;
            }

            MyGamePruningStructure.GetAllEntitiesInBox(ref cellBoundingBox, m_tmpEntityList);
            foreach (var entity in m_tmpEntityList)
            {
                var grid = entity as MyCubeGrid;
                if (grid == null) continue;

                if (!MyGridPathfinding.GridCanHaveNavmesh(grid)) continue;

                gridsToTestOutput.Add(grid);
            }

            m_tmpEntityList.Clear();

            ProfilerShort.End();
        }

        public void TestVoxelNavmeshTriangle(ref Vector3D a, ref Vector3D b, ref Vector3D c, List<MyCubeGrid> gridsToTest, List<MyGridPathfinding.CubeId> linkCandidatesOutput, out bool intersecting)
        {
            ProfilerShort.Begin("TestVoxelNavmeshTriangle");

            ProfilerShort.Begin("Triangle-obstacle tests");
            Vector3D s = (a + b + c) / 3.0;
            if (m_obstacles.IsInObstacle(s))
            {
                intersecting = true;
                ProfilerShort.End();
                ProfilerShort.End();
                return;
            }
            ProfilerShort.End();

            BoundingBoxD triBB;
            Vector3D aLocal, bLocal, cLocal, gLocal;
            Vector3D g = Vector3D.Zero;
            if (MyPerGameSettings.NavmeshPresumesDownwardGravity)
            {
                g = Vector3.Down * 2.0f;
            }

            m_tmpLinkCandidates.Clear();

            intersecting = false;
            foreach (var grid in gridsToTest)
            {
                MatrixD mat = grid.PositionComp.WorldMatrixNormalizedInv;

                Vector3D.Transform(ref a, ref mat, out aLocal);
                Vector3D.Transform(ref b, ref mat, out bLocal);
                Vector3D.Transform(ref c, ref mat, out cLocal);
                Vector3D.TransformNormal(ref g, ref mat, out gLocal);

                triBB = new BoundingBoxD(Vector3D.MaxValue, Vector3D.MinValue);
                triBB.Include(ref aLocal, ref bLocal, ref cLocal);
                
                Vector3I min = grid.LocalToGridInteger(triBB.Min);
                Vector3I max = grid.LocalToGridInteger(triBB.Max);
                Vector3I pos = min - Vector3I.One;
                Vector3I max2 = max + Vector3I.One;
                for (var it = new Vector3I_RangeIterator(ref pos, ref max2); it.IsValid(); it.GetNext(out pos))
                {
                    if (grid.GetCubeBlock(pos) != null)
                    {
                        Vector3 largeMin = (pos - Vector3.One) * grid.GridSize;
                        Vector3 largeMax = (pos + Vector3.One) * grid.GridSize;
                        Vector3 smallMin = (pos - Vector3.Half) * grid.GridSize;
                        Vector3 smallMax = (pos + Vector3.Half) * grid.GridSize;
                        BoundingBoxD largeBb = new BoundingBoxD(largeMin, largeMax);
                        BoundingBoxD bb = new BoundingBoxD(smallMin, smallMax);

                        largeBb.Include(largeMin + gLocal);
                        largeBb.Include(largeMax + gLocal);
                        bb.Include(smallMin + gLocal);
                        bb.Include(smallMax + gLocal);

                        ProfilerShort.Begin("Triangle intersection tests");
                        if (largeBb.IntersectsTriangle(ref aLocal, ref bLocal, ref cLocal))
                        {
                            if (bb.IntersectsTriangle(ref aLocal, ref bLocal, ref cLocal))
                            {
                                intersecting = true;
                                ProfilerShort.End();
                                break;
                            }
                            else
                            {
                                int dx = Math.Min(Math.Abs(min.X - pos.X), Math.Abs(max.X - pos.X));
                                int dy = Math.Min(Math.Abs(min.Y - pos.Y), Math.Abs(max.Y - pos.Y));
                                int dz = Math.Min(Math.Abs(min.Z - pos.Z), Math.Abs(max.Z - pos.Z));
                                if ((dx + dy + dz) < 3)
                                    m_tmpLinkCandidates.Add(new MyGridPathfinding.CubeId() { Grid = grid, Coords = pos });
                            }
                        }
                        ProfilerShort.End();
                    }
                }

                if (intersecting) break;
            }

            if (!intersecting)
            {
                for (int i = 0; i < m_tmpLinkCandidates.Count; ++i)
                {
                    linkCandidatesOutput.Add(m_tmpLinkCandidates[i]);
                }
            }
            m_tmpLinkCandidates.Clear();

            ProfilerShort.End();
        }

        // This is an old version of the function
        public void TryAddVoxelNavmeshLinks(MyNavigationTriangle addedPrimitive, MyVoxelPathfinding.CellId cellId, List<MyGridPathfinding.CubeId> linkCandidates)
        {
            ProfilerShort.Begin("TryAddVoxelNavmeshLinks");

            m_tmpNavTris.Clear();
            foreach (var candidate in linkCandidates)
            {
                // First, find closest navigation triangle from the given candidate cube
                ProfilerShort.Begin("Find closest grid nav tri");
                m_gridPathfinding.GetCubeTriangles(candidate, m_tmpNavTris);

                double closestDistSq = double.MaxValue;
                MyNavigationTriangle closestGridTri = null;

                foreach (var tri in m_tmpNavTris)
                {
                    Vector3D posDiff = addedPrimitive.WorldPosition - tri.WorldPosition;
                    if (MyPerGameSettings.NavmeshPresumesDownwardGravity)
                    {
                        if (Math.Abs(posDiff.Y) < 0.3)
                        {
                            if (posDiff.LengthSquared() < closestDistSq)
                            {
                                closestDistSq = posDiff.LengthSquared();
                                closestGridTri = tri;
                            }
                        }
                    }
                }
                ProfilerShort.End();

                if (closestGridTri != null)
                {
                    bool createLink = true;
                    var existingLinks = m_links.GetLinks(closestGridTri);
                    List<MyNavigationPrimitive> existingCellLinks = null;
                    m_voxelLinkDictionary.TryGetValue(cellId, out existingCellLinks);

                    if (existingLinks != null)
                    {
                        m_tmpNavPrims.Clear();
                        CollectClosePrimitives(addedPrimitive, m_tmpNavPrims, 2);
                        for (int i = 0; i < m_tmpNavPrims.Count; ++i)
                        {
                            if (existingLinks.Contains(m_tmpNavPrims[i]) && existingCellLinks != null && existingCellLinks.Contains(m_tmpNavPrims[i]))
                            {
                                double existingDistSq = (m_tmpNavPrims[i].WorldPosition - closestGridTri.WorldPosition).LengthSquared();
                                if (existingDistSq < closestDistSq)
                                {
                                    createLink = false;
                                    break;
                                }
                                else
                                {
                                    m_links.RemoveLink(closestGridTri, m_tmpNavPrims[i]);
                                    if (m_links.GetLinkCount(m_tmpNavPrims[i]) == 0)
                                    {
                                        RemoveVoxelLinkFromDictionary(cellId, m_tmpNavPrims[i]);
                                    }
                                    DecreaseGridLinkCounter(candidate);
                                    continue;
                                }
                            }
                        }
                        m_tmpNavPrims.Clear();
                    }

                    if (createLink)
                    {
                        m_links.AddLink(addedPrimitive, closestGridTri);
                        SaveVoxelLinkToDictionary(cellId, addedPrimitive);
                        IncreaseGridLinkCounter(candidate);
                    }
                }

                m_tmpNavTris.Clear();
            }

            ProfilerShort.End();
        }

        public void TryAddVoxelNavmeshLinks2(MyVoxelPathfinding.CellId cellId, Dictionary<MyGridPathfinding.CubeId, List<MyNavigationPrimitive>> linkCandidates)
        {
            ProfilerShort.Begin("TryAddVoxelNavmeshLinks");
            foreach (var entry in linkCandidates)
            {
                double closestDistSq = double.MaxValue;
                MyNavigationTriangle closestGridTri = null;
                MyNavigationPrimitive closestLinkedPrim = null;
                
                m_tmpNavTris.Clear();
                m_gridPathfinding.GetCubeTriangles(entry.Key, m_tmpNavTris);
                foreach (var tri in m_tmpNavTris)
                {
                    Vector3 a, b, c;
                    tri.GetVertices(out a, out b, out c);

                    a = tri.Parent.LocalToGlobal(a);
                    b = tri.Parent.LocalToGlobal(b);
                    c = tri.Parent.LocalToGlobal(c);

                    Vector3D normal = (c - a).Cross(b - a);
                    Vector3D center = (a + b + c) / 3.0f;
                    double lowerY = Math.Min(a.Y, Math.Min(b.Y, c.Y));
                    double upperY = Math.Max(a.Y, Math.Max(b.Y, c.Y));
                    lowerY -= 0.25f;
                    upperY += 0.25f;

                    foreach (var primitive in entry.Value)
                    {
                        Vector3D primPos = primitive.WorldPosition;
                        Vector3D offset = primPos - center;
                        double offsetLen = offset.Length();
                        offset = offset / offsetLen;
                        double dot; Vector3D.Dot(ref offset, ref normal, out dot);
                        if (dot > -0.2f && primPos.Y < upperY && primPos.Y > lowerY)
                        {
                            double dist = offsetLen / (dot + 0.3f);
                            if (dist < closestDistSq)
                            {
                                closestDistSq = dist;
                                closestGridTri = tri;
                                closestLinkedPrim = primitive;
                            }
                        }
                    }
                }
                m_tmpNavTris.Clear();

                if (closestGridTri != null)
                {
                    Debug.Assert(closestLinkedPrim.GetHighLevelPrimitive() != null);
                    Debug.Assert(closestGridTri.GetHighLevelPrimitive() != null);
                    m_links.AddLink(closestLinkedPrim, closestGridTri);
                    SaveVoxelLinkToDictionary(cellId, closestLinkedPrim);
                    IncreaseGridLinkCounter(entry.Key);
                }
            }
            ProfilerShort.End();
        }

        public void RemoveVoxelNavmeshLinks(MyVoxelPathfinding.CellId cellId)
        {
            List<MyNavigationPrimitive> list = null;
            if (!m_voxelLinkDictionary.TryGetValue(cellId, out list))
            {
                return;
            }

            foreach (var primitive in list)
            {
                m_links.RemoveAllLinks(primitive);
            }

            m_voxelLinkDictionary.Remove(cellId);
        }

        public void RemoveGridNavmeshLinks(MyCubeGrid grid)
        {
            var navmesh = m_gridPathfinding.GetNavmesh(grid);
            if (navmesh == null)
            {
                Debug.Assert(false, "Navmesh for a grid not found!");
                return;
            }

            m_tmpNavPrims.Clear();

            var enumerator = navmesh.GetCubes();
            while (enumerator.MoveNext())
            {
                MyGridPathfinding.CubeId cubeId = new MyGridPathfinding.CubeId() { Grid = grid, Coords = enumerator.Current };
                int counter;
                if (m_gridLinkCounter.TryGetValue(cubeId, out counter))
                {
                    m_tmpNavTris.Clear();
                    navmesh.GetCubeTriangles(enumerator.Current, m_tmpNavTris);

                    foreach (var tri in m_tmpNavTris)
                    {
                        m_links.RemoveAllLinks(tri);

                        var hlPrim = tri.GetHighLevelPrimitive();
                        if (!m_tmpNavPrims.Contains(hlPrim))
                        {
                            m_tmpNavPrims.Add(hlPrim);
                        }
                    }

                    m_tmpNavTris.Clear();
                    m_gridLinkCounter.Remove(cubeId);
                }
            }
            enumerator.Dispose();

            foreach (var highLevelPrim in m_tmpNavPrims)
            {
                m_highLevelLinks.RemoveAllLinks(highLevelPrim);
            }
            m_tmpNavPrims.Clear();
        }

        private void SaveVoxelLinkToDictionary(MyVoxelPathfinding.CellId cellId, MyNavigationPrimitive linkedPrimitive)
        {
            List<MyNavigationPrimitive> list = null;
            if (!m_voxelLinkDictionary.TryGetValue(cellId, out list))
            {
                // CH:TODO: take these from pre-allocated pools
                list = new List<MyNavigationPrimitive>();
            }
            else if (list.Contains(linkedPrimitive))
            {
                // Avoid duplicates
                return;
            }

            list.Add(linkedPrimitive);
            m_voxelLinkDictionary[cellId] = list;
        }

        private void RemoveVoxelLinkFromDictionary(MyVoxelPathfinding.CellId cellId, MyNavigationPrimitive linkedPrimitive)
        {
            List<MyNavigationPrimitive> list = null;
            if (!m_voxelLinkDictionary.TryGetValue(cellId, out list))
            {
                Debug.Assert(false, "Could not find a removed voxel link in the dictionary!");
                return;
            }
            else
            {
                bool retval = list.Remove(linkedPrimitive);
                Debug.Assert(retval == true, "Couldn't remove a linked triangle from the dictionary!");
                if (list.Count == 0)
                {
                    m_voxelLinkDictionary.Remove(cellId);
                }
            }
        }

        private void IncreaseGridLinkCounter(MyGridPathfinding.CubeId candidate)
        {
            int counter = 0;
            if (!m_gridLinkCounter.TryGetValue(candidate, out counter))
            {
                counter = 1;
            }
            else
            {
                counter++;
            }
            m_gridLinkCounter[candidate] = counter;
        }

        private void DecreaseGridLinkCounter(MyGridPathfinding.CubeId candidate)
        {
            int counter = 0;
            if (!m_gridLinkCounter.TryGetValue(candidate, out counter))
            {
                Debug.Assert(false, "Grid link counter is inconsistent!");
                return;
            }
            else
            {
                counter--;
            }

            if (counter == 0)
            {
                m_gridLinkCounter.Remove(candidate);
            }
            else
            {
                m_gridLinkCounter[candidate] = counter;
            }
        }

        private void CollectClosePrimitives(MyNavigationPrimitive addedPrimitive, List<MyNavigationPrimitive> output, int depth)
        {
            if (depth < 0) return;

            ProfilerShort.Begin("CollectClosePrimitives");

            int prevPrevDepthBegin = output.Count;
            output.Add(addedPrimitive);
            int prevDepthBegin = output.Count;
            for (int i = 0; i < addedPrimitive.GetOwnNeighborCount(); ++i)
            {
                var neighbor = addedPrimitive.GetOwnNeighbor(i) as MyNavigationPrimitive;
                if (neighbor != null)
                    output.Add(neighbor);
            }
            int prevDepthEnd = output.Count;
            depth--;

            while (depth > 0)
            {
                for (int p = prevDepthBegin; p < prevDepthEnd; ++p)
                {
                    MyNavigationPrimitive primitive = output[p];
                    for (int i = 0; i < primitive.GetOwnNeighborCount(); ++i)
                    {
                        var neighbor = primitive.GetOwnNeighbor(i) as MyNavigationPrimitive;
                        bool alreadyAdded = false;
                        for (int j = prevPrevDepthBegin; j < prevDepthEnd; ++j)
                        {
                            if (output[j] == neighbor)
                            {
                                alreadyAdded = true;
                                break;
                            }
                        }
                        if (!alreadyAdded && neighbor != null)
                            output.Add(neighbor);
                    }
                }

                prevPrevDepthBegin = prevDepthBegin;
                prevDepthBegin = prevDepthEnd;
                prevDepthEnd = output.Count;

                depth--;
            }

            ProfilerShort.End();
        }

        public void UpdateVoxelNavmeshCellHighLevelLinks(MyVoxelPathfinding.CellId cellId)
        {
            // Make sure links are where they should be
            List<MyNavigationPrimitive> linkedTriangles = null;
            if (m_voxelLinkDictionary.TryGetValue(cellId, out linkedTriangles))
            {
                MyNavigationPrimitive hlPrimitive1 = null;
                MyNavigationPrimitive hlPrimitive2 = null;

                foreach (var primitive in linkedTriangles)
                {
                    hlPrimitive1 = primitive.GetHighLevelPrimitive();

                    List<MyNavigationPrimitive> otherLinkedPrimitives = null;
                    otherLinkedPrimitives = m_links.GetLinks(primitive);
                    if (otherLinkedPrimitives != null)
                    {
                        foreach (var otherPrimitive in otherLinkedPrimitives)
                        {
                            hlPrimitive2 = otherPrimitive.GetHighLevelPrimitive();
                            m_highLevelLinks.AddLink(hlPrimitive1, hlPrimitive2, onlyIfNotPresent: true);
                        }
                    }
                }
            }

            // CH: TODO: Make sure that links are not where they should not be
        }

        public void InvalidateVoxelsBBox(ref BoundingBoxD bbox)
        {
            m_voxelPathfinding.InvalidateBox(ref bbox);
        }

        public void DebugDraw()
        {
            if (!MyFakes.DEBUG_DRAW_NAVMESH_LINKS)
            {
                return;
            }

            if (MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY)
            {
                foreach (var entry in m_voxelLinkDictionary)
                {
                    var voxelMap = entry.Key.VoxelMap;
                    Vector3I pos = entry.Key.Pos;

                    BoundingBoxD cellBB = new BoundingBoxD();
                    MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(voxelMap.PositionLeftBottomCorner, ref pos, out cellBB);
                    Vector3D textPos = cellBB.Center;
                    MyRenderProxy.DebugDrawText3D(textPos, "LinkNum: " + entry.Value.Count, Color.Red, 1.0f, false);

                }
            }
        }
    }
}
