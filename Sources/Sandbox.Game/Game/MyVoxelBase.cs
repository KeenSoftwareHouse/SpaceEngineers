using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRageRender;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.Game;
using VRage.Game.Components;
using System.Threading;
using VRage.Network;
using Sandbox.Game.World;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Gui;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRage.Profiler;
using VRage.Voxels;

namespace Sandbox.Game.Entities
{
    public abstract class MyVoxelBase : MyEntity, IMyVoxelDrawable, IMyVoxelBase, IMyDecalProxy, IMyEventProxy
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

        public int VoxelMapPruningProxyId = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;

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

        protected Sandbox.Engine.Voxels.IMyStorage m_storage;
        public virtual Sandbox.Engine.Voxels.IMyStorage Storage
        {
            get { return m_storage; }
            set { }
        }

        public bool CreateStorageCopyOnWrite = false;

        public bool DelayRigidBodyCreation { get; set; }

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
        public virtual Vector3D PositionLeftBottomCorner
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
                //TODO: alwawys set false?
                BeforeContentChanged = false;
            }

        }

        /// <summary>
        /// Gets root voxel, for asteroids and planets itself.
        /// For MyVoxelPhysics, should return owning planet.
        /// </summary>
        public abstract MyVoxelBase RootVoxel { get; }

        bool m_beforeContentChanged = false;
        public bool BeforeContentChanged
        {
            get { return m_beforeContentChanged; }
            protected set
            {
                if (m_beforeContentChanged != value)
                {
                    m_beforeContentChanged = value;

                    if (m_beforeContentChanged && CreateStorageCopyOnWrite)
                    {
                        Storage = m_storage.Copy();
                        StorageName = MyVoxelMap.GetNewStorageName(StorageName);

                        CreateStorageCopyOnWrite = false;
                    }
                }
            }
        }

        protected static MyStorageData m_tempStorage = new MyStorageData();

        static MyShapeSphere m_sphereShape = new MyShapeSphere();
        static MyShapeBox m_boxShape = new MyShapeBox();
        static MyShapeRamp m_rampShape = new MyShapeRamp();
        static MyShapeCapsule m_capsuleShape = new MyShapeCapsule();
        static MyShapeEllipsoid m_ellipsoidShape = new MyShapeEllipsoid();

        static List<MyEntity> m_foundElements = new List<MyEntity>();

        public delegate void StorageChanged(MyVoxelBase storage, Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData);

        public event StorageChanged RangeChanged;

        public bool CreatedByUser
        {
            get;
            set;
        }

        public string AsteroidName
        {
            get;
            set;
        }

        protected internal void OnRangeChanged(Vector3I voxelRangeMin, Vector3I voxelRangeMax, MyStorageDataTypeFlags changedData)
        {
            if (RangeChanged != null)
            {
                RangeChanged(this, voxelRangeMin, voxelRangeMax, changedData);
            }
        }

        //  Checks if specified box intersects bounding box of this this voxel map.
        public bool IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref BoundingBoxD boundingBox)
        {
            bool outRet;
            PositionComp.WorldAABB.Intersects(ref boundingBox, out outRet);
            return outRet;
        }

        abstract public void Init(MyObjectBuilder_EntityBase builder, Sandbox.Engine.Voxels.IMyStorage storage);

        public void Init(string storageName, Sandbox.Engine.Voxels.IMyStorage storage, Vector3D positionMinCorner)
        {
            MatrixD worldMatrix = MatrixD.CreateTranslation(positionMinCorner + storage.Size / 2);
            Init(storageName, storage, worldMatrix);
        }

        public virtual void Init(string storageName, Sandbox.Engine.Voxels.IMyStorage storage, MatrixD worldMatrix)
        {
            ProfilerShort.Begin("MyVoxelBase::Init");
            SyncFlag = true;

            // Planet initalization needs to be re-done basically so issues like this don't exist anymore
            if (Name == null)
            base.Init(null);

            StorageName = storageName;
            m_storage = storage;

            CreateStorageCopyOnWrite = m_storage.Shared;

            InitVoxelMap(worldMatrix, storage.Size);

            ProfilerShort.End();
        }

        //  This method initializes voxel map (size, position, etc) but doesn't load voxels
        //  It only presets all materials to values specified in 'defaultMaterial' - so it will become material everywhere.
        protected virtual void InitVoxelMap(MatrixD worldMatrix, Vector3I size, bool useOffset = true)
        {
            ProfilerShort.Begin("InitVoxelMap");

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            SizeInMetres = size * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            SizeInMetresHalf = SizeInMetres / 2.0f;

            PositionComp.LocalAABB = new BoundingBox(-SizeInMetresHalf, SizeInMetresHalf);
            if (MyPerGameSettings.OffsetVoxelMapByHalfVoxel && useOffset)
            {
                worldMatrix.Translation += MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
                PositionLeftBottomCorner += MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            }

            PositionComp.SetWorldMatrix(worldMatrix);

            ContentChanged = false;

            ProfilerShort.End();
        }

        protected override void BeforeDelete()
        {
            base.BeforeDelete();

            RangeChanged = null;

            if (Storage != null && !Storage.Shared && !(this is MyVoxelPhysics))
            {
                Storage.Close();
            }
        }

        #region ModAPI
        VRage.ModAPI.IMyStorage IMyVoxelBase.Storage
        {
            get { return Storage; }
        }

        string IMyVoxelBase.StorageName
        {
            get { return StorageName; }
        }

        IMyVoxelBase IMyVoxelBase.RootVoxel
        {
            get { return RootVoxel; }
        }

        int IMyVoxelBase.CountCornersInside(MatrixD aabbWorldTransform, BoundingBoxD aabb)
        {
            return CountCornersInside(ref aabbWorldTransform, ref aabb);
        }

        bool IMyVoxelBase.AreAllAabbCornersInside(MatrixD aabbWorldTransform, BoundingBoxD aabb)
        {
            return AreAllAabbCornersInside(ref aabbWorldTransform, aabb);
        }

        bool IMyVoxelBase.IsAnyAabbCornerInside(MatrixD aabbWorldTransform, BoundingBoxD aabb)
        {
            return IsAnyAabbCornerInside(ref aabbWorldTransform, aabb);
        }

        void IMyVoxelBase.CreateMeteorCrater(Vector3D center, float radius, Vector3 normal, byte materialIdx)
        {
            Debug.Assert(Sync.IsServer);
            var material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIdx);

            if (Sync.IsServer)
            {
                CreateVoxelMeteorCrater(center, radius, normal, material);
                MyVoxelGenerator.MakeCrater(this, new BoundingSphere(center, radius), normal, material);
            }
        }

        void IMyVoxelBase.VoxelCutoutSphere(Vector3D center, float radius, bool createDebris, bool damage)
        {
            RequestVoxelCutoutSphere(center, radius, createDebris, damage);
        }

        void IMyVoxelBase.VoxelOperationCapsule(Vector3D A, Vector3D B, float radius, MatrixD Transformation, byte material, OperationType operation)
        {
            RequestVoxelOperationCapsule(A, B, radius, Transformation, material, operation);
        }

        void IMyVoxelBase.VoxelOperationBox(BoundingBoxD box, MatrixD Transformation, byte material, OperationType operation)
        {
            RequestVoxelOperationBox(box, Transformation, material, operation);
        }

        void IMyVoxelBase.VoxelOperationElipsoid(Vector3 radius, MatrixD Transformation, byte material, OperationType operation)
        {
            RequestVoxelOperationElipsoid(radius, Transformation, material, operation);
        }

        void IMyVoxelBase.VoxelOperationRamp(BoundingBoxD box, Vector3D rampNormal, double rampNormalW, MatrixD Transformation, byte material, OperationType operation)
        {
            RequestVoxelOperationRamp(box, rampNormal, rampNormalW, Transformation, material, operation);
        }

        void IMyVoxelBase.VoxelOperationSphere(Vector3D center, float radius, byte material, OperationType operation)
        {
            RequestVoxelOperationSphere(center, radius, material, operation);
        }
        #endregion

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_VoxelMap voxelMapBuilder = (MyObjectBuilder_VoxelMap)base.GetObjectBuilder(copy);

            var minCorner = PositionLeftBottomCorner;

            this.PositionLeftBottomCorner = this.WorldMatrix.Translation - Vector3D.TransformNormal(this.SizeInMetresHalf, WorldMatrix);

            if (MyPerGameSettings.OffsetVoxelMapByHalfVoxel)
                minCorner -= MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;

            voxelMapBuilder.PositionAndOrientation = new MyPositionAndOrientation(minCorner, WorldMatrix.Forward, WorldMatrix.Up);
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
            m_tempStorage.Resize(minCorner, maxCorner);
            Storage.ReadRange(m_tempStorage, MyStorageDataTypeFlags.Content, 0, ref minCorner, ref maxCorner);

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
                            float content = m_tempStorage.Content(ref cache) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT;
                            float containPercent = (float)(worldAabb.Intersect(voxelBox).Volume / MyVoxelConstants.VOXEL_VOLUME_IN_METERS);
                            result += content * containPercent;
                            cellCount += containPercent;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates amount of volume of a bounding box in voxels.
        /// </summary>
        /// <param name="localAabb">Local bounding box to query for.</param>
        /// <param name="worldMatrix">World matrix of the bounding box.</param>
        /// <returns>Pair of floats where 1st value is Volume amount and 2nd value is ratio of Volume amount to Whole volume.</returns>
        public MyTuple<float,float> GetVoxelContentInBoundingBox_Fast(BoundingBoxD localAabb, MatrixD worldMatrix)
        {
            MatrixD toVoxel = worldMatrix * PositionComp.WorldMatrixNormalizedInv;
            MatrixD toGrid; MatrixD.Invert(ref toVoxel, out toGrid);

            BoundingBoxD transAABB = localAabb.TransformFast(toVoxel);
            transAABB.Translate(SizeInMetresHalf + StorageMin);
            Vector3I minI = Vector3I.Floor(transAABB.Min);
            Vector3I maxI = Vector3I.Ceiling(transAABB.Max);

            double vol = localAabb.Volume / MyVoxelConstants.VOXEL_VOLUME_IN_METERS;
            int K = Math.Max((MathHelper.Log2Ceiling((int)vol) - MathHelper.Log2Ceiling(100)) / 3, 0);
            float voxelSizeAtLod = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << K);
            float voxelVolumeAtLod = voxelSizeAtLod * voxelSizeAtLod * voxelSizeAtLod;
            minI >>= K;
            maxI >>= K;

           // localAabb.Inflate(1 * voxelSizeAtLod);

            var offset = ((Size >> 1) + StorageMin) >> K;

            m_tempStorage.Resize(maxI - minI + 1);
            Storage.ReadRange(m_tempStorage, MyStorageDataTypeFlags.Content, K, minI, maxI);

            float resultVolume = 0;
            float resultPercent = 0;
            int hitVolumeBoxes = 0;

            MyOrientedBoundingBoxD worldbbox = new MyOrientedBoundingBoxD(localAabb, worldMatrix);

            Vector3I coord, cache;
            for (coord.Z = minI.Z, cache.Z = 0; coord.Z <= maxI.Z; coord.Z++, cache.Z++)
            {
                for (coord.Y = minI.Y, cache.Y = 0; coord.Y <= maxI.Y; coord.Y++, cache.Y++)
                {
                    for (coord.X = minI.X, cache.X = 0; coord.X <= maxI.X; coord.X++, cache.X++)
                    {
                        Vector3D voxelPos = (coord - offset) * voxelSizeAtLod;

                        Vector3D gridPoint;
                        Vector3D.Transform(ref voxelPos, ref toGrid, out gridPoint);

                        ContainmentType cont;
                        //localAabb.Contains(ref gridPoint, out cont);

                        var voxelToWorld = WorldMatrix;
                        voxelToWorld.Translation -= (Vector3D)StorageMin + SizeInMetresHalf;

                        BoundingBoxD voxelBox = new BoundingBoxD();
                        voxelBox.Min = ((Vector3D)(coord) - .5) * voxelSizeAtLod;
                        voxelBox.Max = ((Vector3D)(coord) + .5) * voxelSizeAtLod;

                        MyOrientedBoundingBoxD voxelBbox = new MyOrientedBoundingBoxD(voxelBox, voxelToWorld);

                        cont = worldbbox.Contains(ref voxelBbox);

                        if (cont == ContainmentType.Disjoint)
                        {
                            //VRageRender.MyRenderProxy.DebugDrawOBB(
                            //new MyOrientedBoundingBoxD(voxelBox, voxelToWorld), Color.Red, 0.1f,
                            //true, false);
                            continue;
                        }

                        float content = m_tempStorage.Content(ref cache) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT;



                        //VRageRender.MyRenderProxy.DebugDrawOBB(voxelBbox, Color.Aqua, content,
                        //   true, false);

                        resultVolume += content * voxelVolumeAtLod;
                        resultPercent += content;
                        hitVolumeBoxes++;
                    }
                }
            }

            resultPercent /= hitVolumeBoxes; 
            //float localAABBVol = (float)localAabb.Volume;
            //if (localAABBVol < resultVolume)
            //    resultPercent *= (float)localAabb.Volume / resultVolume;


            //VRageRender.MyRenderProxy.DebugDrawOBB(worldbbox, Color.Yellow, 0,
            //                true, false);
            //VRageRender.MyRenderProxy.DebugWaitForFrameFinish();


            return new MyTuple<float, float>(resultVolume, resultPercent);
        }

        //  Method finds intersection with line and any voxel triangleVertexes in this voxel map. Closes intersection is returned.
        public override bool GetIntersectionWithLine(ref LineD worldLine, out VRage.Game.Models.MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            t = null;

            double intersectionDistance;
            LineD line = (LineD)worldLine;
            if (!PositionComp.WorldAABB.Intersects(ref line, out intersectionDistance))
                return false;

            ProfilerShort.Begin("VoxelMap.LineIntersection");
            try
            {
                Line localLine = new Line(worldLine.From - PositionLeftBottomCorner,
                                          worldLine.To - PositionLeftBottomCorner, true);
                VRage.Game.Models.MyIntersectionResultLineTriangle tmpResult;
                if (Storage.Geometry.Intersect(ref localLine, out tmpResult, flags))
                {
                    t = new VRage.Game.Models.MyIntersectionResultLineTriangleEx(tmpResult, this, ref worldLine);
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
            VRage.Game.Models.MyIntersectionResultLineTriangleEx? result;
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

        public unsafe int CountPointsInside(Vector3D* worldPoints, int pointCount)
        {
            MatrixD voxelTransform = PositionComp.WorldMatrixInvScaled;

            int pointCountInside = 0;
            Vector3I oldMin, oldMax;
            oldMin = new Vector3I(int.MaxValue);
            oldMax = new Vector3I(int.MinValue);
            for (int i = 0; i < pointCount; i++)
            {

                Vector3D local;
                Vector3D.Transform(ref worldPoints[i], ref voxelTransform, out local);

                Vector3D minRel = local + (Vector3D)(Size / 2);
                Vector3I min = (Vector3I)Vector3D.Floor(minRel);

                Vector3D.Fract(ref minRel, out minRel);

                min -= StorageMin;
                var max = min + 1;
                if (min != oldMin && max != oldMax)
                { // load only if range has changed
                    m_tempStorage.Resize(min, max);
                    Storage.ReadRange(m_tempStorage, MyStorageDataTypeFlags.Content, 0, ref min, ref max);
                    oldMin = min;
                    oldMax = max;
                }

                // Don't really need doubles but since position is in double and C# doesn't do SIMD yet, this makes little difference.
                var c000 = (double)m_tempStorage.Content(0, 0, 0);
                var c100 = (double)m_tempStorage.Content(1, 0, 0);
                var c010 = (double)m_tempStorage.Content(0, 1, 0);
                var c110 = (double)m_tempStorage.Content(1, 1, 0);
                var c001 = (double)m_tempStorage.Content(0, 0, 1);
                var c101 = (double)m_tempStorage.Content(1, 0, 1);
                var c011 = (double)m_tempStorage.Content(0, 1, 1);
                var c111 = (double)m_tempStorage.Content(1, 1, 1);

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

        public virtual MyClipmapScaleEnum ScaleGroup
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
            if (Storage.Closed) return false;

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
            m_tempStorage.Resize(minCorner, maxCorner);
            Storage.ReadRange(m_tempStorage, MyStorageDataTypeFlags.Content, 0, ref minCorner, ref maxCorner);

            Vector3I tempVoxelCoord, cache;
            for (tempVoxelCoord.Z = minCorner.Z, cache.Z = 0; tempVoxelCoord.Z <= maxCorner.Z; tempVoxelCoord.Z++, cache.Z++)
            {
                for (tempVoxelCoord.Y = minCorner.Y, cache.Y = 0; tempVoxelCoord.Y <= maxCorner.Y; tempVoxelCoord.Y++, cache.Y++)
                {
                    for (tempVoxelCoord.X = minCorner.X, cache.X = 0; tempVoxelCoord.X <= maxCorner.X; tempVoxelCoord.X++, cache.X++)
                    {
                        byte voxelContent = m_tempStorage.Content(ref cache);

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

        /**
         * We use this to ensure voxels overlapping planets will have priority when spawning debris/ores.
         */
        public virtual int GetOrePriority()
        {
            return MyVoxelConstants.PRIORITY_NORMAL;
        }

        void IMyDecalProxy.AddDecals(MyHitInfo hitInfo, MyStringHash source, object customdata, IMyDecalHandler decalHandler, MyStringHash material)
        {
            MyDecalRenderInfo renderable = new MyDecalRenderInfo();
            renderable.Flags = MyDecalFlags.World;
            renderable.Position = hitInfo.Position;
            renderable.Normal = hitInfo.Normal;
            renderable.RenderObjectId = Render.GetRenderObjectID();

            if (material.GetHashCode() == 0)
                renderable.Material = Physics.GetMaterialAt(hitInfo.Position);
            else
                renderable.Material = material;


            decalHandler.AddDecal(ref renderable);
        }

        public void RequestVoxelCutoutSphere(Vector3D center, float radius, bool createDebris, bool damage)
        {
            BeforeContentChanged = true;
            MyMultiplayer.RaiseEvent(RootVoxel, x => x.VoxelCutoutSphere_Implementation, center, radius, createDebris, damage);
        }

        [Event, Reliable, Broadcast, RefreshReplicable]
        private void VoxelCutoutSphere_Implementation(Vector3D center, float radius, bool createDebris, bool damage = false)
        {
            MyExplosion.CutOutVoxelMap(radius, center, this, createDebris && MySession.Static.Ready, damage);
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
        private static void VoxelOperationCapsule_Implementation(long entityId, MyCapsuleShapeParams capsuleParams, OperationType Type)
        {
            m_capsuleShape.Transformation = capsuleParams.Transformation;
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
                    MyMultiplayer.RaiseEvent(voxel.RootVoxel, x => x.PerformVoxelOperationCapsule_Implementation, capsuleParams, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_capsuleShape, capsuleParams.Material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }
            }
        }

        [Event, Reliable, Broadcast, RefreshReplicable]
        private void PerformVoxelOperationCapsule_Implementation(MyCapsuleShapeParams capsuleParams, OperationType Type)
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
        private static void VoxelOperationSphere_Implementation(long entityId, Vector3D center, float radius, byte material, OperationType Type)
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
                    MyMultiplayer.RaiseEvent(voxel.RootVoxel, x => x.PerformVoxelOperationSphere_Implementation, center, radius, material, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_sphereShape, material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }
            }
        }

        [Event, Reliable, Broadcast, RefreshReplicable]
        private void PerformVoxelOperationSphere_Implementation(Vector3D center, float radius, byte material, OperationType Type)
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
        private static void VoxelOperationBox_Implementation(long entityId, BoundingBoxD box, MatrixD Transformation, byte material, OperationType Type)
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
                    MyMultiplayer.RaiseEvent(voxel.RootVoxel, x => x.PerformVoxelOperationBox_Implementation, box, Transformation, material, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_boxShape, material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }

            }
        }

        [Event, Reliable, Broadcast]
        private void PerformVoxelOperationBox_Implementation(BoundingBoxD box, MatrixD Transformation, byte material, OperationType Type)
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
        private static void VoxelOperationRamp_Implementation(long entityId, MyRampShapeParams shapeParams, OperationType Type)
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
                    MyMultiplayer.RaiseEvent(voxel.RootVoxel, x => x.PerformVoxelOperationRamp_Implementation, shapeParams, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_rampShape, shapeParams.Material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }
            }
        }

        [Event, Reliable, Broadcast]
        private void PerformVoxelOperationRamp_Implementation(MyRampShapeParams shapeParams, OperationType Type)
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
            MyMultiplayer.RaiseStaticEvent(s => VoxelOperationElipsoid_Implementation, EntityId, radius, Transformation, material, Type);
        }

        [Event, Reliable, Server, RefreshReplicable]
        private static void VoxelOperationElipsoid_Implementation(long entityId, Vector3 radius, MatrixD Transformation, byte material, OperationType Type)
        {
            m_ellipsoidShape.Transformation = Transformation;
            m_ellipsoidShape.Radius = radius;
            if (CanPlaceInArea(Type, m_ellipsoidShape))
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(entityId, out entity);
                MyVoxelBase voxel = entity as MyVoxelBase;
                if (voxel != null)
                {
                    voxel.BeforeContentChanged = true;
                    MyMultiplayer.RaiseEvent(voxel.RootVoxel, x => x.PerformVoxelOperationElipsoid_Implementation, radius, Transformation, material, Type);
                    var amountChanged = voxel.UpdateVoxelShape(Type, m_ellipsoidShape, material);
                    if (Type == OperationType.Cut || Type == OperationType.Fill)
                    {
                        MySession.Static.VoxelHandVolumeChanged += amountChanged;
                    }
                }

            }
        }

        [Event, Reliable, Broadcast]
        private void PerformVoxelOperationElipsoid_Implementation(Vector3 radius, MatrixD Transformation, byte material, OperationType Type)
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
                            MyHud.Notifications.Add(MyNotificationSingletons.CopyPasteAsteoridObstructed);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static bool IsForbiddenEntity(MyEntity entity)
        {
            var cubeGrid = entity as MyCubeGrid;
            return (entity is MyCharacter ||
                        (cubeGrid != null && cubeGrid.IsStatic == false && !cubeGrid.IsPreview) ||
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
            MyMultiplayer.RaiseEvent(RootVoxel, x => x.CreateVoxelMeteorCrater_Implementation, center, radius, normal, material.Index);
        }

        [Event, Reliable, Broadcast]
        private void CreateVoxelMeteorCrater_Implementation(Vector3D center, float radius, Vector3 normal, byte material)
        {
            BeforeContentChanged = true;
            MyVoxelGenerator.MakeCrater(this, new BoundingSphere(center, radius), normal, MyDefinitionManager.Static.GetVoxelMaterialDefinition(material));
        }

        public void GetFilledStorageBounds(out Vector3I min, out Vector3I max)
        {
            min = Vector3I.MaxValue;
            max = Vector3I.MinValue;

            Vector3I sz = Size;

            Vector3I SMax = Size - 1;

            MyStorageData data = new MyStorageData();
            data.Resize(Size);

            Storage.ReadRange(data, MyStorageDataTypeFlags.Content, 0, Vector3I.Zero, SMax);

            for (int z = 0; z < sz.Z; ++z)
                for (int y = 0; y < sz.Y; ++y)
                    for (int x = 0; x < sz.X; ++x)
                    {
                        if (data.Content(x, y, z) > MyVoxelConstants.VOXEL_ISO_LEVEL)
                        {
                            Vector3I l = Vector3I.Max(new Vector3I(x - 1, y - 1, z - 1), Vector3I.Zero);
                            min = Vector3I.Min(l, min);

                            Vector3I h = Vector3I.Min(new Vector3I(x + 1, y + 1, z + 1), SMax);
                            max = Vector3I.Max(h, max);
                        }
                    }
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

        /**
         * Intersect the storage
         * 
         * @param box WorldSpace bounding box to intersect with the storage.
         */
        public ContainmentType IntersectStorage(ref BoundingBox box, bool lazy = true)
        {
            box.Transform(PositionComp.WorldMatrixInvScaled);

            box.Translate(SizeInMetresHalf + StorageMin);

            return Storage.Intersect(ref box, lazy);
        }

        /// <summary>
        /// Use only for cut request
        /// </summary>
        public void SendVoxelCloseRequest()
        {
            MyMultiplayer.RaiseStaticEvent(s => OnVoxelClosedRequest, EntityId);
        }

        [Event, Reliable, Server]
        static void OnVoxelClosedRequest(long entityId)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);
            if (entity == null)
                return;

            // Test right to closing entity (e.g. is creative mode?)
            if (!entity.MarkedForClose)
                entity.Close(); // close only on server, server uses replication to propagate it to clients
        }
    }
}
