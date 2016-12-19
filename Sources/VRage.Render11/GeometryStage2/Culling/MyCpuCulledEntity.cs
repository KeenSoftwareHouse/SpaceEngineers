using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Instancing;
using VRageMath;
using VRageRender;

namespace VRage.Render11.GeometryStage2.Culling
{
    [PooledObject]
    class MyCpuCulledEntity
    {
        const int UNITIALISED_AABB_TREE_ID = -1;
        int m_aabbTreeId = UNITIALISED_AABB_TREE_ID;
        public MyInstanceComponent Owner { get; private set; }

        public void Register(BoundingBoxD boundingBox, MyInstanceComponent owner)
        {
            MyRenderProxy.Assert(m_aabbTreeId == UNITIALISED_AABB_TREE_ID, "The cpu culled entity has been initialised before!");
            m_aabbTreeId = MyManagers.HierarchicalCulledEntities.AabbTree.AddProxy(ref boundingBox, this, 0);
            Owner = owner;
        }

        public void Update(ref BoundingBoxD boundingBox)
        {
            MyRenderProxy.Assert(m_aabbTreeId != UNITIALISED_AABB_TREE_ID, "The cpu culled entity is not assigned!");
            MyManagers.HierarchicalCulledEntities.AabbTree.MoveProxy(m_aabbTreeId, ref boundingBox, Vector3D.Zero); 
        }

        public void Unregister()
        {
            MyRenderProxy.Assert(m_aabbTreeId != UNITIALISED_AABB_TREE_ID, "The cpu culled entity is not assigned!");

            MyManagers.HierarchicalCulledEntities.AabbTree.RemoveProxy(m_aabbTreeId);
            m_aabbTreeId = -1;
            Owner = null;
        }

        [PooledObjectCleaner]
        public static void Clear(MyCpuCulledEntity culledEntity) {}
    }

    class MyHierarchicalCulledEntitiesManager: IManager, IManagerUnloadData
    {
        public MyDynamicAABBTreeD AabbTree { get; private set; }

        public MyHierarchicalCulledEntitiesManager()
        {
            AabbTree = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);
        }

        void IManagerUnloadData.OnUnloadData()
        {
            AabbTree.Clear();
        }
    }
}
