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
using VRage.Network;
using Sandbox.Game.World;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Character;

namespace Sandbox.Game.Entities
{
    public abstract class MyVoxelBase : MyEntity, IMyVoxelDrawable, IMyVoxelBase, IMyEventProxy
    {
        struct MyRampShapeParams
        {
            public BoundingBoxD Box;
            public Vector3D RampNormal;
            public double RampNormalW;
            public MatrixD Transformation;
            public byte Material;
        }

        struct MyCapsuleShapeParams
        {
            public Vector3D A;
            public Vector3D B;
            public float Radius;
            public MatrixD Transformation;
            public byte Material;
        }


        public enum OperationType : byte
        {
            Fill,
            Paint,
            Cut
        }
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
            protected set;
        }
        public Vector3 SizeInMetresHalf
        {
            get;
            protected set;
        }

        /// <summary>
        /// Position of left/bottom corner of this voxel map, in world space (not relative to sector)
        /// </summary>
        virtual public Vector3D PositionLeftBottomCorner
        {
            get;
            set;
        }

        public Matrix Orientation
        {
            get { return PositionComp.WorldMatrix; }
        }

        bool m_contentChanged = false;
        public bool ContentChanged 
        {
            get
            {
                return m_contentChanged;
            }

            protected set
            {
                m_contentChanged = value;
                BeforeContentChanged = false;
            }

        }

        

        public bool BeforeContentChanged { get; protected set; }

        protected static MyStorageDataCache m_storageCache = new MyStorageDataCache();

        static MyShapeSphere m_sphereShape = new MyShapeSphere();
        static MyShapeBox m_boxShape = new MyShapeBox();
        static MyShapeRamp m_rampShape = new MyShapeRamp();
        static MyShapeCapsule m_capsuleShape = new MyShapeCapsule();
        static MyShapeEllipsoid m_ellipsoidShape = new MyShapeEllipsoid();

        static List<MyEntity> m_foundElements = new List<MyEntity>();

        //  Checks if specified box intersects bounding box of this this voxel map.
        public bool IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref BoundingBoxD boundingBox)
        {
            bool outRet;
            PositionComp.WorldAABB.Intersects(ref boundingBox, out outRet);
            return outRet;
        }


        public void Init(string storageName, IMyStorage storage, Vector3D positionMinCorner)
        {
            MatrixD worldMatrix = MatrixD.CreateTranslation(positionMinCorner + storage.Size / 2);
            Init(storageName, storage, worldMatrix);
        }

        virtual public void Init(string storageName, IMyStorage storage, MatrixD worldMatrix)
        {
            SyncFlag = true;

            base.Init(null);

            StorageName = storageName;
            m_storage = storage;

            InitVoxelMap(worldMatrix, storage.Size);
        }

        //  This method initializes voxel map (size, position, etc) but doesn't load voxels
        //  It only presets all materials to values specified in 'defaultMaterial' - so it will become material everywhere.
        virtual protected void InitVoxelMap(MatrixD worldMatrix, Vector3I size, bool useOffset = true)
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            var defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();

            SizeInMetres = size * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            SizeInMetresHalf = SizeInMetres / 2.0f;

            PositionComp.LocalAABB = new BoundingBox(-SizeInMetresHalf, SizeInMetresHalf);
            if (MyPerGameSettings.OffsetVoxelMapByHalfVoxel && useOffset)
            {
                worldMatrix.Translation += MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
                PositionLeftBottomCorner += MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            }

            PositionComp.SetWorldMatrix(worldMatrix);

            Debug.Assert((Size.X & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
            Debug.Assert((Size.Y & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
            Debug.Assert((Size.Z & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);

            Debug.Assert((Size.X % MyVoxelCoordSystems.RenderCellSizeInLodVoxels(0)) == 0);
            Debug.Assert((Size.Y % MyVoxelCoordSystems.RenderCellSizeInLodVoxels(0)) == 0);
            Debug.Assert((Size.Z % MyVoxelCoordSystems.RenderCellSizeInLodVoxels(0)) == 0);

            ContentChanged = false;
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
            voxelMapBuilder.ContentChanged = ContentChanged;

            return voxelMapBuilder;
        }

        protected void WorldPositionChanged(object source)
        {
            this.PositionLeftBottomCorner = this.WorldMatrix.Translation - Vector3D.TransformNormal(this.SizeInMetresHalf, WorldMatrix);
            //PositionComp.WorldAABB = PositionComp;
            //PositionComp.WorldVolume = BoundingSphereD.CreateFromBoundingBox(PositionComp.WorldAABB);

            //if (Render is MyRenderComponentVoxelMap)
            //{
            //    (Render as MyRenderComponentVoxelMap).UpdateCells();
            //}
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

        public virtual int GetOrePriority()
        {
            return MyVoxelConstants.PRIORITY_NORMAL;
        }

        public void RequestVoxelCutoutSphere(Vector3D center, float radius, bool createDebris)
        {
            BeforeContentChanged = true;
            MyMultiplayer.RaiseEvent(this, x => x.VoxelCutoutSphere_Implemenentation, center, radius, createDebris);
            if (Sync.IsServer)
            {
                MyExplosion.CutOutVoxelMap(radius, center, this, createDebris && MySession.Ready);
            }
        }

        [Event, Reliable, Server, Broadcast, RefreshReplicable]
        public void VoxelCutoutSphere_Implemenentation(Vector3D center, float radius, bool createDebris)
        {
            MyExplosion.CutOutVoxelMap(radius, center, this, createDebris && MySession.Ready);
        }

        public void RequestVoxelOperationCapsule(Vector3D A, Vector3D B, float radius, MatrixD Transformation, byte material, OperationType Type)
        {
            BeforeContentChanged = true;
            MyCapsuleShapeParams shapeParams = new MyCapsuleShapeParams();
            shapeParams.A = A;
            shapeParams.B = B;
            shapeParams.Radius = radius;
            shapeParams.Transformation = Transformation;
            shapeParams.Material = material;

            MyMultiplayer.RaiseStaticEvent(s => VoxelOperationCapsule_Implementation, EntityId, shapeParams, Type);
        }

        [Event, Reliable, Server, RefreshReplicable]
        static void VoxelOperationCapsule_Implementation(long entityId, MyCapsuleShapeParams capsuleParams, OperationType Type)
        {
            m_capsuleShape.Transformation =capsuleParams.Transformation;
            m_capsuleShape.A = capsuleParams.A;
            m_capsuleShape.B = capsuleParams.B;
            m_capsuleShape.Radius = capsuleParams.Radius;

            if (CanPlaceInArea(Type, m_capsuleShape))
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(entityId, out entity);
                MyVoxelBase voxel = entity as MyVoxelBase;
                if (voxel != null)
                {
                    voxel.BeforeContentChanged = true;
                    MyMultiplayer.RaiseEvent(voxel, x => x.PerformVoxelOperationCapsule_Implementation, capsuleParams, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_capsuleShape, capsuleParams.Material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }
            }
        }

        [Event, Reliable, Broadcast, RefreshReplicable]
        void PerformVoxelOperationCapsule_Implementation(MyCapsuleShapeParams capsuleParams, OperationType Type)
        {
            m_capsuleShape.Transformation = capsuleParams.Transformation;
            m_capsuleShape.A = capsuleParams.A;
            m_capsuleShape.B = capsuleParams.B;
            m_capsuleShape.Radius = capsuleParams.Radius;
            var amountChanged = UpdateVoxelShape(Type, m_capsuleShape, capsuleParams.Material);
            if (Type == OperationType.Cut || Type == OperationType.Fill)
            {
                MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }


        public void RequestVoxelOperationSphere(Vector3D center, float radius, byte material, OperationType Type)
        {
            BeforeContentChanged = true;
            MyMultiplayer.RaiseStaticEvent(s => VoxelOperationSphere_Implementation, EntityId, center, radius, material, Type);
        }

        [Event, Reliable, Server]
        static void VoxelOperationSphere_Implementation(long entityId,Vector3D center, float radius, byte material, OperationType Type)
        {
            m_sphereShape.Center = center;
            m_sphereShape.Radius = radius;

            if (CanPlaceInArea(Type, m_sphereShape))
            { 
                MyEntity entity;
                MyEntities.TryGetEntityById(entityId, out entity);
                MyVoxelBase voxel = entity as MyVoxelBase;
                if (voxel != null)
                {
                    voxel.BeforeContentChanged = true;
                    MyMultiplayer.RaiseEvent(voxel, x => x.PerformVoxelOperationSphere_Implementation, center, radius, material, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_sphereShape, material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }     
            }
        }

        [Event, Reliable, Broadcast, RefreshReplicable]
        public void PerformVoxelOperationSphere_Implementation(Vector3D center, float radius, byte material, OperationType Type)
        {
            m_sphereShape.Center = center;
            m_sphereShape.Radius = radius;

            var amountChanged = UpdateVoxelShape(Type, m_sphereShape, material);
            if (Type == OperationType.Cut || Type == OperationType.Fill)
            {
                MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }


        public void RequestVoxelOperationBox(BoundingBoxD box, MatrixD Transformation, byte material, OperationType Type)
        {
            BeforeContentChanged = true;
            MyMultiplayer.RaiseStaticEvent(s => VoxelOperationBox_Implementation, EntityId, box, Transformation, material, Type);
        }

        [Event, Reliable, Server, RefreshReplicable]
        static void VoxelOperationBox_Implementation(long entityId, BoundingBoxD box, MatrixD Transformation, byte material, OperationType Type)
        {
            m_boxShape.Transformation = Transformation;
            m_boxShape.Boundaries.Max = box.Max;
            m_boxShape.Boundaries.Min = box.Min;

            if (CanPlaceInArea(Type, m_boxShape))
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(entityId, out entity);
                MyVoxelBase voxel = entity as MyVoxelBase;
                if (voxel != null)
                {
                    voxel.BeforeContentChanged = true;
                    MyMultiplayer.RaiseEvent(voxel, x => x.PerformVoxelOperationBox_Implementation, box, Transformation, material, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_boxShape, material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }
               
            }
        }

        [Event, Reliable, Broadcast]
        public void PerformVoxelOperationBox_Implementation(BoundingBoxD box, MatrixD Transformation, byte material, OperationType Type)
        {
            m_boxShape.Transformation = Transformation;
            m_boxShape.Boundaries.Max = box.Max;
            m_boxShape.Boundaries.Min = box.Min;

            var amountChanged = UpdateVoxelShape(Type, m_boxShape, material);
            if (Type == OperationType.Cut || Type == OperationType.Fill)
            {
                MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }

        public void RequestVoxelOperationRamp(BoundingBoxD box, Vector3D rampNormal, double rampNormalW, MatrixD Transformation, byte material, OperationType Type)
        {
            BeforeContentChanged = true;
            MyRampShapeParams shapeParams = new MyRampShapeParams();
            shapeParams.Box = box;
            shapeParams.RampNormal = rampNormal;
            shapeParams.RampNormalW = rampNormalW;
            shapeParams.Transformation = Transformation;
            shapeParams.Material = material;

            MyMultiplayer.RaiseStaticEvent(s => VoxelOperationRamp_Implementation, EntityId, shapeParams, Type);
        }

        [Event, Reliable, Server, RefreshReplicable]
        static void VoxelOperationRamp_Implementation(long entityId, MyRampShapeParams shapeParams, OperationType Type)
        {
            m_rampShape.Transformation = shapeParams.Transformation;
            m_rampShape.Boundaries.Max = shapeParams.Box.Max;
            m_rampShape.Boundaries.Min = shapeParams.Box.Min;
            m_rampShape.RampNormal = shapeParams.RampNormal;
            m_rampShape.RampNormalW = shapeParams.RampNormalW;

            if (CanPlaceInArea(Type, m_rampShape))
            {
               MyEntity entity;
                MyEntities.TryGetEntityById(entityId, out entity);
                MyVoxelBase voxel = entity as MyVoxelBase;
                if (voxel != null)
                {
                    voxel.BeforeContentChanged = true;
                    MyMultiplayer.RaiseEvent(voxel, x => x.PerformVoxelOperationRamp_Implementation, shapeParams, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_rampShape, shapeParams.Material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }
            }
        }

        [Event, Reliable, Broadcast]
        void PerformVoxelOperationRamp_Implementation(MyRampShapeParams shapeParams, OperationType Type)
        {
            m_rampShape.Transformation = shapeParams.Transformation;
            m_rampShape.Boundaries.Max = shapeParams.Box.Max;
            m_rampShape.Boundaries.Min = shapeParams.Box.Min;
            m_rampShape.RampNormal = shapeParams.RampNormal;
            m_rampShape.RampNormalW = shapeParams.RampNormalW;

            var amountChanged = UpdateVoxelShape(Type, m_rampShape, shapeParams.Material);
            if (Type == OperationType.Cut || Type == OperationType.Fill)
            {
                MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }

        public void RequestVoxelOperationElipsoid(Vector3 radius, MatrixD Transformation, byte material, OperationType Type)
        {
            BeforeContentChanged = true;
            MyMultiplayer.RaiseStaticEvent(s => VoxelOperationElipsoid_Implementation,EntityId, radius, Transformation, material, Type);
        }

        [Event, Reliable, Server, RefreshReplicable]
        static void VoxelOperationElipsoid_Implementation(long entityId,Vector3 radius, MatrixD Transformation, byte material, OperationType Type)
        {
            m_ellipsoidShape.Transformation =Transformation;
            m_ellipsoidShape.Radius = radius;
            if (CanPlaceInArea(Type, m_ellipsoidShape))
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(entityId, out entity);
                MyVoxelBase voxel = entity as MyVoxelBase;
                if (voxel != null)
                {
                    voxel.BeforeContentChanged = true;
                    MyMultiplayer.RaiseEvent(voxel, x => x.PerformVoxelOperationElipsoid_Implementation, radius, Transformation, material, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_ellipsoidShape, material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }

            }
        }

        [Event, Reliable, Broadcast]
        public void PerformVoxelOperationElipsoid_Implementation(Vector3 radius, MatrixD Transformation, byte material, OperationType Type)
        {
            m_ellipsoidShape.Transformation = Transformation;
            m_ellipsoidShape.Radius = radius;
            var amountChanged = UpdateVoxelShape(Type, m_ellipsoidShape, material);
            if (Type == OperationType.Cut || Type == OperationType.Fill)
            {
                MySession.Static.VoxelHandVolumeChanged += amountChanged;
            }
        }

        static bool CanPlaceInArea(OperationType type, MyShape Shape)
        {
            if (type == OperationType.Fill)
            {
                m_foundElements.Clear();
                BoundingBoxD box = Shape.GetWorldBoundaries();
                MyEntities.GetElementsInBox(ref box, m_foundElements);
                foreach (var entity in m_foundElements)
                {
                    if (IsForbiddenEntity(entity))
                    {
                        if (entity.PositionComp.WorldAABB.Intersects(box))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        static public  bool IsForbiddenEntity(MyEntity entity)
        {
            return (entity is MyCharacter ||
                        (entity is MyCubeGrid && (entity as MyCubeGrid).IsStatic == false) ||
                        (entity is MyCockpit && (entity as MyCockpit).Pilot != null));
        }

        private ulong UpdateVoxelShape(OperationType type, MyShape shape, byte Material)
        {
            ulong changedVoxelAmount = 0;

            switch (type)
            {
                case OperationType.Paint:
                    MyVoxelGenerator.PaintInShape(this, shape, Material);
                    break;
                case OperationType.Fill:
                    changedVoxelAmount = MyVoxelGenerator.FillInShape(this, shape, Material);
                    break;
                case OperationType.Cut:
                    changedVoxelAmount = MyVoxelGenerator.CutOutShape(this, shape);
                    break;
            }

            return changedVoxelAmount;
        }

        public void CreateVoxelMeteorCrater(Vector3D center, float radius, Vector3 normal, MyVoxelMaterialDefinition material)
        {
            BeforeContentChanged = true;
            MyMultiplayer.RaiseEvent(this, x => x.CreateVoxelMeteorCrater_Implementation, center, radius, normal, material.Index);
        }

        [Event, Reliable, Broadcast]
        public void CreateVoxelMeteorCrater_Implementation(Vector3D center, float radius, Vector3 normal, byte material)
        {
            MyVoxelGenerator.MakeCrater(this, new BoundingSphere(center, radius), normal, MyDefinitionManager.Static.GetVoxelMaterialDefinition(material));
        }
    }
}
