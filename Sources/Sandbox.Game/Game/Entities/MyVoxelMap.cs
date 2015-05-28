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
        private static MyStorageDataCache m_storageCache = new MyStorageDataCache();

        public int VoxelMapPruningProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;

        /// <summary>
        /// Backward compatibility. Helper when generating new name when loaded voxel map had immutable storage (instanced).
        /// </summary>
        private static int m_immutableStorageNameSalt = 0;
   
        internal new MyVoxelPhysicsBody Physics
        {
            get { return base.Physics as MyVoxelPhysicsBody; }
            set { base.Physics = value; }
        }

        public MyVoxelMap() : this(createRender: true) { }

        public MyVoxelMap(bool createRender)
        {
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
            if (createRender)
            {
                Render = new MyRenderComponentVoxelMap();
            }
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

        public void Init(IMyStorage storage, Vector3D positionMinCorner, Vector3I storageMin, Vector3I storageMax)
        {
            SyncFlag = true;

            base.Init(null);

            m_storageMax = storageMax;
            m_storageMin = storageMin;

            m_storage = storage;
            InitVoxelMap(positionMinCorner, Size,false);
        }
   
        public override void UpdateOnceBeforeFrame()
        {
            PositionComp.UpdateAABBHr();
            base.UpdateOnceBeforeFrame();
        }
        
        public bool GetContainedVoxelCoords(ref BoundingBoxD worldAabb, out Vector3I min, out Vector3I max)
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);
            min = default(Vector3I);
            max = default(Vector3I);

            if (!IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref worldAabb))
            {
                return false;
            }

            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref worldAabb.Min, out min);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref worldAabb.Max, out max);

            Storage.ClampVoxelCoord(ref min);
            Storage.ClampVoxelCoord(ref max);

            return true;
        }

        // mk:TODO Remove. This shouldn't be used anymore.
        public MyVoxelRangeType GetVoxelRangeTypeInBoundingBox(BoundingBoxD worldAabb)
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread);

            Vector3I minCorner, maxCorner;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref worldAabb.Min, out minCorner);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref worldAabb.Max, out maxCorner);

            Storage.ClampVoxelCoord(ref minCorner);
            Storage.ClampVoxelCoord(ref maxCorner);

            return MyVoxelRangeType.MIXED;
        }

        // mk:TODO Remove since it's inaccurate and hard to use.
        [Obsolete]
        override public float GetVoxelContentInBoundingBox_Obsolete(BoundingBoxD worldAabb, out float cellCount)
        {
            MyPrecalcComponent.AssertUpdateThread();

            cellCount = 0;
            float result = 0;

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
                            float content = m_storageCache.Content(ref cache) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT;
                            float containPercent = (float)(worldAabb.Intersect(voxelBox).Volume / MyVoxelConstants.VOXEL_VOLUME_IN_METERS);
                            result += content * containPercent;
                            cellCount += containPercent;
                        }
                    }
                }
            }
            return result;
        }

        public override bool IsAnyAabbCornerInside(BoundingBoxD worldAabb)
        {
            MyRenderProxy.DebugDrawAABB(worldAabb, Color.White, 1f, 1f, true);

            unsafe
            {
                Vector3D* corners = stackalloc Vector3D[8];
                worldAabb.GetCornersUnsafe(corners);
                //byte insideMask = 0;
                for (int i = 0; i < 8; i++)
                {
                    Vector3D local;
                    Vector3I min;
                    Vector3D minRel;
                    MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref corners[i], out local);
                    MyVoxelCoordSystems.LocalPositionToVoxelCoord(ref local, out min);
                    MyVoxelCoordSystems.LocalPositionToVoxelCoord(ref local, out minRel);
                    minRel -= (Vector3D)min;
                    var max = min + 1;
                    m_storageCache.Resize(min, max);
                    // mk:TODO Could be improved to not load the same range for each corner if they are inside the same voxel.
                    Storage.ReadRange(m_storageCache, MyStorageDataTypeFlags.Content, 0, ref min, ref max);

                    // Don't really need doubles but since position is in double and C# doesn't do SIMD yet, this makes little difference.
                    var c000 = (double)m_storageCache.Content(0, 0, 0);
                    var c100 = (double)m_storageCache.Content(1, 0, 0);
                    var c010 = (double)m_storageCache.Content(0, 1, 0);
                    var c110 = (double)m_storageCache.Content(1, 1, 0);
                    var c001 = (double)m_storageCache.Content(0, 0, 1);
                    var c101 = (double)m_storageCache.Content(1, 0, 1);
                    var c011 = (double)m_storageCache.Content(0, 1, 1);
                    var c111 = (double)m_storageCache.Content(1, 1, 1);

                    c000 = c000 + (c100 - c000) * minRel.X;
                    c010 = c010 + (c110 - c010) * minRel.X;
                    c001 = c001 + (c101 - c001) * minRel.X;
                    c011 = c011 + (c111 - c011) * minRel.X;

                    c000 = c000 + (c010 - c000) * minRel.Y;
                    c001 = c001 + (c011 - c001) * minRel.Y;

                    c000 = c000 + (c001 - c000) * minRel.Z;

                    //Color color = Color.Green;
                    if (c000 >= (double)MyVoxelConstants.VOXEL_ISO_LEVEL)
                    {
                        return true;
                        //insideMask |= (byte)(1 << i);
                        //color = Color.Red;
                    }
                    //MyRenderProxy.DebugDrawText3D(corners[i], c000.ToString("000.0"), color, 0.7f, false);
                }

                return false;
                //return insideMask != 0;
            }
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


        //collisions
        //sphere vs voxel volumetric test
        // mk:TODO Remove. This is not very accurate.
        public override bool DoOverlapSphereTest(float sphereRadius, Vector3D spherePos)
        {
            ProfilerShort.Begin("MyVoxelMap.DoOverlapSphereTest");
            Vector3D body0Pos = spherePos; // sphere pos
            BoundingSphereD newSphere;
            newSphere.Center = body0Pos;
            newSphere.Radius = sphereRadius;

            //  We will iterate only voxels contained in the bounding box of new sphere, so here we get min/max corned in voxel units
            Vector3I minCorner, maxCorner;
            {
                Vector3D sphereMin = newSphere.Center - newSphere.Radius - MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                Vector3D sphereMax = newSphere.Center + newSphere.Radius + MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref sphereMin, out minCorner);
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(PositionLeftBottomCorner, ref sphereMax, out maxCorner);
            }
            Storage.ClampVoxelCoord(ref minCorner);
            Storage.ClampVoxelCoord(ref maxCorner);
            m_storageCache.Resize(minCorner, maxCorner);
            Storage.ReadRange(m_storageCache, MyStorageDataTypeFlags.Content, 0, ref minCorner, ref maxCorner);

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

                        Vector3D voxelPosition;
                        MyVoxelCoordSystems.VoxelCoordToWorldPosition(PositionLeftBottomCorner, ref tempVoxelCoord, out voxelPosition);

                        float voxelSize = (voxelContent / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT) * MyVoxelConstants.VOXEL_RADIUS;

                        //  If distance to voxel border is less than sphere radius, we have a collision
                        //  So now we calculate normal vector and penetration depth but on OLD sphere
                        float newDistanceToVoxel = Vector3.Distance(voxelPosition, newSphere.Center) - voxelSize;
                        if (newDistanceToVoxel < (newSphere.Radius))
                        {
                            ProfilerShort.End();
                            return true;
                        }
                    }
                }
            }
            ProfilerShort.End();
            return false;
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

        //  Method finds intersection with line and any voxel triangleVertexes in this voxel map. Closes intersection is returned.
        internal override bool GetIntersectionWithLine(ref LineD worldLine, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            t = null;

            double intersectionDistance;
            LineD line = (LineD)worldLine;
            if (!PositionComp.WorldAABB.Intersects(line, out intersectionDistance))
                return false;

            ProfilerShort.Begin("VoxelMap.LineIntersection");
            try
            {
                Line localLine = new Line(worldLine.From - PositionLeftBottomCorner,
                                          worldLine.To - PositionLeftBottomCorner, true);
                MyIntersectionResultLineTriangle tmpResult;
                if (Storage.Geometry.Intersect(ref localLine, out tmpResult, flags))
                {
                    t = new MyIntersectionResultLineTriangleEx(tmpResult, this, ref worldLine);
                    var tmp = t.Value.IntersectionPointInWorldSpace;
                    tmp.AssertIsValid();
                    return true;
                }
                else
                {
                    t = null;
                    return false;
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public override bool GetIntersectionWithLine(ref LineD worldLine, out Vector3D? v, bool useCollisionModel = true, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
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

        //  This method must be called when this object dies or is removed
        //  E.g. it removes lights, sounds, etc
        protected override void BeforeDelete()
        {           
            base.BeforeDelete();
            // mk:TODO Get rid of this check. Should be separate type for subparts of planets.
            if (Render is MyRenderComponentVoxelMap)
            {
                (Render as MyRenderComponentVoxelMap).CancelAllRequests();
            }
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

            Debug.Assert(
                minChanged.IsInsideInclusive(ref m_storageMin, ref m_storageMax) &&
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

        public void OnStorageChanged(Vector3I minChanged, Vector3I maxChanged, MyStorageDataTypeFlags dataChanged)
        {
            storage_RangeChanged(minChanged, maxChanged, dataChanged);
        }

        protected override void InitVoxelMap(Vector3D positionMinCorner, Vector3I size,bool useOffset = true)
        {
            base.InitVoxelMap(positionMinCorner, size, useOffset);
            Physics = new MyVoxelPhysicsBody(this);
            Physics.Enabled = true;
        }

        public override void Init(string storageName, IMyStorage storage, Vector3D positionMinCorner)
        {
            m_storageMax = storage.Size;
            base.Init(storageName, storage, positionMinCorner);

            m_storage.RangeChanged += storage_RangeChanged;

        }
     
    }
}