using System;
using System.Diagnostics;
using VRage.Render11.Common;
using VRage.Render11.Resources;

namespace VRageRender
{
    class MyFoliageStream : IDisposable
    {
        internal IVertexBuffer m_stream;
        int m_allocationSize;
        internal bool Append;

        internal void Reserve(int x)
        {
            m_allocationSize += x;
        }

        internal void AllocateStreamOutBuffer(int vertexStride)
        {
            Dispose();

            // padding to some power of 2
            m_allocationSize = ((m_allocationSize + 511) / 512) * 512;
            const int maxAlloc = 5 * 1024 * 1024;
            m_allocationSize = Math.Min(maxAlloc, m_allocationSize);

            Debug.Assert(m_stream == null);
            m_stream = MyManagers.Buffers.CreateVertexBuffer("MyFoliageStream", m_allocationSize, vertexStride, isStreamOutput: true);
        }

        public void Dispose()
        {
            if (m_stream != null)
            {
                MyManagers.Buffers.Dispose(m_stream);
                m_stream = null;
            }
        }
    }
}
