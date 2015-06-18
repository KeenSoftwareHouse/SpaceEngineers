using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Entities
{
    class MyVoxelPhysics: MyVoxelBase 
    {
        MyPlanet m_parent;

        internal new MyVoxelPhysicsBody Physics
        {
            get { return base.Physics as MyVoxelPhysicsBody; }
            set { base.Physics = value; }
        }

        public MyVoxelPhysics()
        {
            AddDebugRenderComponent(new MyDebugRenderComponentVoxelMap(this));
        }

        public void Init(IMyStorage storage, Vector3D positionMinCorner, Vector3I storageMin, Vector3I storageMax,MyPlanet parent)
        {
            m_parent = parent;

            base.Init(null);

            m_storageMax = storageMax;
            m_storageMin = storageMin;

            m_storage = storage;
            InitVoxelMap(positionMinCorner, Size, false);
        }

        protected override void InitVoxelMap(Vector3D positionMinCorner, Vector3I size, bool useOffset = true)
        {
            base.InitVoxelMap(positionMinCorner, size, useOffset);
            Physics = new MyVoxelPhysicsBody(this,1.1f);
            Physics.Enabled = true;
        }

        public void OnStorageChanged(Vector3I minChanged, Vector3I maxChanged, MyStorageDataTypeFlags dataChanged)
        {
            ProfilerShort.Begin("MyVoxelMap::storage_RangeChanged");

            minChanged = Vector3I.Clamp(minChanged, m_storageMin, m_storageMax);
            maxChanged = Vector3I.Clamp(maxChanged, m_storageMin, m_storageMax);
            Debug.Assert(minChanged.IsInsideInclusive(ref m_storageMin, ref m_storageMax) &&
                maxChanged.IsInsideInclusive(ref m_storageMin, ref m_storageMax));

            // Physics doesn't care about materials, just shape of things.
            if ((dataChanged & MyStorageDataTypeFlags.Content) != 0 &&
                Physics != null)
            {
                Physics.InvalidateRange(minChanged, maxChanged);
            }

            ProfilerShort.End();
        }

        protected override void BeforeDelete()
        {
            base.BeforeDelete();
            m_storage = null;
            MySession.Static.VoxelMaps.RemoveVoxelMap(this);
        }

        override public MySyncVoxel GetSyncObject
        {
            get { return (MySyncVoxel)m_parent.SyncObject; }
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
    }
}
