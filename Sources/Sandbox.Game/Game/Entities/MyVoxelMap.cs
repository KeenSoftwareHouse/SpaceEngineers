using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using Sandbox.Game.World;
using System.Diagnostics;
using VRageMath;
using Sandbox.Engine.Utils;
using VRage.Utils;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Voxels;
using System;


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
        Content_DataProvider,
    }

    [MyEntityType(typeof(MyObjectBuilder_VoxelMap))]
    public partial class MyVoxelMap : MyVoxelBase
    {
        public override IMyStorage Storage
        {
            get { return m_storage; }
            set
            {
                if (m_storage != null)
                {
                    m_storage.RangeChanged -= storage_RangeChanged;
                }

                m_storage = value;
                m_storage.RangeChanged += storage_RangeChanged;
                m_storageMax = m_storage.Size;

                //m_storage.Reset();
            }
        }
        private static int m_immutableStorageNameSalt = 0;

        internal new MyVoxelPhysicsBody Physics
        {
            get { return base.Physics as MyVoxelPhysicsBody; }
            set { base.Physics = value; }
        }

        public override MyVoxelBase RootVoxel { get { return this; } }

        public MyVoxelMap()
        {
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
            Render = new MyRenderComponentVoxelMap();
            AddDebugRenderComponent(new MyDebugRenderComponentVoxelMap(this));
        }

        public override void Init(MyObjectBuilder_EntityBase builder)
        {
            var ob = (MyObjectBuilder_VoxelMap)builder;
            if (ob == null)
            {
                return;
            }

            m_storage = MyStorageBase.Load(ob.StorageName);
            
            if(m_storage == null)
            {
                throw new Exception("Voxel storage not found: " + ob.StorageName);
            }

            Init(builder, m_storage);

            if (ob.ContentChanged.HasValue)
            {
                ContentChanged = ob.ContentChanged.Value;
            }
            else
            {
                ContentChanged = true;
            }
        }

        public override void Init(MyObjectBuilder_EntityBase builder, IMyStorage storage)
        {
            ProfilerShort.Begin("base init");

            SyncFlag = true;

            base.Init(builder);
            base.Init(null, null, null, null, null);

            ProfilerShort.BeginNextBlock("Load file");

            var ob = (MyObjectBuilder_VoxelMap)builder;
            if (ob == null)
            {
                return;
            }
            if (ob.MutableStorage)
            {
                StorageName = ob.StorageName;
            }
            else
            {
                StorageName = GetNewStorageName(ob.StorageName);
            }

            m_storage = storage;
            m_storage.RangeChanged += storage_RangeChanged;
            m_storageMax = m_storage.Size;

            InitVoxelMap(MatrixD.CreateWorld((Vector3D)ob.PositionAndOrientation.Value.Position + Vector3D.TransformNormal((Vector3D)m_storage.Size / 2, WorldMatrix), WorldMatrix.Forward, WorldMatrix.Up), m_storage.Size);

            ProfilerShort.End();
        }

        public static string GetNewStorageName(string storageName)
        {
            return string.Format("{0}-{1}", storageName, m_immutableStorageNameSalt++);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
        }

        public override bool IsOverlapOverThreshold(BoundingBoxD worldAabb, float thresholdPercentage)
        {
            if(m_storage == null) {
                if (MyEntities.GetEntityByIdOrDefault(this.EntityId) != this)
                    MyDebug.FailRelease("Voxel map was deleted!");
                else
                    MyDebug.FailRelease("Voxel map is still in world but has null storage!");
                return false;
            }

            //Debug.Assert(
            //    worldAabb.Size.X > MyVoxelConstants.VOXEL_SIZE_IN_METRES &&
            //    worldAabb.Size.Y > MyVoxelConstants.VOXEL_SIZE_IN_METRES &&
            //    worldAabb.Size.Z > MyVoxelConstants.VOXEL_SIZE_IN_METRES,
            //    "One of the sides of queried AABB is too small compared to voxel size. Results will be unreliable.");

            Vector3I minCorner, maxCorner;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref worldAabb.Min, out minCorner);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref worldAabb.Max, out maxCorner);

            minCorner += StorageMin;
            maxCorner += StorageMin;

            Storage.ClampVoxelCoord(ref minCorner);
            Storage.ClampVoxelCoord(ref maxCorner);
            m_tempStorage.Resize(minCorner, maxCorner);
            Storage.ReadRange(m_tempStorage, MyStorageDataTypeFlags.Content, 0, ref minCorner, ref maxCorner);
            BoundingBoxD voxelBox;

            //MyRenderProxy.DebugDrawAABB(worldAabb, Color.White, 1f, 1f, true);

            var invFullVoxel = 1.0 / (double)MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT;
            var voxelVolume = 1.0 / (double)MyVoxelConstants.VOXEL_VOLUME_IN_METERS;
            double overlapContentVolume = 0.0;

            var queryVolume = worldAabb.Volume;

            //using (var batch = MyRenderProxy.DebugDrawBatchAABB(Matrix.Identity, new Color(Color.Green, 0.1f), true, true))
            {
                Vector3I coord, cache;
                for (coord.Z = minCorner.Z, cache.Z = 0; coord.Z <= maxCorner.Z; coord.Z++, cache.Z++)
                {
                    for (coord.Y = minCorner.Y, cache.Y = 0; coord.Y <= maxCorner.Y; coord.Y++, cache.Y++)
                    {
                        for (coord.X = minCorner.X, cache.X = 0; coord.X <= maxCorner.X; coord.X++, cache.X++)
                        {
                            MyVoxelCoordSystems.VoxelCoordToWorldAABB(PositionLeftBottomCorner, ref coord, out voxelBox);
                            if (worldAabb.Intersects(voxelBox))
                            {
                                var contentVolume = m_tempStorage.Content(ref cache) * invFullVoxel * voxelVolume;
                                var overlapVolume = worldAabb.Intersect(voxelBox).Volume;
                                overlapContentVolume += contentVolume * overlapVolume;

                                //batch.Add(ref voxelBox);
                            }
                        }
                    }
                }
            }

            var overlapVolumePercentage = overlapContentVolume / queryVolume;
            //MyRenderProxy.DebugDrawText3D(worldAabb.Center, overlapVolumePercentage.ToString("0.000"), Color.White, 1f, false);
            return overlapVolumePercentage >= thresholdPercentage;
        }


        //  Return true if voxel map intersects specified sphere.
        //  This method doesn't return exact point of intersection or any additional data.
        //  We don't look for closest intersection - so we stop on first intersection found.
        public override bool GetIntersectionWithSphere(ref BoundingSphereD sphere)
        {
            ProfilerShort.Begin("MyVoxelMap.GetIntersectionWithSphere()");
            try
            {
                if (!PositionComp.WorldAABB.Intersects(ref sphere))
                    return false;

                var localSphere = new BoundingSphereD(sphere.Center - PositionLeftBottomCorner, sphere.Radius);
                return Storage.Geometry.Intersects(ref localSphere);
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (Physics != null)
            {
                Physics.UpdateBeforeSimulation10();
            }
        }

        public override void UpdateAfterSimulation10()
        {
            //Debug.Assert(MyExternalReplicable.FindByObject(this) != null, "Voxel map replicable not found, but it should be there");
            base.UpdateAfterSimulation10();
            if (Physics != null)
            {
                Physics.UpdateAfterSimulation10();
            }
        }


        //  This method must be called when this object dies or is removed
        //  E.g. it removes lights, sounds, etc
        protected override void BeforeDelete()
        {
            base.BeforeDelete();
            (Render as MyRenderComponentVoxelMap).CancelAllRequests();
            m_storage = null;
            MySession.Static.VoxelMaps.RemoveVoxelMap(this);
        }

        /// <summary>
        /// Invalidates voxel cache
        /// </summary>
        /// <param name="minChanged">Inclusive min</param>
        /// <param name="maxChanged">Inclusive max</param>
        private void storage_RangeChanged(Vector3I minChanged, Vector3I maxChanged, MyStorageDataTypeFlags dataChanged)
        {
            ProfilerShort.Begin("MyVoxelMap::storage_RangeChanged");

            Debug.Assert(minChanged.IsInsideInclusive(ref m_storageMin, ref m_storageMax) &&
                maxChanged.IsInsideInclusive(ref m_storageMin, ref m_storageMax));

            // Physics doesn't care about materials, just shape of things.
            if ((dataChanged & MyStorageDataTypeFlags.Content) != 0 &&
                Physics != null)
            {
                Physics.InvalidateRange(minChanged, maxChanged);
            }

            if (Render is MyRenderComponentVoxelMap)
            {
                (Render as MyRenderComponentVoxelMap).InvalidateRange(minChanged, maxChanged);
            }
            OnRangeChanged(minChanged, maxChanged, dataChanged);
            ContentChanged = true;
            ProfilerShort.End();

        }

        public override string GetFriendlyName()
        {
            return "MyVoxelMap";
        }

        public override bool IsVolumetric
        {
            get { return true; }
        }

        protected override void ClampToWorld()
        {
            return;
        }


        public override void Init(string storageName, IMyStorage storage, MatrixD worldMatrix)
        {
            ProfilerShort.Begin("MyVoxelMap::Init");

            m_storageMax = storage.Size;
            base.Init(storageName, storage, worldMatrix);

            m_storage.RangeChanged += storage_RangeChanged;

            ProfilerShort.End();
        }


        protected override void InitVoxelMap(MatrixD worldMatrix, Vector3I size, bool useOffset = true)
        {
            base.InitVoxelMap(worldMatrix, size, useOffset);

            ((MyStorageBase)Storage).InitWriteCache(8);

            ProfilerShort.Begin("new MyVoxelPhysicsBody");
            Physics = new MyVoxelPhysicsBody(this, 3.0f, lazyPhysics: DelayRigidBodyCreation);
            Physics.Enabled = !MyFakes.DISABLE_VOXEL_PHYSICS;
            ProfilerShort.End();
        }

        public bool IsStaticForCluster
        {
            get { return Physics.IsStaticForCluster; }
            set { Physics.IsStaticForCluster = value; }
        }

        public override int GetOrePriority()
        {
            return 1;
        }

    }
}