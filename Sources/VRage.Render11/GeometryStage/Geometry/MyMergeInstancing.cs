using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage.Render11.Common;
using VRage.Render11.Resources;
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
        internal MyMeshTableSrv m_meshTable;

        internal ISrvBuffer m_indirectionBuffer;
        internal ISrvBuffer m_instanceBuffer;
        internal ISrvBindable[] m_srvs = new ISrvBindable[2];

        Dictionary<uint, MyInstanceInfo> m_entities = new Dictionary<uint, MyInstanceInfo>();

        bool m_instancesDataDirty;
        bool m_tableDirty;

        MyPackedPool<MyInstancingTableEntry> m_instancingTable = new MyPackedPool<MyInstancingTableEntry>(64);
        MyFreelist<MyPerInstanceData> m_perInstance = new MyFreelist<MyPerInstanceData>(16);
        MyFreelist<MyInstanceEntityInfo> m_entityInfos = new MyFreelist<MyInstanceEntityInfo>(16);

        internal int VerticesNum { get { return m_meshTable.PageSize * m_instancingTable.Size; } }

        internal MyMergeInstancing(MyMeshTableSrv meshTable)
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
            var key = MyMeshTableSrv.MakeKey(model);
            foreach (var id in m_meshTable.Pages(key))
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
            if (m_indirectionBuffer != null)
                MyManagers.Buffers.Dispose(m_indirectionBuffer); m_indirectionBuffer = null;
            if (m_instanceBuffer != null)
                MyManagers.Buffers.Dispose(m_instanceBuffer); m_instanceBuffer = null;

            Array.Clear(m_srvs, 0, m_srvs.Length);

            m_tableDirty = true;
            m_instancesDataDirty = true;
        }

        internal unsafe void MoveToGPU()
        {
            if (m_tableDirty)
            {
                var array = m_instancingTable.Data;

                fixed (void* ptr = array)
                {
                    // m_indirectionBuffer used to be resized each time here; it is now only resized when it needs to grow
                    // If this causes issues, change it back to resize to exactly array.Length each time
                    CreateResizeOrFill(
                        "MyMergeInstancing/Tables", ref m_indirectionBuffer, array.Length,
                        array, new IntPtr(ptr));
                    m_srvs[0] = m_indirectionBuffer;
                }

                m_tableDirty = false;
            }

            if (m_instancesDataDirty)
            {
                var array = m_perInstance.Data;

                fixed (void* ptr = array)
                {
                    CreateResizeOrFill(
                        "MyMergeInstancing instances", ref m_instanceBuffer, array.Length,
                        array, new IntPtr(ptr));
                    m_srvs[1] = m_instanceBuffer;
                }

                m_instancesDataDirty = false;
            }
        }

        void CreateResizeOrFill<TDataElement>(string name, ref ISrvBuffer buffer, int size, TDataElement[] data, IntPtr rawData)
            where TDataElement : struct
        {
            if (buffer != null && buffer.ElementCount < size)
            {
                MyManagers.Buffers.Resize(buffer, size, newData: rawData);
            }
            if (buffer == null)
            {
                // We can't create ptr to a generic array here, we have to get it through param :'(
                buffer = MyManagers.Buffers.CreateSrv(
                    name, size, Marshal.SizeOf<TDataElement>(),
                    rawData, ResourceUsage.Dynamic);
            }
            else
            {
                var mapping = MyMapping.MapDiscard(MyImmediateRC.RC, buffer);
                mapping.WriteAndPosition(data, data.Length * Marshal.SizeOf<TDataElement>());
                mapping.Unmap();
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
