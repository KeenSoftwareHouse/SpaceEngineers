using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public class MyVoxelBase : MyEntity, IMyVoxelDrawable,IMyVoxelBase
    {     
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

        public new MySyncVoxel SyncObject
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
        virtual public float GetVoxelContentInBoundingBox_Obsolete(BoundingBoxD worldAabb, out float cellCount)
        {
            cellCount = 0;
            return 0.0f;
        }

        public virtual bool IsAnyAabbCornerInside(BoundingBoxD worldAabb)
        {
            return false;
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
    }
}
