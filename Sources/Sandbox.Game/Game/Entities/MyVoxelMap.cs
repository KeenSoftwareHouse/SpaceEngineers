using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;

using VRage;
using VRageMath;
using VRageRender;
using Sandbox.Common.Components;
using Sandbox.Engine.Utils;
using VRage.Voxels;
using VRage.Utils;
using System;
using VRage.ObjectBuilders;
using VRage.Components;

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
        /// <summary>
        /// Backward compatibility. Helper when generating new name when loaded voxel map had immutable storage (instanced).
        /// </summary>
        private static int m_immutableStorageNameSalt = 0;
   
        internal new MyVoxelPhysicsBody Physics
        {
            get { return base.Physics as MyVoxelPhysicsBody; }
            set { base.Physics = value; }
        }

        public MyVoxelMap()
        {
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
            Render = new MyRenderComponentVoxelMap();
            AddDebugRenderComponent(new MyDebugRenderComponentVoxelMap(this));
        }

        public override void Init(MyObjectBuilder_EntityBase builder)
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
                StorageName = string.Format("{0}-{1}", ob.StorageName, m_immutableStorageNameSalt++);
            }

            m_storage = MyStorageBase.Load(ob.StorageName);
            m_storage.RangeChanged += storage_RangeChanged;
            m_storageMax = m_storage.Size;

            InitVoxelMap(ob.PositionAndOrientation.Value.Position, m_storage.Size);

            ProfilerShort.End();
        }

        public override void UpdateOnceBeforeFrame()
        {
            PositionComp.UpdateAABBHr();
            base.UpdateOnceBeforeFrame();
        }
        
       
        public override bool IsOverlapOverThreshold(BoundingBoxD worldAabb, float thresholdPercentage)
        {
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
            m_storageCache.Resize(minCorner, maxCorner);
            Storage.ReadRange(m_storageCache, MyStorageDataTypeFlags.Content, 0, ref minCorner, ref maxCorner);
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
                                var contentVolume = m_storageCache.Content(ref cache) * invFullVoxel * voxelVolume;
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

        private void UpdateWorldVolume()
        {
            this.PositionLeftBottomCorner = (Vector3)(this.WorldMatrix.Translation - this.SizeInMetresHalf);
            PositionComp.WorldAABB = new BoundingBoxD((Vector3D)PositionLeftBottomCorner, (Vector3D)PositionLeftBottomCorner + (Vector3D)SizeInMetres); 
            PositionComp.WorldVolume = BoundingSphereD.CreateFromBoundingBox(PositionComp.WorldAABB);

            Render.InvalidateRenderObjects();
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


        public override void Init(string storageName, IMyStorage storage, Vector3D positionMinCorner)
        {
            m_storageMax = storage.Size;
            base.Init(storageName, storage, positionMinCorner);

            m_storage.RangeChanged += storage_RangeChanged;

        }

        public MyVoxelRangeType GetVoxelRangeTypeInBoundingBox(BoundingBoxD worldAabb)
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);

            Vector3I minCorner, maxCorner;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref worldAabb.Min, out minCorner);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref worldAabb.Max, out maxCorner);
            minCorner += StorageMin;
            maxCorner += StorageMin;

            Storage.ClampVoxelCoord(ref minCorner);
            Storage.ClampVoxelCoord(ref maxCorner);

            return MyVoxelRangeType.MIXED;
        }

        protected override void InitVoxelMap(Vector3D positionMinCorner, Vector3I size, bool useOffset = true)
        {
            base.InitVoxelMap(positionMinCorner, size, useOffset);
            Physics = new MyVoxelPhysicsBody(this,3.0f);
            Physics.Enabled = true;
        }
     
    }
}