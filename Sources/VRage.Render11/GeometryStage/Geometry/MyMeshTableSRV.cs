using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageRender.Vertex;

namespace VRageRender
{
    struct MyDraw
    {
        internal int Indices;
        internal int StartI;
        internal int BaseV;
    }

    struct MyMeshTableSRV_Entry
    {
        internal List<int> Pages;
    }

    struct MyMeshTableEntry
    {
        internal MeshId Model;
        internal int Lod;
        internal int Part;
    }


    class MyMeshTableSRV
    {
        #region Static
        internal static VertexLayoutId OneAndOnlySupportedVertexLayout;

        internal static void Init()
        {
            OneAndOnlySupportedVertexLayout = MyVertexLayouts.GetLayout(
                new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED),
                new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1),
                new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1),
                new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1)
                );
        }
        #endregion

        internal bool IsMergable(MeshId model)
        {
            var mesh = MyMeshes.GetLodMesh(model, 0);

            return mesh.Info.Data.VertexLayout == OneAndOnlySupportedVertexLayout && mesh.Info.IndicesNum > 0 && model.Info.RuntimeGenerated == false && model.Info.Dynamic == false;
        }

        //List<MyVertexFormatPositionHalf4> m_vertexPositionList = new List<MyVertexFormatPositionHalf4>();
        //List<MyVertexFormatTexcoordNormalTangent> m_vertexList = new List<MyVertexFormatTexcoordNormalTangent>();
        //List<uint> m_indicesList = new List<uint>();

        int m_vertices;
        int m_indices;
        byte[] m_vertexStream0 = new byte[2048]; // MyVertexFormatPositionHalf4
        byte[] m_vertexStream1 = new byte[2048]; // MyVertexFormatTexcoordNormalTangent
        byte[] m_indexStream = new byte[2048]; // uint

        unsafe static readonly int Stride0 = sizeof(MyVertexFormatPositionH4);
        unsafe static readonly int Stride1 = sizeof(MyVertexFormatTexcoordNormalTangent);
        static readonly int IndexStride = sizeof(uint);

        int m_indexPageSize = 0;
        int m_pagesUsed = 0;

        Dictionary<MyMeshTableEntry, MyMeshTableSRV_Entry> m_table = new Dictionary<MyMeshTableEntry, MyMeshTableSRV_Entry>();

        internal int PageSize { get { return m_indexPageSize; } }

        internal StructuredBufferId m_VB_positions = StructuredBufferId.NULL;
        internal StructuredBufferId m_VB_rest = StructuredBufferId.NULL;
        internal StructuredBufferId m_IB = StructuredBufferId.NULL;

        bool m_dirty;

        internal MyMeshTableSRV(int pageSize = 36)
        {
            // non multiple-of-3 page size?
            Debug.Assert((pageSize % 3) == 0);

            m_indexPageSize = pageSize;

            m_dirty = false;
        }

        internal bool ContainsKey(MyMeshTableEntry key)
        {
            return m_table.ContainsKey(key);
        }

        internal static MyMeshTableEntry MakeKey(MeshId model)
        {
            return new MyMeshTableEntry { Model = model, Lod = 0, Part = 0 };
        }

        internal void OnSessionEnd()
        {
            Release();
            m_vertices = 0;
            m_indices = 0;
            m_vertexStream0 = new byte[2048]; // MyVertexFormatPositionHalf4
            m_vertexStream1 = new byte[2048]; // MyVertexFormatTexcoordNormalTangent
            m_indexStream = new byte[2048]; // uint
            m_pagesUsed = 0;
            m_table.Clear();
        }

        internal List<int> Pages(MyMeshTableEntry key)
        {
            return m_table.Get(key).Pages;
        }

        internal unsafe void AddMesh(MeshId model)
        {
            Debug.Assert(IsMergable(model));

            var key = new MyMeshTableEntry { Model = model, Lod = 0, Part = 0 };
            if (!ContainsKey(key))
            {
                var vertexOffset = m_vertices;
                var indexOffset = m_indices;

                var mesh = MyMeshes.GetLodMesh(model, 0);
                Debug.Assert(mesh.Info.Data.IndicesFmt == SharpDX.DXGI.Format.R16_UInt);

                var meshInfo = mesh.Info;
                var data = meshInfo.Data;

                int verticesCapacity = vertexOffset + meshInfo.VerticesNum;
                int indicesCapacity = indexOffset + ((meshInfo.IndicesNum + m_indexPageSize - 1) / m_indexPageSize) * m_indexPageSize;

                m_vertices = verticesCapacity;
                m_indices = indicesCapacity;

                MyArrayHelpers.Reserve(ref m_vertexStream0, verticesCapacity * Stride0, 1024 * 1024);
                MyArrayHelpers.Reserve(ref m_vertexStream1, verticesCapacity * Stride1, 1024 * 1024);
                MyArrayHelpers.Reserve(ref m_indexStream, indicesCapacity * IndexStride, 1024 * 1024);

                var list = new List<int>();

                fixed(byte* src = data.VertexStream0, dst_ = m_vertexStream0)
                {
                    byte* dst = dst_ + data.Stride0 * vertexOffset;
                    SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), data.Stride0 * meshInfo.VerticesNum);
                }
                fixed (byte* src = data.VertexStream1, dst_ = m_vertexStream1)
                {
                    byte* dst = dst_ + data.Stride1 * vertexOffset;
                    SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), data.Stride1 * meshInfo.VerticesNum);
                }

                fixed (void* dst = m_indexStream)
                {
                    uint* stream = (uint*) dst;
                    stream += indexOffset;
                    fixed(void* src = data.Indices)
                    {
                        ushort* indices = (ushort*)src;
                        for (int k = 0; k < meshInfo.IndicesNum; k += m_indexPageSize)
                        {
                            int iEnd = Math.Min(k + m_indexPageSize, meshInfo.IndicesNum);

                            for (int i = k; i < iEnd; i++)
                            {
                                stream[i] = (uint) (indices[i] + vertexOffset);
                            }

                            list.Add(m_pagesUsed++);
                        }

                        if ((meshInfo.IndicesNum % m_indexPageSize) != 0)
                        {
                            var pageIndex = meshInfo.IndicesNum / m_indexPageSize;
                            var pageOffset = meshInfo.IndicesNum % m_indexPageSize;
                            uint lastIndex = stream[pageIndex * m_indexPageSize + pageOffset - 1];
                            for (int i = pageOffset; i < m_indexPageSize; i++)
                            {
                                stream[pageIndex * m_indexPageSize + i] = lastIndex;
                            }
                        }
                    }
                }

                m_table.Add(key, new MyMeshTableSRV_Entry { Pages = list });
                m_dirty = true;
            }
        }

        internal unsafe void MoveToGPU()
        {
            if(m_dirty)
            {
                Release();

                fixed(void* ptr = m_vertexStream0)
                {
                    m_VB_positions = MyHwBuffers.CreateStructuredBuffer(m_vertices, Stride0, false, new IntPtr(ptr));
                }
                fixed (void* ptr = m_vertexStream1)
                {
                    m_VB_rest = MyHwBuffers.CreateStructuredBuffer(m_vertices, Stride1, false, new IntPtr(ptr));
                }
                fixed (void* ptr = m_indexStream)
                {
                    m_IB = MyHwBuffers.CreateStructuredBuffer(m_indices, IndexStride, false, new IntPtr(ptr));
                }

                m_dirty = false;
            }
        }

        internal void OnDeviceReset()
        {
            if (m_vertices > 0)
            {
                m_dirty = true;
            }
        }

        internal void Release()
        {
            if (m_VB_positions != StructuredBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_VB_positions);
                MyHwBuffers.Destroy(m_VB_rest);
                MyHwBuffers.Destroy(m_IB);
            }

            m_VB_positions = StructuredBufferId.NULL;
            m_VB_rest = StructuredBufferId.NULL;
            m_IB = StructuredBufferId.NULL;
        }
    }
}
