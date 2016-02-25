using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    class MyFoliageStream : IDisposable
    {
        internal VertexBufferId m_stream = VertexBufferId.NULL;
        int m_allocationSize;
        internal bool Append;

        internal void Reserve(int x)
        {
            m_allocationSize += x;
        }

        internal unsafe void AllocateStreamOutBuffer(int vertexStride)
        {
            Dispose();

            // padding to some power of 2
            m_allocationSize = ((m_allocationSize + 511) / 512) * 512;
            const int maxAlloc = 5 * 1024 * 1024;
            m_allocationSize = Math.Min(maxAlloc, m_allocationSize);

            Debug.Assert(m_stream == VertexBufferId.NULL);
            m_stream = MyHwBuffers.CreateVertexBuffer(m_allocationSize, vertexStride, BindFlags.VertexBuffer | BindFlags.StreamOutput, ResourceUsage.Default);
        }

        public void Dispose()
        {
            if (m_stream != VertexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_stream);
                m_stream = VertexBufferId.NULL;
            }
        }
    }
}
