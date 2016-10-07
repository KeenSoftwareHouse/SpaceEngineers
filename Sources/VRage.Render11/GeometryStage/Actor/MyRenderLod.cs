using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    [PooledObject(poolPreallocationSize: 8)]
#if XB1
    class MyRenderLod : IMyPooledObjectCleaner
#else // !XB1
    class MyRenderLod
#endif // !XB1
    {
        internal MyRenderableProxy[] RenderableProxies;
        internal UInt64[] SortingKeys;

        internal VertexLayoutId VertexLayout1;
        internal MyShaderUnifiedFlags VertexShaderFlags;

        internal float Distance;

        internal void AllocateProxies(int allocationSize)
        {
            if (RenderableProxies == null || allocationSize != RenderableProxies.Length)
            {
                DeallocateProxies();

                RenderableProxies = new MyRenderableProxy[allocationSize];
                SortingKeys = new UInt64[allocationSize];
            }
            else if (RenderableProxies != null)
            {
                for (int proxyIndex = 0; proxyIndex < RenderableProxies.Length; ++proxyIndex)
                {
                    MyObjectPoolManager.Deallocate(RenderableProxies[proxyIndex]);
                }
            }

            for (int proxyIndex = 0; proxyIndex < allocationSize; ++proxyIndex)
            {
                RenderableProxies[proxyIndex] = MyObjectPoolManager.Allocate<MyRenderableProxy>();
            }
        }

        internal void DeallocateProxies()
        {
            if (RenderableProxies != null)
            {
                foreach (var renderableProxy in RenderableProxies)
                    MyObjectPoolManager.Deallocate(renderableProxy);
                RenderableProxies = null;
            }
        }

#if XB1
        public void ObjectCleaner()
        {
            Clear();
        }
#else // !XB1
        [PooledObjectCleaner]
        public static void Clear(MyRenderLod renderLod)
        {
            renderLod.Clear();
        }
#endif // !XB1

        internal void Clear()
        {
            DeallocateProxies();
            SortingKeys = null;
            VertexLayout1 = VertexLayoutId.NULL;
            VertexShaderFlags = MyShaderUnifiedFlags.NONE;
            Distance = float.MinValue;
        }
    }
}
