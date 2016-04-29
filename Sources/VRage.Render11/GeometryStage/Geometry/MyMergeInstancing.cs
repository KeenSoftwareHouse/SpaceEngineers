using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;
using Matrix = VRageMath.Matrix;
using Vector4 = VRageMath.Vector4;

namespace VRageRender
{
    struct MyInstancingTableEntry
    {
        internal int InnerMeshId;
        internal int InstanceId;
    }

    struct MyInstanceEntityInfo
    {
        internal uint? EntityId;
        internal int PageOffset;
    }

    struct MyPerInstanceData
    {
        internal Vector4 Row0;
        internal Vector4 Row1;
        internal Vector4 Row2;
        internal uint DepthBias;
        internal Vector3 __padding;

        internal static MyPerInstanceData FromWorldMatrix(ref MatrixD matrix, uint depthBias)
        {
            var mat = (Matrix)matrix;

            return new MyPerInstanceData
            {
                Row0 = new Vector4(mat.M11, mat.M21, mat.M31, mat.M41),
                Row1 = new Vector4(mat.M12, mat.M22, mat.M32, mat.M42),
                Row2 = new Vector4(mat.M13, mat.M23, mat.M33, mat.M43),
                DepthBias = depthBias,
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
        internal IShaderResourceBindable[] m_SRVs = new IShaderResourceBindable[2];

        Dictionary<uint, MyInstanceInfo> m_entities = new Dictionary<uint, MyInstanceInfo>();

        bool m_instancesDataDirty;
        bool m_tableDirty;

        MyPackedPool<MyInstancingTableEntry> m_instancingTable = new MyPackedPool<MyInstancingTableEntry>(64);
        MyFreelist<MyPerInstanceData> m_perInstance = new MyFreelist<MyPerInstanceData>(16);
        MyFreelist<MyInstanceEntityInfo> m_entityInfos = new MyFreelist<MyInstanceEntityInfo>(16);

        internal int VerticesNum { get { return m_meshTable.PageSize * m_instancingTable.Size; } }

        internal MyMergeInstancing(MyMeshTableSRV meshTable)
        {
            m_meshTable = meshTable;

            m_instancesDataDirty = false;
            m_tableDirty = false;
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

        internal void AddEntity(MyActor actor, MeshId model)
        {
            uint ID = actor.ID;
            var instanceIndex = m_perInstance.Allocate();
            var entityIndex = m_entityInfos.Allocate();
            Debug.Assert(instanceIndex == entityIndex);

            m_entities[ID] = new MyInstanceInfo { InstanceIndex = instanceIndex, PageHandles = new List<MyPackedPoolHandle>() };

            int pageOffset = -1;
            foreach (var id in m_meshTable.Pages(MyMeshTableSRV.MakeKey(model)))
            {
                if (pageOffset == -1)
                    pageOffset = id;

                var pageHandle = m_instancingTable.Allocate();

                m_instancingTable.Data[m_instancingTable.AsIndex(pageHandle)] = new MyInstancingTableEntry { InstanceId = instanceIndex, InnerMeshId = id };

                m_entities[ID].PageHandles.Add(pageHandle);
            }

            m_perInstance.Data[instanceIndex] = MyPerInstanceData.FromWorldMatrix(ref MatrixD.Zero, 0);
            m_entityInfos.Data[instanceIndex] = new MyInstanceEntityInfo { EntityId = ID, PageOffset = pageOffset };

            m_tableDirty = true;
        }

        internal void UpdateEntity(MyActor actor, ref MatrixD matrix, uint depthBias)
        {
            uint ID = actor.ID;
            m_perInstance.Data[m_entities[ID].InstanceIndex] = MyPerInstanceData.FromWorldMatrix(ref matrix, depthBias);

            m_instancesDataDirty = true;
        }

        internal void RemoveEntity(MyActor actor)
        {
            uint ID = actor.ID;
            MyInstanceInfo info = m_entities[ID];
            m_perInstance.Free(info.InstanceIndex);
            m_entityInfos.Free(info.InstanceIndex);
            foreach (var page in m_entities[ID].PageHandles)
                m_instancingTable.Free(page);

            // CHECK-ME m_entities.Remove(ID) is missing

            m_tableDirty = true;
        }

        public MyInstanceEntityInfo[] GetEntityInfos(out int filledSize)
        {
            filledSize = m_entityInfos.FilledSize;
            return m_entityInfos.Data;
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

            m_tableDirty = true;
            m_instancesDataDirty = true;
        }

        internal unsafe void MoveToGPU()
        {
            var context = MyImmediateRC.RC.DeviceContext;

            if (m_tableDirty)
            {
                var array = m_instancingTable.Data;

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    MyHwBuffers.ResizeAndUpdateStaticStructuredBuffer(ref m_indirectionBuffer, array.Length, sizeof(MyInstancingTableEntry), intPtr, 
                        "MyMergeInstancing/Tables", context);
                    m_SRVs[0] = m_indirectionBuffer;
                }

                m_tableDirty = false;
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
                        m_instanceBuffer = MyHwBuffers.CreateStructuredBuffer(array.Length, sizeof(MyPerInstanceData), true, intPtr, "MyMergeInstancing instances");
                        m_SRVs[1] = m_instanceBuffer;
                    }
                    else
                    {
                        var mapping = MyMapping.MapDiscard(context, m_instanceBuffer.Buffer);
                        mapping.WriteAndPosition(array, 0, array.Length);
                        mapping.Unmap();
                    }
                }

                m_instancesDataDirty = false;
            }
        }

        public bool TableDirty
        {
            get { return m_tableDirty; }
        }

        public int TablePageSize
        {
            get { return m_meshTable.PageSize; }
        }
    }
}
