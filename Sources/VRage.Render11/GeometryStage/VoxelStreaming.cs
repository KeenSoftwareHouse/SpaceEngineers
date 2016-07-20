//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using SharpDX;
//using SharpDX.Direct3D;
//using SharpDX.Direct3D11;
//using VRageMath;
//using RectangleF = VRageMath.RectangleF;
//using Vector2 = VRageMath.Vector2;
//using Vector3 = VRageMath.Vector3;
//using Color = VRageMath.Color;
//using Matrix = VRageMath.Matrix;
//using BoundingSphere = VRageMath.BoundingSphere;
//using BoundingBox = VRageMath.BoundingBox;
//using BoundingFrustum = VRageMath.BoundingFrustum;
//using VRageRender.Vertex;
//using VRageMath.PackedVector;
//

//namespace VRageRender
//{
//    struct MyVoxelCellQuery
//    {
//        internal Vector3I Coord;
//        internal int LOD;
//    }

//    struct MyVoxelCellOcclusionQuery
//    {
//        internal Vector3I Coord;
//        internal int LOD;
//        internal MyOcclusionQuery Query;
//    }

//    class MyVoxelCellStreamer
//    {
//        int m_levelLimit = 5;
//        int[] m_radii = new[] { 0, 32, 64, 128, 256, 512, 1024, 2048 };
//        int m_initStage;
//        float m_cellSize;
//        Vector3 m_currentPosition;

//        List<MyVoxelCellQuery>[] m_buffers;
//        Dictionary<Vector3I, bool>[] m_requestsStatus;
//        List<MyVoxelCellOcclusionQuery>[] m_oqList;

//        bool IsInvalid(Vector3I coord, int LOD)
//        {
//            return true;
//        }

//        float GetLevelRadius(int level)
//        {
//            return m_radii[level];
//        }

//        bool IsLevelReady(int L)
//        {
//            var values = m_requestsStatus[L].Values;
//            for (int i = 0; i < values.Count; i++)
//            {
//                if (!values.ElementAt(i))
//                    return false;
//            }
//            return true;
//        }

//        void AskForCell(Vector3I coord, int LOD)
//        {
//            m_requestsStatus[LOD][coord] = false;
//            // TODO:
//            // SEND TO GAME
//        }

//        void ProcessResponse(Vector3I coord, int LOD, bool data)
//        {
//            m_requestsStatus[LOD][coord] = true;
//        }

//        bool IsInInitStage()
//        {
//            return m_initStage < m_levelLimit * 2 + 1;
//        }

//        void GatherQueries()
//        {
//            for (int L = 0; L < m_levelLimit; L++)
//            {

//                var list = m_oqList[L];
//                // request non-occluded cells
//                for (int i = 0; i < list.Count; i++)
//                {
//                    int num;
//                    if (list[i].Query.GetResult(out num, true) && num > 0)
//                    {
//                        AskForCell(list[i].Coord, list[i].LOD);
//                    }
//                    list[i].Query.Destroy();
//                    list[i] = new MyVoxelCellOcclusionQuery();
//                }
//                list.RemoveAll(x => x.Query == null);
//            }
//        }

//        void StartLevelUpdate(int L)
//        {
//            var buffer = m_buffers[L];

//            buffer.Clear();
//            GetLevelCells(L, buffer, (Vector3)MyRender11.Environment.ViewFrustum);

//            for (int i = 0; i < buffer.Count; i++)
//            {
//                BoundingBox box = GetCellBB(buffer[i].Coord, L);

//                if (IsInvalid(buffer[i].Coord, buffer[i].LOD))
//                {
//                    if (MyRender11.Environment.ViewFrustum.Near.Intersects(box) == VRageMath.PlaneIntersectionType.Intersecting)
//                    {
//                        AskForCell(buffer[i].Coord, buffer[i].LOD);
//                    }
//                    else
//                    {
//                        // issue query
//                        var oq = new MyVoxelCellOcclusionQuery();
//                        oq.Coord = buffer[i].Coord;
//                        oq.LOD = buffer[i].LOD;
//                        oq.Query = MyQueryFactory.CreateOcclusionQuery();

//                        oq.Query.Begin();
//                        // TODO: render AABB 
//                        oq.Query.End();

//                        m_oqList[L].Add(oq);
//                    }
//                }
//            }
//        }

//        void InitialQuerying()
//        {
//            if (m_initStage == 0)
//            {
//                var buffer = m_buffers[0];

//                buffer.Clear();
//                GetLevelCells(0, buffer, null);

//                for (int i = 0; i < buffer.Count; i++)
//                {
//                    if (IsInvalid(buffer[i].Coord, buffer[i].LOD))
//                        AskForCell(buffer[i].Coord, buffer[i].LOD); // store cell as requested for
//                }
//            }
//            else
//            {
//                // occlusion and request stages, one after another 
//                bool occlusionStage = (m_initStage % 2) == 1;
//                int processedLevel = (m_initStage + 1) / 2 + 1;
//                int prevLevel = processedLevel - 1;
//                var buffer = m_buffers[processedLevel];

//                if (occlusionStage)
//                {
//                    if (IsLevelReady(prevLevel)) // proceed
//                    {
//                        // NOW -> using camera projection
//                        // LATER -> omnidirectional (?)

//                        BoundingFrustum frustum = MyRender11.Environment.ViewFrustum;

//                        buffer.Clear();
//                        GetLevelCells(processedLevel, buffer, frustum);

//                        for (int i = 0; i < buffer.Count; i++)
//                        {
//                            BoundingBox box = GetCellBB(buffer[i].Coord, processedLevel);

//                            if (IsInvalid(buffer[i].Coord, buffer[i].LOD))
//                            {
//                                if (frustum.Near.Intersects(box) == VRageMath.PlaneIntersectionType.Intersecting)
//                                {
//                                    AskForCell(buffer[i].Coord, buffer[i].LOD);
//                                }
//                                else
//                                {
//                                    // issue query
//                                    var oq = new MyVoxelCellOcclusionQuery();
//                                    oq.Coord = buffer[i].Coord;
//                                    oq.LOD = buffer[i].LOD;
//                                    oq.Query = MyQueryFactory.CreateOcclusionQuery();

//                                    oq.Query.Begin();
//                                    // TODO: render AABB 
//                                    oq.Query.End();

//                                    m_oqList[processedLevel].Add(oq);
//                                }
//                            }
//                        }

//                        m_initStage++;
//                    }
//                    else
//                    {
//                        // skipping work till renderer gets all requested cells...
//                    }
//                }
//                else
//                {
//                    var list = m_oqList[processedLevel];
//                    // request non-occluded cells
//                    for (int i = 0; i < list.Count; i++)
//                    {
//                        int num;
//                        if (list[i].Query.GetResult(out num, true) && num > 0)
//                        {
//                            AskForCell(list[i].Coord, list[i].LOD);
//                        }
//                        list[i].Query.Destroy();
//                    }
//                    list.Clear();


//                    m_initStage++;
//                }

//            }
//        }

//        BoundingBox GetCellBB(Vector3I coord, int L)
//        {
//            float LCellSize = m_cellSize * (float)Math.Pow(2, L);
//            return new BoundingBox(coord * LCellSize, coord * LCellSize + LCellSize);
//        }

//        void GetLevelCells(int L, List<MyVoxelCellQuery> result, BoundingFrustum frustum = null)
//        {
//            float LCellSize = m_cellSize * (float)Math.Pow(2, L);

//            var PPosition = m_currentPosition.Snap(LCellSize);
//            var LPosition = m_currentPosition.Snap(LCellSize * 2);

//            float PRadius = GetLevelRadius(L);
//            float LRadius = GetLevelRadius(L + 1);


//            var pmin = (LPosition - PRadius - LCellSize * 2).Snap(LCellSize * 2);
//            var pmax = (LPosition + PRadius + LCellSize * 2).Snap(LCellSize * 2);

//            for (var x = pmin.X; x < pmax.X; x += LCellSize)
//            {
//                for (var y = pmin.Y; y < pmax.Y; y += LCellSize)
//                {
//                    for (var z = pmin.Z; z < pmax.Z; z += LCellSize)
//                    {
//                        var cellMin = new Vector3(x, y, z);
//                        var cellPack = cellMin.Snap(L * 2);

//                        var inPLevel = (cellMin - PRadius).Length() < PRadius;
//                        var inLevel = (cellPack - LRadius).Length() < LRadius;

//                        var frustumCulled = frustum != null
//                            ? frustum.Contains(new BoundingBox(cellMin, cellMin + LCellSize)) == VRageMath.ContainmentType.Disjoint
//                            : false;

//                        if (inLevel && !inPLevel && !frustumCulled)
//                        {
//                            var q = new MyVoxelCellQuery();
//                            q.Coord = cellMin.AsCoord(LCellSize);
//                            q.LOD = L;
//                            result.Add(q);
//                        }
//                    }
//                }
//            }
//        }
//    }
//}
