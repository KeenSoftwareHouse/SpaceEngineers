using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharpDX.Direct3D11;
using VRageRender.Vertex;

namespace VRageRender.Resources
{
    internal struct MyMeshPart
    {
        internal readonly int indexCount;
        internal readonly int startIndex;
        internal readonly int baseVertex;
        internal readonly int materialID;

        internal MyMeshPart(int count, int start, int baseVertex, int materialID)
        {
            indexCount = count;
            startIndex = start;
            this.baseVertex = baseVertex;
            this.materialID = materialID;
        }
    }

    class MyRenderModel
    {
        // draw calls
        internal MyMeshPart[] m_parts;
        internal MyIndexBuffer m_indexBuffer;
        internal bool m_hasBones;

        // streams
        internal MyVertexBuffer m_vertexBufferStream0;
        internal MyVertexBuffer m_vertexBufferStream1;

        internal void Dispose()
        {
            if (m_vertexBufferStream0 != null)
                m_vertexBufferStream0.Dispose();
            m_vertexBufferStream0 = null;

            if (m_vertexBufferStream1 != null)
                m_vertexBufferStream1.Dispose();
            m_vertexBufferStream1 = null;
 
            if(m_indexBuffer != null)
                m_indexBuffer.Dispose();
            m_indexBuffer = null;
        }
    }
}
