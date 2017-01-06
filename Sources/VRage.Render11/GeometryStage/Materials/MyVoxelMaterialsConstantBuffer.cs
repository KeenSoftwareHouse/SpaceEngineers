using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageRender;

namespace VRage.Render11.GeometryStage.Materials
{
    // This is temporary unoptimized implementation created just to get voxel terrain drawcalls reduction working.
    // It does NOT handle invalidation if already updated  MyVoxelMaterialEntry data changes.
    class MyVoxelMaterialsConstantBuffer
    {
        private const int MAX_ENTRIES = 128;

        public IConstantBuffer Cb {get; private set;}
        readonly MyVoxelMaterialEntry[] m_entries;
        readonly bool[] m_needsUpdate;

        public unsafe MyVoxelMaterialsConstantBuffer()
        {
            Cb = MyManagers.Buffers.CreateConstantBuffer("VoxelMaterialConstants", sizeof(MyVoxelMaterialEntry) * MAX_ENTRIES, usage: ResourceUsage.Dynamic);
            m_entries = new MyVoxelMaterialEntry[MAX_ENTRIES];
            m_needsUpdate = new bool[MAX_ENTRIES];
            for (int i = 0; i < MAX_ENTRIES; i++)
                m_needsUpdate[i] = true;
        }

        public bool NeedsUpdate(int voxelMaterialId)
        {
            return m_needsUpdate[voxelMaterialId];
        }

        public void Invalidate(int voxelMaterialId)
        {
            MyRenderProxy.Assert(voxelMaterialId < MAX_ENTRIES);
            m_needsUpdate[voxelMaterialId] = true;
        }

        public void UpdateEntry(int voxelMaterialId, ref MyVoxelMaterialEntry entry)
        {
            MyRenderProxy.Assert(voxelMaterialId < MAX_ENTRIES);
            MyRenderProxy.Assert(Cb != null);
            MyRenderProxy.Assert(MyRenderProxy.RenderThread.SystemThread == System.Threading.Thread.CurrentThread);

            if (m_needsUpdate[voxelMaterialId])
            {
                m_entries[voxelMaterialId] = entry;
                MyMapping mapping = MyMapping.MapDiscard(Cb);
                for (int i = 0; i < MAX_ENTRIES; i++)
                    mapping.WriteAndPosition(ref m_entries[i]);
                mapping.Unmap();
                m_needsUpdate[voxelMaterialId] = false;
            }
        }
    }
}
