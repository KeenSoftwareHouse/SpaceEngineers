using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using System.Diagnostics;
using VRage;
using VRage.Profiler;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Entities
{
    // This is assumed to be from a planet in the code
    internal class MyVoxelPhysics : MyVoxelBase
    {
        private MyPlanet m_parent;

        internal new MyVoxelPhysicsBody Physics
        {
            get { return base.Physics as MyVoxelPhysicsBody; }
            set { base.Physics = value; }
        }

        public override MyVoxelBase RootVoxel { get { return m_parent; } }

        public MyVoxelPhysics()
        {
            AddDebugRenderComponent(new MyDebugRenderComponentVoxelMap(this));
        }

        public override void Init(VRage.ObjectBuilders.MyObjectBuilder_EntityBase builder, IMyStorage storage)
        {
        }

        public void Init(IMyStorage storage, Vector3D positionMinCorner, Vector3I storageMin, Vector3I storageMax, MyPlanet parent)
        {
            PositionLeftBottomCorner = positionMinCorner;

            m_storageMax = storageMax;
            m_storageMin = storageMin;

            m_storage = storage;

            SizeInMetres = Size * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            SizeInMetresHalf = SizeInMetres / 2.0f;

            MatrixD worldMatrix = MatrixD.CreateTranslation(positionMinCorner + SizeInMetresHalf);
            Init(storage, worldMatrix, storageMin, storageMax, parent);
        }

        public void Init(IMyStorage storage, MatrixD worldMatrix, Vector3I storageMin, Vector3I storageMax, MyPlanet parent)
        {
            m_parent = parent;

            long hash = storageMin.X;
            hash = (hash * 397L) ^ (long)storageMin.Y;
            hash = (hash * 397L) ^ (long)storageMin.Z;
            hash = (hash * 397L) ^ (long)parent.EntityId;

            EntityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.VOXEL_PHYSICS, hash & 0x00FFFFFFFFFFFFFF);

            base.Init(null);
            InitVoxelMap(worldMatrix, Size, false);
        }

        public MyPlanet Parent
        {
            get { return m_parent; }
        }

        protected override void InitVoxelMap(MatrixD worldMatrix, Vector3I size, bool useOffset = true)
        {
            base.InitVoxelMap(worldMatrix, size, useOffset);
            Physics = new MyVoxelPhysicsBody(this, 1.5f, 7.0f);
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
                RaisePhysicsChanged();
            }

            ProfilerShort.End();
        }

        protected override void BeforeDelete()
        {
            base.BeforeDelete();
            m_storage = null;
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

        public override int GetOrePriority()
        {
            // This ensures that other overlapping voxel grids will have drilling priority.
            return MyVoxelConstants.PRIORITY_PLANET;
        }

        public void PrefetchShapeOnRay(ref LineD ray)
        {
            if (Physics != null)
            {
                Physics.PrefetchShapeOnRay(ref ray);
            }
        }
    }
}