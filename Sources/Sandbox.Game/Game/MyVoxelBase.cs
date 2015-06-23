using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRage.Game;
using VRage.Components;
using Sandbox.Engine.Utils;
using System.Threading;

namespace Sandbox.Game.Entities
{
    public abstract class MyVoxelBase : MyEntity, IMyVoxelDrawable, IMyVoxelBase
    {
        public int VoxelMapPruningProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;

        protected Vector3I m_storageMin = new Vector3I(0, 0, 0);
        public Vector3I StorageMin
        {
            get
            {
                return m_storageMin;
            }
        }

        protected Vector3I m_storageMax;
        public Vector3I StorageMax
        {
            get
            {
                return m_storageMax;
            }
        }

        public string StorageName
        {
            get;
            protected set;
        }

        protected IMyStorage m_storage;
        public IMyStorage Storage
        {
            get { return m_storage; }
        }

        /// <summary>
        /// Size of voxel map (in voxels)
        /// </summary>
        public Vector3I Size
        {
            get { return m_storageMax - m_storageMin; }
        }
        public Vector3I SizeMinusOne
        {
            get { return Size - 1; }
        }

        /// <summary>
        /// Size of voxel map (in metres)
        /// </summary>
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

        /// <summary>
        /// Position of left/bottom corner of this voxel map, in world space (not relative to sector)
        /// </summary>
        virtual public Vector3D PositionLeftBottomCorner
        {
            get;
            set;
        }

        protected static MyStorageDataCache m_storageCache = new MyStorageDataCache();

        //  Checks if specified box intersects bounding box of this this voxel map.
        public bool IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref BoundingBoxD boundingBox)
        {
            bool outRet;
            PositionComp.WorldAABB.Intersects(ref boundingBox, out outRet);
            return outRet;
        }

        virtual public void Init(string storageName, IMyStorage storage, Vector3D positionMinCorner)
        {
            SyncFlag = true;

            base.Init(null);

            StorageName = storageName;
            m_storage = storage;
          
            InitVoxelMap(positionMinCorner, storage.Size);
        }

        //  This method initializes voxel map (size, position, etc) but doesn't load voxels
        //  It only presets all materials to values specified in 'defaultMaterial' - so it will become material everywhere.
        virtual protected void InitVoxelMap(Vector3D positionMinCorner, Vector3I size, bool useOffset = true)
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            var defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();

            SizeInMetres = size * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            SizeInMetresHalf = SizeInMetres / 2.0f;

            PositionComp.LocalAABB = new BoundingBox(-SizeInMetresHalf, SizeInMetresHalf);
            if (MyPerGameSettings.OffsetVoxelMapByHalfVoxel && useOffset)
                positionMinCorner += MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            PositionLeftBottomCorner = positionMinCorner;
            PositionComp.SetWorldMatrix(MatrixD.CreateTranslation(PositionLeftBottomCorner + SizeInMetresHalf));

            //  Voxel map size must be multiple of a voxel data cell size.
            MyDebug.AssertRelease((Size.X & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
            MyDebug.AssertRelease((Size.Y & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
            MyDebug.AssertRelease((Size.Z & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);

            //  Voxel map size must be multiple of a voxel data cell size.
            MyDebug.AssertRelease((Size.X % MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS) == 0);
            MyDebug.AssertRelease((Size.Y % MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS) == 0);
            MyDebug.AssertRelease((Size.Z % MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS) == 0);         
        }

        virtual public MySyncVoxel GetSyncObject
        {
            get { return (MySyncVoxel)base.SyncObject; }
        }

        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncVoxel(this);
        }

        ModAPI.Interfaces.IMyStorage IMyVoxelBase.Storage
        {
            get { return (Storage as ModAPI.Interfaces.IMyStorage); }
        }

        string IMyVoxelBase.StorageName
        {
            get { return StorageName; }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_VoxelMap voxelMapBuilder = (MyObjectBuilder_VoxelMap)base.GetObjectBuilder(copy);

            var minCorner = PositionLeftBottomCorner;
            if (MyPerGameSettings.OffsetVoxelMapByHalfVoxel)
                minCorner -= MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;

            voxelMapBuilder.PositionAndOrientation = new MyPositionAndOrientation(minCorner, Vector3.Forward, Vector3.Up);
            voxelMapBuilder.StorageName = StorageName;
            voxelMapBuilder.MutableStorage = true;

            return voxelMapBuilder;
        }

        protected void WorldPositionChanged(object source)
        {
            if (Render is MyRenderComponentVoxelMap)
            {
                (Render as MyRenderComponentVoxelMap).UpdateCells();
            }
        }

      
        [Obsolete]
        public float GetVoxelContentInBoundingBox_Obsolete(BoundingBoxD worldAabb, out float cellCount)
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
                        MyVoxelCoordSystems.VoxelCoordToWorldAABB(PositionLeftBottomCorner - StorageMin * MyVoxelConstants.VOXEL_SIZE_IN_METRES, ref coord, out voxelBox);
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

        public bool AreAllAabbCornersInside(ref MatrixD aabbWorldTransform, BoundingBoxD aabb)
        {
            return CountCornersInside(ref aabbWorldTransform, ref aabb) == 8;
        }

        public bool IsAnyAabbCornerInside(ref MatrixD aabbWorldTransform, BoundingBoxD aabb)
        {
            return CountCornersInside(ref aabbWorldTransform, ref aabb) > 0;
        }

        private int CountCornersInside(ref MatrixD aabbWorldTransform, ref BoundingBoxD aabb)
        {
            unsafe
            {
                const int cornerCount = 8;
                Vector3D* corners = stackalloc Vector3D[cornerCount];
                aabb.GetCornersUnsafe(corners);
                for (int i = 0; i < cornerCount; i++)
                {
                    Vector3D.Transform(ref corners[i], ref aabbWorldTransform, out corners[i]);
                }
                return CountPointsInside(corners, cornerCount);
            }
        }

        private unsafe int CountPointsInside(Vector3D* worldPoints, int pointCount)
        {
            int pointCountInside = 0;
            Vector3I oldMin, oldMax;
            oldMin = new Vector3I(int.MaxValue);
            oldMax = new Vector3I(int.MinValue);
            for (int i = 0; i < pointCount; i++)
            {
                Vector3D local;
                Vector3I min;
                Vector3D minRel;
                MyVoxelCoordSystems.WorldPositionToLocalPosition(PositionLeftBottomCorner, ref worldPoints[i], out local);
                MyVoxelCoordSystems.LocalPositionToVoxelCoord(ref local, out min);
                MyVoxelCoordSystems.LocalPositionToVoxelCoord(ref local, out minRel);
                minRel -= (Vector3D)min;
                var max = min + 1;
                if (min != oldMin && max != oldMax)
                { // load only if range has changed
                    m_storageCache.Resize(min, max);
                    Storage.ReadRange(m_storageCache, MyStorageDataTypeFlags.Content, 0, ref min, ref max);
                    oldMin = min;
                    oldMax = max;
                }

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
                    pointCountInside++;
                    //color = Color.Red;
                }
                //VRageRender.MyRenderProxy.DebugDrawText3D(worldPoints[i], c000.ToString("000.0"), color, 0.7f, false);
            }

            return pointCountInside;
        }

        public virtual bool IsOverlapOverThreshold(BoundingBoxD worldAabb, float thresholdPercentage = 0.9f)
        {
            return false;
        }

        virtual public MyClipmapScaleEnum ScaleGroup
        {
            get
            {
                return MyClipmapScaleEnum.Normal;
            }
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

            minCorner += StorageMin;
            maxCorner += StorageMin;

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
                        MyVoxelCoordSystems.VoxelCoordToWorldPosition(PositionLeftBottomCorner - StorageMin * MyVoxelConstants.VOXEL_SIZE_IN_METRES, ref tempVoxelCoord, out voxelPosition);

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
            min += StorageMin;
            max += StorageMin;

            Storage.ClampVoxelCoord(ref min);
            Storage.ClampVoxelCoord(ref max);

            return true;
        }
    }
}
