using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;


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
        GroupRoot,
        InstanceLod,
    }

    static class MyActorComponentEnumExtensions
    {
        public static Type TypeForEnum(this MyActorComponentEnum self)
        {
            switch (self)
            {
                case MyActorComponentEnum.Renderable:
                    return typeof(MyRenderableComponent);
                case MyActorComponentEnum.Instancing:
                    return typeof(MyInstancingComponent);
                case MyActorComponentEnum.Skinning:
                    return typeof(MySkinningComponent);
                case MyActorComponentEnum.Foliage:
                    return typeof(MyFoliageComponent);
                case MyActorComponentEnum.GroupLeaf:
                    return typeof(MyGroupLeafComponent);
                case MyActorComponentEnum.GroupRoot:
                    return typeof(MyGroupRootComponent);
                case MyActorComponentEnum.InstanceLod:
                    return typeof(MyInstanceLodComponent);
            }
            return null;
        }
    }

    class MyActorComponent
    {
        internal MyActor Owner { get; private set; }
        internal MyActorComponentEnum Type;

        internal virtual void OnRemove(MyActor owner) { }
        internal virtual void OnMatrixChange() { }
        internal virtual void OnAabbChange() { }
        internal virtual void OnVisibilityChange() { }

        internal virtual void Assign(MyActor owner)
        {
            Owner = owner;
        }

        internal virtual void Construct()
        {
            Owner = null;
            Type = MyActorComponentEnum.Unassigned;
        }

        internal virtual void Destruct()
        {
            // dispose resources
            // deallocate to pools!
        }

        internal bool IsVisible { get { return Owner.IsVisible; } }
    }

    static class MyActorComponentExtenstions
    {
        internal static void Deallocate(this MyActorComponent item)
        {
            var voxelRenderable = item as MyVoxelRenderableComponent; // TODO: Rewrite this whole method
            if (voxelRenderable != null)
            {
                MyComponentFactory<MyVoxelRenderableComponent>.Deallocate(voxelRenderable);
                return;
            }

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
                case MyActorComponentEnum.InstanceLod:
                    MyComponentFactory<MyInstanceLodComponent>.Deallocate(item as MyInstanceLodComponent);
                    break;
                case MyActorComponentEnum.Unassigned:
                    Debug.Assert(false, "Can't find component factory");
                    break;
            }
        }
    }

    static class MyComponentFactory<T> where T : MyActorComponent, new()
    {
        static MyObjectsPool<T> m_pool = new MyObjectsPool<T>(32);

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
