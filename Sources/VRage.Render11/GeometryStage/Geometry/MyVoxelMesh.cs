using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
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

namespace VRageRender
{





    class MyVoxelMesh : MyMesh
    {
        internal const string SINGLE_MATERIAL_TAG = "triplanar_single";
        internal const string MULTI_MATERIAL_TAG = "triplanar_multi";

        //static List<MyVoxelMesh> m_cellsList = new List<MyVoxelMesh>();


        //internal Vector3I m_coord;
        //internal int m_lod;
        //string m_debugName;
        ////internal List<int> MaterialsUsed = new List<int>();

        //List<MyVoxelCellUpdate> m_waitingUpdates = new List<MyVoxelCellUpdate>(4);
        //internal SortedDictionary<Tuple<int, int, int>, MyBufferSegment> m_vbAllocations = new SortedDictionary<Tuple<int, int, int>, MyBufferSegment>();

        //internal MyRenderMeshInfo Mesh { get { return LODs[0].m_meshInfo; } }

        //internal MyVoxelMesh(Vector3I coord, int lod, string debugName)
        //{
        //    m_coord = coord;
        //    m_lod = lod;
        //    m_debugName = debugName + " " + lod + " " + coord;

        //    LODs = new MyRenderLodInfo[1];
        //    LODs[0] = new MyRenderLodInfo();
        //    LODs[0].m_meshInfo = new MyRenderMeshInfo();

            
        //    Mesh.VertexLayout = MyVertexInputLayout.Empty();
        //    Mesh.VertexLayout = Mesh.VertexLayout.Append(MyVertexInputComponentType.VOXEL_POSITION_MAT).Append(MyVertexInputComponentType.NORMAL, 1);

        //    Mesh.Parts[SINGLE_MATERIAL_TAG] = null;
        //    Mesh.Parts[MULTI_MATERIAL_TAG] = null;

        //    m_cellsList.Add(this);
        //}

        //internal static void RemoveAll()
        //{
        //    foreach (var cell in m_cellsList)
        //    {
        //        cell.Dispose();
        //    }
        //    m_cellsList.Clear();
        //}

        //public void Dispose()
        //{
        //    if (Mesh.VB != null)
        //    {
        //        for (int i = 0; i < Mesh.VB.Length; i++)
        //        {
        //            if (Mesh.VB[i] != VertexBufferId.NULL)
        //            {
        //                MyHwBuffers.Destroy(Mesh.VB[i]);
        //            }
        //            Mesh.VB[i] = VertexBufferId.NULL;
        //        }
        //    }



        //    if (Mesh.IB != IndexBufferId.NULL)
        //    {
        //        MyHwBuffers.DestroyIndexBuffer(Mesh.IB);
        //    }
        //    Mesh.IB = IndexBufferId.NULL;
        //}



        //#region Updating

        //const float BUFFER_OVERALLOCATION = 0;

        //internal struct MyBufferSegment
        //{
        //    internal int vertexOffset;
        //    internal int vertexCapacity;
        //    internal int indexOffset;
        //    internal int indexCapacity;

        //    internal int indexCount;
        //    internal int vertexCount;
        //}

        //struct MyVoxelCellUpdate
        //{
        //    internal short[] indices;
        //    internal MyVertexFormatVoxelSingleData[] vertexData;
        //    internal int material0;
        //    internal int material1;
        //    internal int material2;
        //}

        //internal void Update(MyVertexFormatVoxelSingleData[] vertexData, short[] indices, int mat0, int mat1, int mat2)
        //{
        //    var queued = new MyVoxelCellUpdate();

        //    queued.indices = indices;
        //    queued.vertexData = vertexData;
        //    queued.material0 = mat0;
        //    queued.material1 = mat1;
        //    queued.material2 = mat2;

        //    m_waitingUpdates.Add(queued);
        //}

        //// true if buffers changed
        //internal unsafe bool CommitToGPU()
        //{
        //    if (m_waitingUpdates.Count == 0)
        //        return false;

        //    int len = m_waitingUpdates.Count;
        //    bool isInitialization = Mesh.IB == null;

        //    bool recreateBuffers = false;

        //    int vertexCapacity = 0;
        //    int indexCapacity = 0;

        //    var vbAllocations = new SortedDictionary<Tuple<int, int, int>, MyBufferSegment>();

        //    //MaterialsUsed.Clear();

        //    for (int i = 0; i < len; i++)
        //    {
        //        var ilen = m_waitingUpdates[i].indices.Length;
        //        var vlen = m_waitingUpdates[i].vertexData.Length;

        //        var key = Tuple.Create(m_waitingUpdates[i].material0, m_waitingUpdates[i].material1, m_waitingUpdates[i].material2);
        //        //if (key.Item1 != -1)
        //        //    MaterialsUsed.Add(key.Item1);
        //        //if (key.Item2 != -1)
        //        //    MaterialsUsed.Add(key.Item2);
        //        //if (key.Item3 != -1)
        //        //    MaterialsUsed.Add(key.Item3);

        //        MyBufferSegment entry = new MyBufferSegment();
        //        //m_vbAllocations.TryGetValue(key, out entry);

        //        //vertexCapacity += entry.vertexCapacity;
        //        //indexCapacity += entry.indexCapacity;

        //        if (entry.indexCapacity < ilen)
        //        {
        //            entry.indexCapacity = (int)(ilen * (1 + BUFFER_OVERALLOCATION));
        //            recreateBuffers = true;
        //        }
        //        if (entry.vertexCapacity < vlen)
        //        {
        //            entry.vertexCapacity = (int)(vlen * (1 + BUFFER_OVERALLOCATION));
        //            recreateBuffers = true;
        //        }

        //        entry.indexCount = ilen;
        //        entry.vertexCount = vlen;
        //        vbAllocations[key] = entry;
        //    }

        //    //MaterialsUsed.Distinct().ToList();
        //    m_vbAllocations = vbAllocations;
        //    if (recreateBuffers)
        //    {
        //        // update offsets
        //        int voffset = 0;
        //        int ioffset = 0;

        //        // allocation
        //        var keys = m_vbAllocations.Keys.ToList();
        //        foreach (var key in keys)
        //        {
        //            var val = m_vbAllocations[key];
        //            val.indexOffset = ioffset;
        //            val.vertexOffset = voffset;
        //            ioffset += val.indexCapacity;
        //            voffset += val.vertexCapacity;
        //            m_vbAllocations[key] = val;
        //        }

        //        Dispose();

        //        vertexCapacity = voffset;
        //        indexCapacity = ioffset;

        //        if (Mesh.VB == null)
        //        {
        //            Mesh.VB = new VertexBufferId[2];
        //        }

        //        Mesh.VB[0] = MyHwBuffers.CreateVertexBuffer(vertexCapacity, sizeof(MyVertexFormatVoxel), null, m_debugName + " vertex buffer 0");
        //        Mesh.VB[1] = MyHwBuffers.CreateVertexBuffer(vertexCapacity, sizeof(MyVertexFormatNormal), null, m_debugName + " vertex buffer 1");
        //        Mesh.IB = MyHwBuffers.CreateIndexBuffer(indexCapacity, Format.R16_UInt, null, m_debugName + " index buffer");
        //    }

        //    var indices = new ushort[indexCapacity];
        //    var vertices0 = new MyVertexFormatVoxel[vertexCapacity];
        //    var vertices1 = new MyVertexFormatNormal[vertexCapacity];

        //    int singleMat = 0;
        //    int multiMat = 0;

        //    for (int i = 0; i < len; i++)
        //    {
        //        var key = Tuple.Create(m_waitingUpdates[i].material0, m_waitingUpdates[i].material1, m_waitingUpdates[i].material2);
        //        var entry = m_vbAllocations[key];

        //        if (key.Item2 == -1 && key.Item3 == -1)
        //            singleMat++;
        //        else
        //            multiMat++;

        //        var offset = entry.indexOffset;
        //        var batchIndices = m_waitingUpdates[i].indices;
        //        for (int j = 0; j < batchIndices.Length; j++)
        //        {
        //            indices[offset + j] = (ushort)(batchIndices[j] + entry.vertexOffset);
        //        }

        //        offset = entry.vertexOffset;
        //        var batchVertices = m_waitingUpdates[i].vertexData;
        //        for (int j = 0; j < batchVertices.Length; j++)
        //        {
        //            vertices0[offset + j] = new MyVertexFormatVoxel();
        //            vertices0[offset + j].Position = batchVertices[j].Position;
        //            var mat = batchVertices[j].MaterialAlphaIndex;
        //            switch (mat)
        //            {
        //                case 0:
        //                    vertices0[offset + j].Weight0 = 1;
        //                    break;
        //                case 1:
        //                    vertices0[offset + j].Weight1 = 1;
        //                    break;
        //                case 2:
        //                    vertices0[offset + j].Weight2 = 1;
        //                    break;
        //            }

        //            vertices1[offset + j] = new MyVertexFormatNormal(batchVertices[j].PackedNormal);
        //        }
        //    }

        //    !!! Use either MyMapping or MyHwBuffers.ResizeAndUpdateVertexBuffer instead of direct access to UpdateSubresource (add ImmediateContext as param)
        //    DataBox srcData = new DataBox();
        //    fixed (ushort* I = indices)
        //    {
        //        srcData.DataPointer = new IntPtr(I);
        //        MyRender11.ImmediateContext.UpdateSubresource(srcData, Mesh.IB.Buffer);
        //    }
        //    fixed (MyVertexFormatVoxel* V = vertices0)
        //    {
        //        srcData.DataPointer = new IntPtr(V);
        //        MyRender11.ImmediateContext.UpdateSubresource(srcData, Mesh.VB[0].Buffer);
        //    }
        //    fixed (MyVertexFormatNormal* V = vertices1)
        //    {
        //        srcData.DataPointer = new IntPtr(V);
        //        MyRender11.ImmediateContext.UpdateSubresource(srcData, Mesh.VB[1].Buffer);
        //    }


        //    if (Mesh.Parts[SINGLE_MATERIAL_TAG] == null || Mesh.Parts[SINGLE_MATERIAL_TAG].Length != singleMat)
        //        Mesh.Parts[SINGLE_MATERIAL_TAG] = new MyDrawSubmesh[singleMat];
        //    if (Mesh.Parts[MULTI_MATERIAL_TAG] == null || Mesh.Parts[MULTI_MATERIAL_TAG].Length != multiMat)
        //        Mesh.Parts[MULTI_MATERIAL_TAG] = new MyDrawSubmesh[multiMat];

        //    int cs = 0;
        //    int cm = 0;
        //    foreach (var kv in m_vbAllocations)
        //    {
        //        var submesh = new MyDrawSubmesh(kv.Value.indexCount, kv.Value.indexOffset, 0, MyVoxelMaterials1.GetMaterialProxyId(
        //            new MyVoxelMaterialTriple { I0 = kv.Key.Item1, I1 = kv.Key.Item2, I2 = kv.Key.Item3 }));
        //        //submesh.Material = MyVoxelMaterials.GetBindings(kv.Key.Item1, kv.Key.Item2, kv.Key.Item3);
        //        if (kv.Key.Item2 == -1 && kv.Key.Item3 == -1)
        //        {
        //            Mesh.Parts[SINGLE_MATERIAL_TAG][cs++] = submesh;
        //        }
        //        else
        //        {
        //            Mesh.Parts[MULTI_MATERIAL_TAG][cm++] = submesh;
        //        }
        //    }

        //    m_waitingUpdates.Clear();

        //    m_loadingStatus = MyAssetLoadingEnum.Ready;

        //    return recreateBuffers;
        //}

        //#endregion
    }
}
