using System.Diagnostics;
using VRage.Collections;
using VRage.Generics;

namespace VRageRender
{
    enum MyActorComponentEnum
    {
        Unassigned,
        Renderable,
        Instancing,
        Skinning,
        Foliage,
        GroupLeaf,
        GroupRoot
    }

    class MyActorComponent
    {
        internal MyActor m_owner;
        internal MyActorComponentEnum Type;

        internal virtual void OnRemove(MyActor owner) { }
        internal virtual void OnMatrixChange() { }
        internal virtual void OnAabbChange() { }
        internal virtual void OnVisibilityChange() { }

        internal virtual void Assign(MyActor owner)
        {
            m_owner = owner;
        }

        internal virtual void Construct()
        {
            m_owner = null;
            Type = MyActorComponentEnum.Unassigned;
        }

        internal virtual void Destruct()
        {
            // dispose resources
            // deallocate to pools!
        }

        internal bool IsVisible => m_owner.m_visible;
    }

    static class MyActorComponentExtenstions
    {
        internal static void Deallocate(this MyActorComponent item)
        {
            switch (item.Type)
            {
                case MyActorComponentEnum.Renderable:
                    MyComponentFactory<MyRenderableComponent>.Deallocate(item as MyRenderableComponent);
                    break;
                case MyActorComponentEnum.Instancing:
                    MyComponentFactory<MyInstancingComponent>.Deallocate(item as MyInstancingComponent);
                    break;
                case MyActorComponentEnum.Skinning:
                    MyComponentFactory<MySkinningComponent>.Deallocate(item as MySkinningComponent);
                    break;
                case MyActorComponentEnum.Foliage:
                    MyComponentFactory<MyFoliageComponent>.Deallocate(item as MyFoliageComponent);
                    break;
                case MyActorComponentEnum.GroupLeaf:
                    MyComponentFactory<MyGroupLeafComponent>.Deallocate(item as MyGroupLeafComponent);
                    break;
                case MyActorComponentEnum.GroupRoot:
                    MyComponentFactory<MyGroupRootComponent>.Deallocate(item as MyGroupRootComponent);
                    break;
                case MyActorComponentEnum.Unassigned:
                    Debug.Assert(false, "Can't find component factory");
                    break;
            }
        }
    }

    static class MyComponentFactory<T> where T : MyActorComponent, new()
    {
        static MyObjectsPool<T> m_pool = new MyObjectsPool<T>(50);

        internal static T Create()
        {
            T item;
            m_pool.AllocateOrCreate(out item);
            item.Construct();
            return item;
        }

        internal static void Deallocate(T item)
        {
            item.Destruct();
            m_pool.Deallocate(item);
        }

        internal static HashSetReader<T> GetAll()
        {
            return m_pool.Active;
        }

        internal static void RemoveAll()
        {
            foreach (var item in m_pool.Active)
            {
                item.Destruct();
            }
            m_pool.DeallocateAll();
        }

    }
}
