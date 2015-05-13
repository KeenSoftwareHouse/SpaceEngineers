using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.VoxelMaps;
using Sandbox.Game.Entities.VoxelMaps.Voxels;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SysUtils.Utils;
using VRageMath;
using VRageRender;

//  Caching voxel cell vertex buffers.
//
//  Voxel cell cache implemented as preallocated array of cached cells - that can be accessed be key (calculated from voxel map Id and cell coordinates).
//  All methods are O(1). Class can hold cached cells for any voxel map.
//
//  At the beginning, cell caches are preallocated (they contain vertex buffer, etc). Later when user wants to display particular cell, he looks
//  into the cache if that cell isn't already calculated. If yes, he gets reference and can draw its vertex buffer. If not, he asks for one preallocated
//  cell - and he is given cell with lowest priority.
//  After that, he updates the cell - that means, he tells he needed this cell in cache so it should be stored for future (cell is given higher priority and
//  there is less chance it's removed in near future).
//  If he decides to remove cell from cache (e.g. after explosion), cell is freed and marked as available for new allocation - its priority is lowered, so
//  it's very probable it will be allocated when next time new cell will be needed.
//
//  Using this approach, frequently drawn cells are almost always in the cache. As user moves through the level, older cells are removed and new are added.
//  If cell is changed, we need to invalidate it in cache and use it's place for another cell (or same).
//  Remember: size of cache is constant, so this is sort of a better circular buffer (just with priorities, etc).
//
//  Added 8.6.2008: Now I can decide if one render-cell-element stored LOD0 cell data or LOD1 version. So one render cell can be stored in render cache
//  two times - once for detail version LOD0 and once for LOD1 version. But they are separate, so if we don't need LOD1 version, it may be freed and not be
//  calculated until we need it again. E.g. if player is near voxel map to which he is making a tunnel, we are updating LOD0 cached version. All players
//  that are in distance of this voxel map update just their LOD1 version, which is fast.
namespace Sandbox.Game.Voxels
{
    //  One cell stored in a cache
    class MyVoxelCacheCellRender
    {
        public MyVoxelMap VoxelMap;
        public Vector3 Center;          //  We need render cell center for sorting by distance
        public Vector3I CellCoord;
        public MyLodTypeEnum CellHashType;        //  This will tell us if this render-cell-item stores normal data or LOD1
        public bool Contains;       //  True if this render cell cache actualy stores some render cell. False if not and is waiting for its time (because it was just reseted)

        private Vector3 m_positionScale;
        private Vector3 m_positionOffset;

        public MyVoxelCacheCellRender()
        {
            Contains = false;
        }

        public void Begin(MyVoxelMap voxelMap, ref Vector3I cellCoord)
        {
            VoxelMap = voxelMap;
            CellCoord = cellCoord;
            Center = voxelMap.GetRenderCellPositionAbsolute(ref cellCoord) + MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES_HALF;
            Contains = true;
            m_positionScale = MyVoxelCacheRender.CellVertexPositionScale;
            m_positionOffset = MyVoxelCacheRender.CellVertexPositionOffset(ref cellCoord);
            MyVoxelCacheCellRenderHelper.Begin();
        }

        public void End()
        {

            foreach (MySingleMaterialHelper materialHelper in MyVoxelCacheCellRenderHelper.GetSingleMaterialHelpers())
            {
                if (materialHelper != null && materialHelper.IndexCount > 0)
                    EndSingleMaterial(materialHelper);
            }

            foreach (var pair in MyVoxelCacheCellRenderHelper.GetMultiMaterialHelpers())
            {
                if (pair.Value.VertexCount > 0)
                    EndMultiMaterial(pair.Value);
            }
        }

        void EndSingleMaterial(MySingleMaterialHelper materialHelper)
        {
            //Synchronize to VRage render
            if (materialHelper.IndexCount > 0 && materialHelper.VertexCount > 0)
            {
                //Todo - is it possible without allocations?
                MyVertexFormatVoxelSingleData[] vertices = new MyVertexFormatVoxelSingleData[materialHelper.VertexCount];
                Array.Copy(materialHelper.Vertices, vertices, vertices.Length);
                short[] indices = new short[materialHelper.IndexCount];
                Array.Copy(materialHelper.Indices, indices, indices.Length);

                VRageRender.MyRenderProxy.UpdateRenderVoxelCell(
                    VoxelMap.GetRenderObjectID(ref this.CellCoord),
                    (VRageRender.MyLodTypeEnum)(int)CellHashType,
                    vertices,
                    indices,
                    (int)materialHelper.Material.Index,
                    -1,
                    -1);
            }

            //  Reset helper arrays, so we can start adding triangles to them again
            materialHelper.IndexCount = 0;
            materialHelper.VertexCount = 0;
            MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookupCount[(int)materialHelper.Material.Index]++;
        }

        void EndMultiMaterial(MyMultiMaterialHelper helper)
        {
            if (helper.VertexCount > 0)
            {
                //Todo - is it possible without allocations?
                MyVertexFormatVoxelSingleData[] vertices = new MyVertexFormatVoxelSingleData[helper.VertexCount];
                Array.Copy(helper.Vertices, vertices, vertices.Length);

                short[] indices = new short[helper.VertexCount];
                for (short i = 0; i < indices.Length; i++)
                {
                    indices[i] = i;
                }

                VRageRender.MyRenderProxy.UpdateRenderVoxelCell(
                    VoxelMap.GetRenderObjectID(ref this.CellCoord),
                    (VRageRender.MyLodTypeEnum)(int)CellHashType,
                    vertices,
                    indices,
                    (int)helper.Material0.Index,
                    (int)helper.Material1.Index,
                    (int)helper.Material2.Index);
            }

            //  Reset helper arrays, so we can start adding triangles to them again
            helper.VertexCount = 0;
        }

        public static int GetMultimaterialId(int i0, int i1, int i2)
        {
            if (i0 > i1)
            {
                MyUtils.Swap(ref i0, ref i1);
            }
            if (i1 > i2)
            {
                MyUtils.Swap(ref i1, ref i2);
            }
            if (i0 > i1)
            {
                MyUtils.Swap(ref i0, ref i1);
            }
            var voxelMaterialCount = MyDefinitionManager.Static.VoxelMaterialCount;
            return i0 + i1 * voxelMaterialCount + i2 * voxelMaterialCount * voxelMaterialCount;
        }

        public void Reset()
        {
            Contains = false;
        }

        //  This method adds triangles from one data cell into this render cell. Single-texture triangles are added using indices (so we use m_notCompressedIndex buffer).
        //  For this we need to find indices. We use lookup array for it.
        //  Now we support only 16-bit indices, so vertex buffer can't have more then short.MaxValue vertices.
        public void AddTriangles(List<MyVoxelGeometry.CellData> cacheDataArray)
        {
            foreach (var cacheData in cacheDataArray)
            {
                //  Increase lookup count, so we will think that all vertexes in helper arrays are new
                for (int i = 0; i < MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookupCount.Length; i++)
                {
                    MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookupCount[i]++;
                }

                for (int i = 0; i < cacheData.VoxelTrianglesCount; i++)
                {
                    MyVoxelTriangle triangle = cacheData.VoxelTriangles[i];
                    MyVoxelVertex vertex0, vertex1, vertex2;
                    cacheData.GetUnpackedVertex(triangle.VertexIndex0, out vertex0);
                    cacheData.GetUnpackedVertex(triangle.VertexIndex1, out vertex1);
                    cacheData.GetUnpackedVertex(triangle.VertexIndex2, out vertex2);

                    vertex0.Position = (vertex0.Position - m_positionOffset) / m_positionScale;
                    vertex1.Position = (vertex1.Position - m_positionOffset) / m_positionScale;
                    vertex2.Position = (vertex2.Position - m_positionOffset) / m_positionScale;
                    Debug.Assert(vertex0.Position.IsInsideInclusive(ref Vector3.Zero, ref Vector3.One));
                    Debug.Assert(vertex1.Position.IsInsideInclusive(ref Vector3.Zero, ref Vector3.One));
                    Debug.Assert(vertex2.Position.IsInsideInclusive(ref Vector3.Zero, ref Vector3.One));

                    if ((vertex0.Material == vertex1.Material) && (vertex0.Material == vertex2.Material))
                    {
                        var matDef = MyDefinitionManager.Static.GetVoxelMaterialDefinition((byte)vertex0.Material);

                        //  This is single-texture triangleVertexes, so we can choose material from any edge
                        MySingleMaterialHelper materialHelper = MyVoxelCacheCellRenderHelper.GetForMaterial(matDef);

                        //  Add vertex0 to vertex buffer
                        AddVertexToBuffer(materialHelper, ref vertex0, matDef.Index, triangle.VertexIndex0);

                        //  Add vertex1 to vertex buffer
                        AddVertexToBuffer(materialHelper, ref vertex1, matDef.Index, triangle.VertexIndex1);

                        //  Add vertex2 to vertex buffer
                        AddVertexToBuffer(materialHelper, ref vertex2, matDef.Index, triangle.VertexIndex2);

                        MyVoxelCacheCellRenderHelper.CreateArrayIfNotExist(matDef.Index);

                        //  Add indices
                        int nextTriangleIndex = materialHelper.IndexCount;
                        materialHelper.Indices[nextTriangleIndex + 0] = MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookup[matDef.Index][triangle.VertexIndex0].VertexIndex;
                        materialHelper.Indices[nextTriangleIndex + 1] = MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookup[matDef.Index][triangle.VertexIndex1].VertexIndex;
                        materialHelper.Indices[nextTriangleIndex + 2] = MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookup[matDef.Index][triangle.VertexIndex2].VertexIndex;
                        materialHelper.IndexCount += 3;

                        if ((materialHelper.VertexCount >= MyVoxelCacheCellRenderHelper.MAX_VERTICES_COUNT_STOP) ||
                            (materialHelper.IndexCount >= MyVoxelCacheCellRenderHelper.MAX_INDICES_COUNT_STOP))
                        {
                            //  If this batch is almost full (or is full), we end it and start with new one
                            EndSingleMaterial(materialHelper);
                        }
                    }
                    else
                    {
                        int id = GetMultimaterialId(vertex0.Material, vertex1.Material, vertex2.Material);
                        // Assign current material
                        MyMultiMaterialHelper multiMaterialHelper = MyVoxelCacheCellRenderHelper.GetForMultimaterial(vertex0.Material, vertex1.Material, vertex2.Material);

                        // Copy packed normals
                        multiMaterialHelper.Vertices[multiMaterialHelper.VertexCount + 0].Normal = vertex0.Normal;
                        multiMaterialHelper.Vertices[multiMaterialHelper.VertexCount + 1].Normal = vertex0.Normal;
                        multiMaterialHelper.Vertices[multiMaterialHelper.VertexCount + 2].Normal = vertex0.Normal;

                        multiMaterialHelper.AddVertex(ref vertex0);
                        multiMaterialHelper.AddVertex(ref vertex1);
                        multiMaterialHelper.AddVertex(ref vertex2);

                        if (multiMaterialHelper.VertexCount >= MyVoxelCacheCellRenderHelper.MAX_VERTICES_COUNT_STOP)
                        {
                            EndMultiMaterial(multiMaterialHelper);
                        }
                    }
                }
            }
        }

        private static void AddVertexToBuffer(MySingleMaterialHelper materialHelper, ref MyVoxelVertex vertex0,
            int matIndex, short vertexIndex0)
        {
            MyVoxelCacheCellRenderHelper.CreateArrayIfNotExist(matIndex);

            if (MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookup[matIndex][vertexIndex0].CalcCounter !=
                MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookupCount[matIndex])
            {
                int nextVertexIndex = materialHelper.VertexCount;

                //  Short overflow check
                System.Diagnostics.Debug.Assert(nextVertexIndex <= short.MaxValue);

                // copy position and ambient
                materialHelper.Vertices[nextVertexIndex].Position = vertex0.Position;
                materialHelper.Vertices[nextVertexIndex].Ambient = vertex0.Ambient;

                // Copy normal
                materialHelper.Vertices[nextVertexIndex].Normal = vertex0.Normal;

                MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookup[matIndex][vertexIndex0].CalcCounter =
                    MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookupCount[matIndex];
                MyVoxelCacheCellRenderHelper.SingleMaterialIndicesLookup[matIndex][vertexIndex0].VertexIndex =
                    (short)nextVertexIndex;

                materialHelper.VertexCount++;
            }
        }
    }

    //  Container of all cells stored in voxel cache
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class MyVoxelCacheRender : MySessionComponentBase
    {

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //  Access cached cell by key
        static Dictionary<Int64, LinkedListNode<MyVoxelCacheCellRender>> m_cellsByCoordinate = null;

        //  Here we preallocate cell caches
        static MyVoxelCacheCellRender[] m_cellsPreallocated = null;

        //  Linked list used to allocate, remove and update voxel cell in O(1) time.
        static LinkedList<MyVoxelCacheCellRender> m_priority;
        static LinkedListNode<MyVoxelCacheCellRender>[] m_priorityArray;

        //  Capacity of this cell cache
        static int m_capacity;

        //  Used for creating LOD1 version only
        static MyVoxelGeometry.CellData m_helperLodCachedDataCell;

        static List<MyVoxelGeometry.CellData> m_dataCellsQueue;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public override void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyVoxelCacheRender.LoadData");

            MySandboxGame.Log.WriteLine("MyVoxelCacheRender.LoadData() - START");
            MySandboxGame.Log.IncreaseIndent();

            m_capacity = MyVoxelConstants.RENDER_CELL_CACHE_SIZE;
            m_cellsByCoordinate = new Dictionary<Int64, LinkedListNode<MyVoxelCacheCellRender>>(m_capacity);
            m_priority = new LinkedList<MyVoxelCacheCellRender>();
            m_priorityArray = new LinkedListNode<MyVoxelCacheCellRender>[m_capacity];
            m_cellsPreallocated = new MyVoxelCacheCellRender[m_capacity];
            m_helperLodCachedDataCell = new MyVoxelGeometry.CellData();
            m_dataCellsQueue = new List<MyVoxelGeometry.CellData>(MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS_TOTAL);
            for (int i = 0; i < m_capacity; i++)
            {
                m_cellsPreallocated[i] = new MyVoxelCacheCellRender();
                m_priorityArray[i] = new LinkedListNode<MyVoxelCacheCellRender>(m_cellsPreallocated[i]);
                m_priority.AddLast(m_priorityArray[i]);
            }
            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVoxelCacheRender.LoadData() - END");
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            LoadCache();
        }

        protected override void UnloadData()
        {
            UnloadCache();

            m_cellsPreallocated = null;
            m_priority = null;
            m_priorityArray = null;
            if (m_dataCellsQueue != null)
            {
                m_dataCellsQueue.Clear();
            }
            m_helperLodCachedDataCell = null;
        }

        private void LoadCache()
        {
            MySandboxGame.Log.WriteLine("MyVoxelCacheRender.LoadContent() - START");
            MySandboxGame.Log.IncreaseIndent();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyVoxelCacheRender::LoadContent");

            foreach (KeyValuePair<Int64, LinkedListNode<MyVoxelCacheCellRender>> kvp in m_cellsByCoordinate)
            {
                m_priority.Remove(kvp.Value.Value);
                m_priority.AddFirst(kvp.Value.Value);
            }
            m_cellsByCoordinate.Clear();//forces to recreate all rendering cells on next call

            //this is here becouse it worked and is probably right way how to invalidate this stuff (really dont know why)
            foreach (MyVoxelMap map in MySession.Static.VoxelMaps.GetVoxelMaps())
            {
                map.InvalidateCache(new Vector3I(-1000, -1000, -1000), new Vector3I(1000, 1000, 1000));
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVoxelCacheRender.LoadContent() - END");
        }

        private void UnloadCache()
        {
            MySandboxGame.Log.WriteLine("MyVoxelCacheRender.UnloadContent - START");
            MySandboxGame.Log.IncreaseIndent();

            if (m_cellsByCoordinate != null)
            {
                m_cellsByCoordinate.Clear();
            }

            if (m_cellsPreallocated != null)
            {
                for (int i = 0; i < m_cellsPreallocated.Length; i++)
                {
                    if (m_cellsPreallocated[i] != null)
                    {
                        //  Dispose vertex buffers
                        m_cellsPreallocated[i].Reset();
                    }
                }
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVoxelCacheRender.UnloadContent - END");
        }

        public static MyVoxelCacheCellRender GetCell(MyVoxelMap voxelMap, ref Vector3I renderCellCoord, MyLodTypeEnum cellHashType)
        {
            MyVoxelCacheCellRender ret = GetCellFromCache(voxelMap.VoxelMapId, ref renderCellCoord, cellHashType);

            if (ret == null)
            {
                ret = LoadCell(voxelMap, ref renderCellCoord, cellHashType);
            }

            if (ret != null)
            {
                UpdateCell(voxelMap.VoxelMapId, ref renderCellCoord, cellHashType);
            }

            return ret;
        }

        private static MyVoxelCacheCellRender LoadCell(MyVoxelMap voxelMap, ref Vector3I renderCellCoord, MyLodTypeEnum cellHashType)
        {
            Profiler.Begin("AddCell");

            MyVoxelCacheCellRender ret = AddCell(voxelMap.VoxelMapId, ref renderCellCoord, cellHashType);
            ret.Begin(voxelMap, ref renderCellCoord);
            ret.CellHashType = cellHashType;

            Profiler.End();

            if (cellHashType == MyLodTypeEnum.LOD0)
            {
                Profiler.Begin("LOD0 - queue cells");
                m_dataCellsQueue.Clear();

                //  Create normal (LOD0) version
                for (int dataX = 0; dataX < MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS; dataX++)
                {
                    for (int dataY = 0; dataY < MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS; dataY++)
                    {
                        for (int dataZ = 0; dataZ < MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS; dataZ++)
                        {
                            //  Don't precalculate this cells now. Store it in queue and calculate all cells at once by MyVoxelPrecalc.PrecalcQueue()
                            Vector3I dataCellCoord =
                                new Vector3I(
                                    renderCellCoord.X * MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS + dataX,
                                    renderCellCoord.Y * MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS + dataY,
                                    renderCellCoord.Z * MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS + dataZ);
                            MyVoxelGeometry.CellData cachedDataCell = voxelMap.Geometry.GetCellLater(MyLodTypeEnum.LOD0, ref dataCellCoord);
                            if (cachedDataCell != null)
                            {
                                m_dataCellsQueue.Add(cachedDataCell);
                            }
                        }
                    }
                }

                Profiler.BeginNextBlock("LOD0 - PrecalcQueue");

                //  Precalculate all queued data cells in parallel threads - using multiple cores if possible.
                MyVoxelPrecalc.PrecalcQueue();

                Profiler.BeginNextBlock("LOD0 - AddTriangles");
                ret.AddTriangles(m_dataCellsQueue);
                Profiler.End();
            }
            else if (cellHashType == MyLodTypeEnum.LOD1)
            {
                Profiler.Begin("LOD1 - PrecalcImmediatelly");

                m_helperLodCachedDataCell.Reset();

                //  Create LOD1 render cell
                MyVoxelPrecalc.PrecalcImmediatelly(
                    new MyVoxelPrecalcTaskItem(
                        MyLodTypeEnum.LOD1,
                        voxelMap,
                        m_helperLodCachedDataCell,
                        new Vector3I(
                            renderCellCoord.X * MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS,
                            renderCellCoord.Y * MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS,
                            renderCellCoord.Z * MyVoxelConstants.RENDER_CELL_SIZE_IN_GEOMETRY_CELLS)));


                Profiler.BeginNextBlock("LOD1 - AddTriangles");
                m_dataCellsQueue.Clear();
                m_dataCellsQueue.Add(m_helperLodCachedDataCell);
                ret.AddTriangles(m_dataCellsQueue);
                Profiler.End();
            }
            else
            {
                throw new InvalidBranchException();
            }

            ret.End();

            return ret;
        }

        //  Gets cell from the cache. If cell isn't in the cache, null is returned.
        //  This is only lookup into hashtable. No precalc is made here.
        private static MyVoxelCacheCellRender GetCellFromCache(int voxelMapId, ref Vector3I cellCoord, MyLodTypeEnum cellHashType)
        {
            Int64 key = MySession.Static.VoxelMaps.GetCellHashCode(voxelMapId, ref cellCoord, cellHashType);

            LinkedListNode<MyVoxelCacheCellRender> ret;
            if (m_cellsByCoordinate.TryGetValue(key, out ret) == true)
            {
                return ret.Value;
            }

            return null;
        }

        //  Add cell into cache and returns reference to it. Cache item with lowest priority is choosen.
        //  Call this method when you want allocate new item in the cache.
        private static MyVoxelCacheCellRender AddCell(int voxelMapId, ref Vector3I cellCoord, MyLodTypeEnum cellHashType)
        {
            Int64 key = MySession.Static.VoxelMaps.GetCellHashCode(voxelMapId, ref cellCoord, cellHashType);

            //  Cache item with lowest priority is choosen.
            LinkedListNode<MyVoxelCacheCellRender> first = m_priority.First;

            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);
            m_priority.RemoveFirst();
            m_priority.AddLast(first);

            //  If this object already contained some vertex buffers (and of course some render cell), we need to dispose its vertex buffers and 
            //  remove from hash table, so that render cell will no longer be in the render cell cache
            if (first.Value.Contains == true)
            {
                Int64 keyForRemoving = MySession.Static.VoxelMaps.GetCellHashCode(first.Value.VoxelMap.VoxelMapId, ref first.Value.CellCoord, first.Value.CellHashType);
                m_cellsByCoordinate.Remove(keyForRemoving);
                first.Value.Reset();
            }

            //  Remember where is render cell cache for this render cell
            m_cellsByCoordinate.Add(key, first);

            //  You have reached the capacity of RENDER cells cache. Consider increasing it.
            MyDebug.AssertDebug(m_cellsByCoordinate.Count <= m_capacity);

            return first.Value;
        }

        //  Remove cell - after voxels were changed, etc.
        public static void RemoveCell(MyVoxelMap voxelMap, ref Vector3I cellCoord, MyLodTypeEnum cellHashType)
        {
            Int64 key = MySession.Static.VoxelMaps.GetCellHashCode(voxelMap.VoxelMapId, ref cellCoord, cellHashType);

            //  If cell is in cache, we remove it from dictionary and move it to the beginning of priority linked list
            LinkedListNode<MyVoxelCacheCellRender> ret;
            if (m_cellsByCoordinate.TryGetValue(key, out ret) == true)
            {
                m_cellsByCoordinate.Remove(key);

                ret.Value.Reset();

                //  Move it to the beginning of priority linked list
                Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);
                m_priority.Remove(ret);
                m_priority.AddFirst(ret);

                VRageRender.MyRenderProxy.InvalidateRenderVoxelCell(voxelMap.GetRenderObjectID(ref cellCoord), (VRageRender.MyLodTypeEnum)(int)cellHashType);
            }
        }

        //  Update cell - immediately after it was last time used. It will get higher priority and won't be flushed when AddCell() called next time.
        private static void UpdateCell(int voxelMapId, ref Vector3I cellCoord, MyLodTypeEnum cellHashType)
        {
            Int64 key = MySession.Static.VoxelMaps.GetCellHashCode(voxelMapId, ref cellCoord, cellHashType);
            LinkedListNode<MyVoxelCacheCellRender> ret = m_cellsByCoordinate[key];

            //  Move it to the end of priority linked list
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);
            m_priority.Remove(ret);
            m_priority.AddLast(ret);
        }

        //  Remove cell for voxels specified be min/max corner. Used after explosion when we want to remove a lot of voxels/cell from cache.
        //  This is efficient method, because it doesn't invalidate cache after every voxel change.
        //  Method knows that adjacent cells need to be removed too (because of MCA), so it invalidates them too.
        public static void RemoveCellForVoxels(MyVoxelMap voxelMap, Vector3I minVoxel, Vector3I maxVoxel)
        {
            //  Calculate voxel for boundary things...
            minVoxel -= MyVoxelPrecalc.InvalidatedRangeInflate;
            maxVoxel += MyVoxelPrecalc.InvalidatedRangeInflate;
            if (maxVoxel.X > voxelMap.SizeMinusOne.X) maxVoxel.X = voxelMap.SizeMinusOne.X;
            if (maxVoxel.Y > voxelMap.SizeMinusOne.Y) maxVoxel.Y = voxelMap.SizeMinusOne.Y;
            if (maxVoxel.Z > voxelMap.SizeMinusOne.Z) maxVoxel.Z = voxelMap.SizeMinusOne.Z;

            //  Min/max cell
            Vector3I minCell = voxelMap.GetVoxelRenderCellCoordinate(ref minVoxel);
            Vector3I maxCell = voxelMap.GetVoxelRenderCellCoordinate(ref maxVoxel);

            //  Invalidate cells
            Vector3I tempCellCoord;
            for (tempCellCoord.X = minCell.X; tempCellCoord.X <= maxCell.X; tempCellCoord.X++)
            {
                for (tempCellCoord.Y = minCell.Y; tempCellCoord.Y <= maxCell.Y; tempCellCoord.Y++)
                {
                    for (tempCellCoord.Z = minCell.Z; tempCellCoord.Z <= maxCell.Z; tempCellCoord.Z++)
                    {
                        RemoveCell(voxelMap, ref tempCellCoord, MyLodTypeEnum.LOD0);
                        RemoveCell(voxelMap, ref tempCellCoord, MyLodTypeEnum.LOD1);
                    }
                }
            }
        }

        public static Vector3 CellVertexPositionScale
        {
            get { return new Vector3(MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES + MyVoxelPrecalc.VertexPositionRangeSizeChange); }
        }

        public static Vector3 CellVertexPositionOffset(ref Vector3I cellCoord)
        {
            return cellCoord * MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES;
        }
    }
}