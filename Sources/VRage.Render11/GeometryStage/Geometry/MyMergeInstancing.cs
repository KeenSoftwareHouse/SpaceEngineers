using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Import;
using VRage.Utils;

using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector4 = VRageMath.Vector4;
using Matrix = VRageMath.Matrix;
using BoundingBox = VRageMath.BoundingBox;
using SharpDX.Direct3D;

namespace VRageRender
{
    struct MyInstancedMeshPages
    {
        internal List<int> Pages;
    }

    struct MyInstancingTableEntry
    {
        internal int InnerMeshId;
        internal int InstanceId;
    }

    struct MyPerInstanceData
    {
        internal Vector4 Row0;
        internal Vector4 Row1;
        internal Vector4 Row2;

        internal static MyPerInstanceData FromWorldMatrix(ref MatrixD matrix)
        {
            var mat = (Matrix)matrix;

            return new MyPerInstanceData
            {
                Row0 = new Vector4(mat.M11, mat.M21, mat.M31, mat.M41),
                Row1 = new Vector4(mat.M12, mat.M22, mat.M32, mat.M42),
                Row2 = new Vector4(mat.M13, mat.M23, mat.M33, mat.M43)
            };
        }
    }

    struct MyInstanceInfo
    {
        internal int InstanceIndex;
        internal List<MyPackedPoolHandle> PageHandles;
    }

    class MyMergeInstancing
    {
        internal MyMeshTableSRV m_meshTable;

        internal StructuredBufferId m_indirectionBuffer = StructuredBufferId.NULL;
        internal StructuredBufferId m_instanceBuffer = StructuredBufferId.NULL;
        internal ShaderResourceView[] m_SRVs = new ShaderResourceView[2];

        Dictionary<uint, MyInstanceInfo> m_entities = new Dictionary<uint, MyInstanceInfo>();

        bool m_instancesDataDirty;
        bool m_tablesDirty;

        MyPackedPool<MyInstancingTableEntry> m_instancingTable = new MyPackedPool<MyInstancingTableEntry>(64);
        MyFreelist<MyPerInstanceData> m_perInstance = new MyFreelist<MyPerInstanceData>(16);

        internal int VerticesNum { get { return m_meshTable.PageSize * m_instancingTable.Size; } }

        internal static void Init()
        {

        }

        internal MyMergeInstancing(MyMeshTableSRV meshTable)
        {
            m_meshTable = meshTable;

            m_instancesDataDirty = false;
            m_tablesDirty = false;
        }

        internal bool IsMergable(MeshId model)
        {
            // check if one and only spoorted vertex format
            // check if same material as the rest
            // check if has one part(!) 
            // check if has one lod - for now

            return
                model.Info.LodsNum == 1 && MyMeshes.GetLodMesh(model, 0).Info.PartsNum == 1 && m_meshTable.IsMergable(model);
        }

        internal void AddEntity(uint ID, MeshId model)
        {
            var instanceIndex = m_perInstance.Allocate();

            m_entities[ID] = new MyInstanceInfo { InstanceIndex = instanceIndex, PageHandles = new List<MyPackedPoolHandle>() };

            foreach (var id in m_meshTable.Pages(MyMeshTableSRV.MakeKey(model)))
            {
                var pageHandle = m_instancingTable.Allocate();

                m_instancingTable.Data[m_instancingTable.AsIndex(pageHandle)] = new MyInstancingTableEntry { InstanceId = instanceIndex, InnerMeshId = id };
                m_entities[ID].PageHandles.Add(pageHandle);
            }

            m_perInstance.Data[instanceIndex] = MyPerInstanceData.FromWorldMatrix(ref MatrixD.Zero);

            m_tablesDirty = true;
        }

        internal void UpdateEntity(uint ID, ref MatrixD matrix)
        {
            m_perInstance.Data[m_entities[ID].InstanceIndex] = MyPerInstanceData.FromWorldMatrix(ref matrix);

            m_instancesDataDirty = true;
        }

        internal void RemoveEntity(uint ID)
        {
            m_perInstance.Free(m_entities[ID].InstanceIndex);
            foreach (var page in m_entities[ID].PageHandles)
            {
                m_instancingTable.Free(page);
            }

            m_tablesDirty = true;
        }

        internal void OnDeviceReset()
        {
            if (m_indirectionBuffer != StructuredBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_indirectionBuffer);
                MyHwBuffers.Destroy(m_instanceBuffer);
            }

            m_indirectionBuffer = StructuredBufferId.NULL;
            m_instanceBuffer = StructuredBufferId.NULL;
            Array.Clear(m_SRVs, 0, m_SRVs.Length);

            m_tablesDirty = true;
            m_instancesDataDirty = true;
        }

        internal unsafe void MoveToGPU()
        {
            var context = MyImmediateRC.RC.Context;

            if (m_tablesDirty)
            {
                var array = m_instancingTable.Data;

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);

                    if (m_indirectionBuffer != StructuredBufferId.NULL && m_indirectionBuffer.Capacity < array.Length)
                    {
                        MyHwBuffers.Destroy(m_indirectionBuffer);
                        m_indirectionBuffer = StructuredBufferId.NULL;
                        m_SRVs[0] = null;
                    }
                    if (m_indirectionBuffer == StructuredBufferId.NULL)
                    {
                        m_indirectionBuffer = MyHwBuffers.CreateStructuredBuffer(array.Length, sizeof(MyInstancingTableEntry), false, intPtr);
                        m_SRVs[0] = m_indirectionBuffer.Srv;
                    }
                    else
                    {
                        context.UpdateSubresource(new DataBox(intPtr, array.Length * sizeof(MyInstancingTableEntry), 0), m_indirectionBuffer.Buffer);
                    }
                }

                m_tablesDirty = false;
            }

            if (m_instancesDataDirty)
            {
                var array = m_perInstance.Data;

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);

                    if (m_instanceBuffer != StructuredBufferId.NULL && m_instanceBuffer.Capacity < array.Length)
                    {
                        MyHwBuffers.Destroy(m_instanceBuffer);
                        m_instanceBuffer = StructuredBufferId.NULL;
                        m_SRVs[1] = null;
                    }
                    if (m_instanceBuffer == StructuredBufferId.NULL)
                    {
                        m_instanceBuffer = MyHwBuffers.CreateStructuredBuffer(array.Length, sizeof(MyPerInstanceData), true, intPtr);
                        m_SRVs[1] = m_instanceBuffer.Srv;
                    }
                    else
                    {
                        var mapping = MyMapping.MapDiscard(context, m_instanceBuffer.Buffer);
                        mapping.stream.Write(intPtr, 0, array.Length * sizeof(MyPerInstanceData));
                        mapping.Unmap();
                    }
                }

                m_instancesDataDirty = false;
            }
        }
    }
}
