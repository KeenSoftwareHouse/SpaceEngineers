using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.VoxelMaps;
using Sandbox.Game.Entities.VoxelMaps.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Voxels;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SysUtils.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    public enum MyVoxelDebugDrawMode
    {
        None,
        EmptyCells,
        MixedCells,
        FullCells,
        Content_MicroNodes,
        Content_MicroNodesScaled,
        Content_MacroNodes,
        Content_MacroLeaves,
        Content_MacroScaled,
        Materials_MacroNodes,
        Materials_MacroLeaves,
    }

    [MyEntityType(typeof(MyObjectBuilder_VoxelMap))]
    public partial class MyVoxelMap : MyEntity
    {
        private static MyStorageDataCache m_storageCache = new MyStorageDataCache();

        private MyProxyStorage m_storage;
        public IMyStorage Storage
        {
            get { return m_storage; }
        }

        public readonly MyVoxelGeometry Geometry = new MyVoxelGeometry();

        internal new MyVoxelPhysicsBody Physics
        {
            get { return base.Physics as MyVoxelPhysicsBody; }
            set { base.Physics = value; }
        }

        //  Voxel map ID must be unique, now just on client side, but later maybe on server too
        public int VoxelMapId
        {
            get;
            private set;
        }

        //  Position of left/bottom corner of this voxel map, in world space (not relative to sector)
        public Vector3 PositionLeftBottomCorner
        {
            get;
            private set;
        }

        //  Size of voxel map (in voxels)
        public Vector3I Size
        {
            get { return Storage.Size; }
        }
        public Vector3I SizeMinusOne
        {
            get { return Storage.Size - 1; }
        }

        //  Size of voxel map (in metres)
        public Vector3 SizeInMetres
        {
            get;
            private set;
        }
        public Vector3 SizeInMetresHalf
        {
            get;
            private set;
        }

        //  Count of voxel render cells in all directions
        public Vector3I RenderCellsCount
        {
            get;
            private set;
        }

        internal new MySyncVoxel SyncObject
        {
            get { return (MySyncVoxel)base.SyncObject; }
        }

        public void Init(MyObjectBuilder_EntityBase builder, bool createPhysics)
        {
            MyObjectBuilder_VoxelMap ob = (MyObjectBuilder_VoxelMap)builder;
            Profiler.Begin("base init");

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            SyncFlag = true;
            base.Init(builder);
            base.Init(null, null, null, null, null);

            Profiler.BeginNextBlock("Load file");
            LoadFile(builder.PositionAndOrientation.Value.Position, ob);
            Profiler.End();

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public void Init(MyStorageBase storage, bool isStorageMutable)
        {
            Flags |= EntityFlags.NeedsUpdate10;
            SyncFlag = true;

            base.Init(null);

            m_storage = new MyProxyStorage(isStorageMutable, storage);
            InitVoxelMap(Vector3.Zero);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void Init(MyObjectBuilder_EntityBase builder)
        {
            Init(builder, createPhysics: true);
        }

        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncVoxel(this);
        }

        //  This method initializes voxel map (size, position, etc) but doesn't load voxels
        //  It only presets all materials to values specified in 'defaultMaterial' - so it will become material everywhere.
        private void InitVoxelMap(Vector3 positionLeftBottom)
        {
            MySandboxGame.Log.WriteLine("MyVoxelMap.InitVoxelMap() - Start");
            MySandboxGame.Log.IncreaseIndent();

            var defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();

            VoxelMapId = MySession.Static.VoxelMaps.AllocateId();
            var size = Storage.Size;
            MySandboxGame.Log.WriteLine("File: " + Storage.Name, LoggingOptions.VOXEL_MAPS);
            MySandboxGame.Log.WriteLine("ID: " + VoxelMapId, LoggingOptions.VOXEL_MAPS);
            MySandboxGame.Log.WriteLine("Size: " + size, LoggingOptions.VOXEL_MAPS);

            //  If you need more voxel maps, enlarge this constant.
            MyDebug.AssertRelease(VoxelMapId <= MyVoxelConstants.MAX_VOXEL_MAP_ID);

            SizeInMetres = size * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            SizeInMetresHalf = SizeInMetres / 2.0f;

            LocalAABB = new BoundingBox(-SizeInMetresHalf, SizeInMetresHalf);

            PositionLeftBottomCorner = positionLeftBottom;
            if (MyFakes.ENABLE_DEPRECATED_HALF_VOXEL_OFFSET)
            { // for backward compatibility only
                PositionLeftBottomCorner += MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            }
            SetWorldMatrix(Matrix.CreateTranslation(PositionLeftBottomCorner + SizeInMetresHalf));

            //  If you need larged voxel maps, enlarge this constant.
            MyDebug.AssertRelease(Size.X <= MyVoxelConstants.MAX_VOXEL_MAP_SIZE_IN_VOXELS);
            MyDebug.AssertRelease(Size.Y <= MyVoxelConstants.MAX_VOXEL_MAP_SIZE_IN_VOXELS);
            MyDebug.AssertRelease(Size.Z <= MyVoxelConstants.MAX_VOXEL_MAP_SIZE_IN_VOXELS);

            //  Voxel map size must be multiple of a voxel data cell size.
            MyDebug.AssertRelease((Size.X & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
            MyDebug.AssertRelease((Size.Y & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
            MyDebug.AssertRelease((Size.Z & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);

            //  Voxel map size must be multiple of a voxel data cell size.
            MyDebug.AssertRelease((Size.X % MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS) == 0);
            MyDebug.AssertRelease((Size.Y % MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS) == 0);
            MyDebug.AssertRelease((Size.Z % MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS) == 0);
            RenderCellsCount = Size / MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;
            Geometry.Init(this);
            CastShadows = true;

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVoxelMap.InitVoxelMap() - End");
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (Physics == null)
                CreatePhysics();

            base.UpdateOnceBeforeFrame();
        }

        public void CreatePhysics()
        {
            Profiler.Begin("MyVoxelMap::CreatePhysics()");
            if (Physics != null)
                Physics.Close();

            Physics = new MyVoxelPhysicsBody(this);
            if (Physics.IsEmpty)
            {
                Physics.Close();
                Physics = null;
            }
            else
            {
                Physics.Enabled = true;
            }

            Profiler.End();
        }

        public void MergeVoxelMaterials(MyMwcVoxelFilesEnum voxelFile, Vector3I voxelPosition, MyVoxelMaterialDefinition materialToSet)
        {
            Storage.MergeVoxelMaterials(voxelFile, voxelPosition, materialToSet);
        }

        public MyVoxelRangeType GetVoxelRangeTypeInBoundingBox(BoundingBox worldAabb)
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);

            Vector3I minCorner = GetVoxelCoordinateFromMeters(worldAabb.Min - MyVoxelConstants.VOXEL_SIZE_IN_METRES);
            Vector3I maxCorner = GetVoxelCoordinateFromMeters(worldAabb.Max + MyVoxelConstants.VOXEL_SIZE_IN_METRES);
            FixVoxelCoord(ref minCorner);
            FixVoxelCoord(ref maxCorner);

            return Storage.GetRangeType(0, ref minCorner, ref maxCorner);
        }

        public float GetVoxelContentInBoundingBox(BoundingBox worldAabb, out float cellCount)
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);
            cellCount = 0;
            float result = 0;

            Vector3I minCorner = GetVoxelCoordinateFromMeters(worldAabb.Min - MyVoxelConstants.VOXEL_SIZE_IN_METRES);
            Vector3I maxCorner = GetVoxelCoordinateFromMeters(worldAabb.Max + MyVoxelConstants.VOXEL_SIZE_IN_METRES);
            FixVoxelCoord(ref minCorner);
            FixVoxelCoord(ref maxCorner);
            m_storageCache.Resize(ref minCorner, ref maxCorner);
            Storage.ReadRange(m_storageCache, true, false, 0, ref minCorner, ref maxCorner);
            BoundingBox voxelBox;

            Vector3I coord, cache;
            for (coord.Z = minCorner.Z, cache.Z = 0; coord.Z <= maxCorner.Z; coord.Z++, cache.Z++)
            {
                for (coord.Y = minCorner.Y, cache.Y = 0; coord.Y <= maxCorner.Y; coord.Y++, cache.Y++)
                {
                    for (coord.X = minCorner.X, cache.X = 0; coord.X <= maxCorner.X; coord.X++, cache.X++)
                    {
                        GetVoxelBoundingBox(ref coord, out voxelBox);
                        if (worldAabb.Intersects(voxelBox))
                        {
                            float content = m_storageCache.Content(ref cache) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT;
                            float containPercent = worldAabb.Intersect(voxelBox).Volume() / MyVoxelConstants.VOXEL_VOLUME_IN_METERS;
                            result += content * containPercent;
                            cellCount += containPercent;
                        }
                    }
                }
            }
            return result;
        }

        public void GetAllMaterialsPresent(HashSet<MyVoxelMaterialDefinition> outputMaterialSet)
        {
            Storage.GetAllMaterialsPresent(outputMaterialSet);
        }

        //  Return cell to which belongs specified voxel (render cell)
        //  IMPORTANT: Input variable 'tempVoxelCoord' is 'ref' only for optimization. Never change its value in the method!!!
        public Vector3I GetVoxelRenderCellCoordinate(ref Vector3I voxelCoord)
        {
            return new Vector3I(voxelCoord.X / MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS, voxelCoord.Y / MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS, voxelCoord.Z / MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS);
        }

        //  Return voxel's coordinate (from meters) to voxel map (int)
        //  Method return position of voxel corner, not the center.
        public Vector3I GetVoxelCoordinateFromMeters(Vector3 pos)
        {
            return Vector3I.Round((pos - PositionLeftBottomCorner) / MyVoxelConstants.VOXEL_SIZE_IN_METRES);
        }

        //  Return voxel's coordinate in world space (in metres)
        //  It's coordinate of closest corner (not center of a voxel)
        public Vector3 GetVoxelPositionAbsolute(ref Vector3I voxelCoord)
        {
            return PositionLeftBottomCorner + voxelCoord * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
        }

        //  Return render cell absolute coordinate in world space (in metres).
        //  It's coordinate of closest corner (not center of a cell)
        //  It can be used to calculate bounding box of a render cell.
        //  IMPORTANT: It's hard to say if border should be offset by half-voxel or not... depends on requirements.
        public Vector3 GetRenderCellPositionAbsolute(ref Vector3I cellCoord)
        {
            Vector3I voxelCoord = cellCoord * MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;
            return GetVoxelPositionAbsolute(ref voxelCoord);
        }

        public void GetVoxelBoundingBox(ref Vector3I voxelCoord, out BoundingBox aabb)
        {
            var pos = GetVoxelPositionAbsolute(ref voxelCoord);
            aabb = new BoundingBox(pos - MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF, pos + MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF);
        }

        //  Calculates bounding box of a specified render cell. Coordinates are in world/absolute space.
        private void GetRenderCellBoundingBox(ref Vector3I cellCoord, out BoundingBox outBoundingBox)
        {
            Vector3 renderCellMin = GetRenderCellPositionAbsolute(ref cellCoord);
            outBoundingBox = new BoundingBox(renderCellMin, renderCellMin + MyVoxelConstants.RENDER_CELL_SIZE_VECTOR_IN_METRES);
        }

        //  Prepares render cell cache. Basicaly, it will precalculate all cells in this voxel map.
        //  Cells that don't contain triangles will be precalced too, but of course not stored in the cache.
        //  This method prepares render and data cells too, so you don't have to call PrepareDataCellCache()
        //  IMPORTANT: Do not use this method because it fills vertex/index buffers and when called from background thread 
        //  while game is minimized through alt+f4, those VB/IB won't be filled
        public void PrepareRenderCellCache()
        {
            Vector3I cellCoord;
            for (cellCoord.X = 0; cellCoord.X < RenderCellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < RenderCellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < RenderCellsCount.Z; cellCoord.Z++)
                    {
                        MyVoxelCacheRender.GetCell(this, ref cellCoord, MyLodTypeEnum.LOD0);
                        MyVoxelCacheRender.GetCell(this, ref cellCoord, MyLodTypeEnum.LOD1);
                    }
                }
            }

            UpdateAABBHr();
        }

        //  If voxel coord0 (in voxel units, not meters) is outside of the voxelmap, we fix its coordinate so it lie in the voxelmap.
        public void FixVoxelCoord(ref Vector3I voxelCoord)
        {
            var sizeMinusOne = Storage.Size - 1;
            Vector3I.Clamp(ref voxelCoord, ref Vector3I.Zero, ref sizeMinusOne, out voxelCoord);
        }

        //collisions
        //sphere vs voxel volumetric test
        public override bool DoOverlapSphereTest(float sphereRadius, Vector3 spherePos)
        {
            MyVoxelMap voxelMap = this;

            Profiler.Begin("MyVoxelMap.DoOverlapSphereTest");
            Vector3 body0Pos = spherePos; // sphere pos
            BoundingSphere newSphere;
            newSphere.Center = body0Pos;
            newSphere.Radius = sphereRadius;

            //  We will iterate only voxels contained in the bounding box of new sphere, so here we get min/max corned in voxel units
            Vector3I minCorner = voxelMap.GetVoxelCoordinateFromMeters(new Vector3(
                newSphere.Center.X - newSphere.Radius - MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                newSphere.Center.Y - newSphere.Radius - MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                newSphere.Center.Z - newSphere.Radius - MyVoxelConstants.VOXEL_SIZE_IN_METRES));
            Vector3I maxCorner = voxelMap.GetVoxelCoordinateFromMeters(new Vector3(
                newSphere.Center.X + newSphere.Radius + MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                newSphere.Center.Y + newSphere.Radius + MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                newSphere.Center.Z + newSphere.Radius + MyVoxelConstants.VOXEL_SIZE_IN_METRES));
            voxelMap.FixVoxelCoord(ref minCorner);
            voxelMap.FixVoxelCoord(ref maxCorner);
            m_storageCache.Resize(ref minCorner, ref maxCorner);
            Storage.ReadRange(m_storageCache, true, false, 0, ref minCorner, ref maxCorner);

            Vector3I tempVoxelCoord, cache;
            for (tempVoxelCoord.Z = minCorner.Z, cache.Z = 0; tempVoxelCoord.Z <= maxCorner.Z; tempVoxelCoord.Z++, cache.Z++)
            {
                for (tempVoxelCoord.Y = minCorner.Y, cache.Y = 0; tempVoxelCoord.Y <= maxCorner.Y; tempVoxelCoord.Y++, cache.Y++)
                {
                    for (tempVoxelCoord.X = minCorner.X, cache.X = 0; tempVoxelCoord.X <= maxCorner.X; tempVoxelCoord.X++, cache.X++)
                    {
                        byte voxelContent = m_storageCache.Content(ref cache);

                        //  Ignore voxels bellow the ISO value (empty, partialy empty...)
                        if (voxelContent < MyVoxelConstants.VOXEL_ISO_LEVEL) continue;

                        Vector3 voxelPosition = voxelMap.GetVoxelPositionAbsolute(ref tempVoxelCoord);

                        float voxelSize = (voxelContent / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT) * MyVoxelConstants.VOXEL_RADIUS;

                        //  If distance to voxel border is less than sphere radius, we have a collision
                        //  So now we calculate normal vector and penetration depth but on OLD sphere
                        float newDistanceToVoxel = Vector3.Distance(voxelPosition, newSphere.Center) - voxelSize;
                        if (newDistanceToVoxel < (newSphere.Radius))
                        {
                            Profiler.End();
                            return true;
                        }
                    }
                }
            }
            Profiler.End();
            return false;
        }

        //  Return true if voxel map intersects specified sphere.
        //  This method doesn't return exact point of intersection or any additional data.
        //  We don't look for closest intersection - so we stop on first intersection found.
        public override bool GetIntersectionWithSphere(ref BoundingSphere sphere)
        {
            Profiler.Begin("MyVoxelMap.GetIntersectionWithSphere()");
            try
            {
                if (!WorldAABB.Intersects(ref sphere))
                    return false;

                return Geometry.Intersects(ref sphere);
            }
            finally
            {
                Profiler.End();
            }
        }

        protected override void UpdateWorldVolume()
        {
            this.PositionLeftBottomCorner = (this.WorldMatrix.Translation - this.SizeInMetresHalf);
            m_worldAABB = MyUtils.GetNewBoundingBox(PositionLeftBottomCorner, SizeInMetres);
            m_worldVolume = BoundingSphere.CreateFromBoundingBox(m_worldAABB);

            InvalidateRenderObjects();
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            UpdatePhysicsShape();
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            UpdatePhysicsShape();
        }

        private void UpdatePhysicsShape()
        {
            Debug.Assert(Physics != null);
            if (Physics == null)
                return;

            Physics.UpdateShape();
            if (Physics.IsEmpty)
            {
                Physics.Close();
                Physics = null;
                RaisePhysicsChanged();
            }
        }

        //  Method finds intersection with line and any voxel triangleVertexes in this voxel map. Closes intersection is returned.
        internal override bool GetIntersectionWithLine(ref Line worldLine, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            t = null;

            float intersectionDistance;
            if (!m_worldAABB.Intersects(worldLine, out intersectionDistance))
                return false;

            Profiler.Begin("VoxelMap.LineIntersection");
            try
            {
                return Geometry.Intersect(ref worldLine, out t, flags);
            }
            finally
            {
                Profiler.End();
            }
        }

        public override bool GetIntersectionWithLine(ref Line worldLine, out Vector3? v, bool useCollisionModel = true, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            MyIntersectionResultLineTriangleEx? result;
            GetIntersectionWithLine(ref worldLine, out result);
            v = null;
            if (result != null)
            {
                v = result.Value.IntersectionPointInWorldSpace;
                return true;
            }
            return false;
        }

        public override bool DebugDraw()
        {
            VRageRender.MyRenderProxy.DebugDrawAABB(WorldAABB, Vector3I.One, 1f, 1f, true);
            VRageRender.MyRenderProxy.DebugDrawLine3D(PositionLeftBottomCorner, PositionLeftBottomCorner + new Vector3(1f, 0f, 0f), Color.Red, Color.Red, true);
            VRageRender.MyRenderProxy.DebugDrawLine3D(PositionLeftBottomCorner, PositionLeftBottomCorner + new Vector3(0f, 1f, 0f), Color.Green, Color.Green, true);
            VRageRender.MyRenderProxy.DebugDrawLine3D(PositionLeftBottomCorner, PositionLeftBottomCorner + new Vector3(0f, 0f, 1f), Color.Blue, Color.Blue, true);

            Storage.DebugDraw(this, MyFakes.DEBUG_DRAW_VOXELS_MODE, (int)MyFakes.VOXEL_OCTREE_DEBUG_DRAW_DEPTH);
            if (MyFakes.DEBUG_DRAW_VOXEL_GEOMETRY_CELL)
            {
                var line = new Line(MySector.MainCamera.Position, MySector.MainCamera.Position + 50f * MySector.MainCamera.ForwardVector);
                MyIntersectionResultLineTriangleEx? result;
                bool depthRead = true;
                if (Geometry.Intersect(ref line, out result, IntersectionFlags.ALL_TRIANGLES))
                {
                    var cellCoord = new Vector3I(result.Value.IntersectionPointInObjectSpace / MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES);
                    var cell = Geometry.GetCell(MyLodTypeEnum.LOD0, ref cellCoord);
                    if (cell != null)
                    {
                        MyVoxelVertex tmp;
                        var triangleBatch = MyRenderProxy.PrepareDebugDrawTriangles();
                        for (int i = 0; i < cell.VoxelVerticesCount; ++i)
                        {
                            cell.GetUnpackedVertex(i, out tmp);
                            triangleBatch.AddVertex(tmp.Position);
                            tmp.Position += PositionLeftBottomCorner;
                            MyRenderProxy.DebugDrawLine3D(tmp.Position, tmp.Position + tmp.Normal * MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF, Color.Gray, Color.White, depthRead);
                        }
                        for (int i = 0; i < cell.VoxelTrianglesCount; ++i)
                        {
                            triangleBatch.AddIndex(cell.VoxelTriangles[i].VertexIndex2);
                            triangleBatch.AddIndex(cell.VoxelTriangles[i].VertexIndex1);
                            triangleBatch.AddIndex(cell.VoxelTriangles[i].VertexIndex0);
                        }
                        MyRenderProxy.DebugDrawTriangles(triangleBatch, Matrix.CreateTranslation(PositionLeftBottomCorner), Color.CornflowerBlue, depthRead, false);
                    }
                }
            }
            return base.DebugDraw();
        }

        //  Checks if specified box intersects bounding box of this this voxel map.
        public bool IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref BoundingBox boundingBox)
        {
            bool outRet;
            WorldAABB.Intersects(ref boundingBox, out outRet);
            return outRet;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_VoxelMap voxelMapBuilder = (MyObjectBuilder_VoxelMap)base.GetObjectBuilder(copy);

            var minCorner = PositionLeftBottomCorner;
            if (MyFakes.ENABLE_DEPRECATED_HALF_VOXEL_OFFSET)
            { // for backward compatibility only
                minCorner -= MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            }

            voxelMapBuilder.PositionAndOrientation = new MyPositionAndOrientation(minCorner, Vector3.Forward, Vector3.Up);
            voxelMapBuilder.Name = Storage.Name;
            voxelMapBuilder.MutableStorage = m_storage.IsMutable;

            return voxelMapBuilder;
        }

        //  This method must be called when this object dies or is removed
        //  E.g. it removes lights, sounds, etc
        public override void Close()
        {
            base.Close();

            //  Delete this voxel map from data cell cache
            Storage.Close();
            Geometry.Clear();

            //  Delete this voxel map from render cell cache
            Vector3I renderCellCoord;
            for (renderCellCoord.X = 0; renderCellCoord.X < RenderCellsCount.X; renderCellCoord.X++)
            {
                for (renderCellCoord.Y = 0; renderCellCoord.Y < RenderCellsCount.Y; renderCellCoord.Y++)
                {
                    for (renderCellCoord.Z = 0; renderCellCoord.Z < RenderCellsCount.Z; renderCellCoord.Z++)
                    {
                        MyVoxelCacheRender.RemoveCell(this, ref renderCellCoord, MyLodTypeEnum.LOD1);
                        MyVoxelCacheRender.RemoveCell(this, ref renderCellCoord, MyLodTypeEnum.LOD0);
                    }
                }
            }

            MySession.Static.VoxelMaps.RemoveVoxelMap(this);
        }

        /// <summary>
        /// Invalidates voxel cache
        /// </summary>
        /// <param name="minChanged">Inclusive min</param>
        /// <param name="maxChanged">Inclusive max</param>
        public void InvalidateCache(Vector3I minChanged, Vector3I maxChanged)
        {
            minChanged = Vector3I.Clamp(minChanged, Vector3I.Zero, SizeMinusOne);
            maxChanged = Vector3I.Clamp(maxChanged, Vector3I.Zero, SizeMinusOne);
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyVoxelMap::InvalidateCache()");
            Geometry.InvalidateRange(minChanged, maxChanged);
            MyVoxelCacheRender.RemoveCellForVoxels(this, minChanged, maxChanged);

            if (Physics != null)
                Physics.InvalidateRange(minChanged, maxChanged);

            InvalidateRenderObjects(true);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public override string GetFriendlyName()
        {
            return "MyVoxelMap";
        }

        protected override void AddRenderObjects()
        {
            m_renderObjectIDs = new uint[RenderCellsCount.X * RenderCellsCount.Y * RenderCellsCount.Z];

            for (int j = 0; j < m_renderObjectIDs.Length; j++)
                m_renderObjectIDs[j] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;

            int i = 0;
            Vector3I cellCoord;
            BoundingBox aabb;
            var scale = MyVoxelCacheRender.CellVertexPositionScale;
            for (cellCoord.X = 0; cellCoord.X < RenderCellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < RenderCellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < RenderCellsCount.Z; cellCoord.Z++)
                    {
                        var offset = MyVoxelCacheRender.CellVertexPositionOffset(ref cellCoord);

                        // x * (maxy*maxz) + y * maxz  + z
                        GetRenderCellBoundingBox(ref cellCoord, out aabb);
                        SetRenderObjectID(i++, VRageRender.MyRenderProxy.CreateRenderVoxelCell(GetFriendlyName(), VoxelMapId, cellCoord, aabb, PositionLeftBottomCorner,
                            offset,
                            scale
                            ));
                    }
                }
            }
        }

        public override void OnWorldPositionChanged(object source)
        {
            base.OnWorldPositionChanged(source);

            int i = 0;
            Vector3I cellCoord;
            BoundingBox aabb;
            for (cellCoord.X = 0; cellCoord.X < RenderCellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < RenderCellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < RenderCellsCount.Z; cellCoord.Z++)
                    {
                        GetRenderCellBoundingBox(ref cellCoord, out aabb);
                        VRageRender.MyRenderProxy.UpdateRenderVoxelCellAABB(m_renderObjectIDs[i++], aabb, PositionLeftBottomCorner);
                    }
                }
            }
        }

        public uint GetRenderObjectID(ref Vector3I renderCellCoord)
        {
            int index = renderCellCoord.X * RenderCellsCount.Y * RenderCellsCount.Z + renderCellCoord.Y * RenderCellsCount.Z + renderCellCoord.Z;

            if (m_renderObjectIDs.Length == 0 || index >= m_renderObjectIDs.Length)
                return VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED; //If voxel is not activated, but needs to be calculated

            return m_renderObjectIDs[index];
        }

        protected override void RemoveRenderObjects()
        {
            for (int i = 0; i < m_renderObjectIDs.Length; i++)
            {
                ReleaseRenderObjectID(i);
            }
        }

        protected override void InvalidateRenderObjects(bool sortIntoCullobjects = false)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyVoxelMap::InvalidateRenderObjects()");
            if (Visible)
            {
                foreach (uint renderObjectID in m_renderObjectIDs)
                {
                    Matrix voxelMatrix = Matrix.CreateTranslation(PositionLeftBottomCorner);
                    VRageRender.MyRenderProxy.UpdateRenderObject(renderObjectID, ref voxelMatrix, sortIntoCullobjects);
                }
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public void OverwriteAllMaterials(MyVoxelMaterialDefinition material)
        {
            Profiler.Begin("MyVoxelMap.OverwriteAllMaterials");

            Storage.OverwriteAllMaterials(material);
            InvalidateCache(Vector3I.Zero, Size);

            Profiler.End();
        }

        public override bool IsVolumetric
        {
            get { return true; }
        }

        /// <summary>
        /// Covers surface of the voxel map using given material.
        /// Material is applied per data cell and number of consecutive cells is given by cellThickness.
        /// </summary>
        /// <param name="material">Material to use for surface.</param>
        /// <param name="cellThickness">Number of consecutive cells covered using given material. Min. is 1.</param>
        public void SetSurfaceMaterial(MyVoxelMaterialDefinition material, int cellThickness)
        {
            Storage.SetSurfaceMaterial(material, cellThickness);
            Vector3I minCorner = Vector3I.Zero;
            Vector3I maxCorner = Size;
            InvalidateCache(minCorner, maxCorner);
        }

        private void LoadFile(Vector3 position, MyObjectBuilder_VoxelMap objectBuilder)
        {
            m_storage = new MyProxyStorage(objectBuilder.MutableStorage, MyStorageBase.Load(objectBuilder.Name));
            InitVoxelMap(position);
        }
    }
}
