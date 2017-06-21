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
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;


namespace VRageRender
{
    class MyDestructionMesh : MyMesh
    {
      //  static string DEFAULT_MESH_TECHNIQUE = "MESH";

        //internal static Dictionary<string, MyDestructionMesh> ModelsDictionary = new Dictionary<string, MyDestructionMesh>();

        //int m_sortID;

        //internal override int GetSortingID(int lodNum)
        //{
        //    return m_sortID;
        //}

        //internal MyDestructionMesh(string name)
        //{
        //    m_name = name;
        //    ModelsDictionary.Add(name, this);
        //    m_sortID = ModelsDictionary.Count;
        //}

        //#region Loading internals

        //internal unsafe void Fill(
        //    List<int> indices,
        //    List<Vector3> positions,
        //    List<Vector3> normals,
        //    List<Vector3> tangents,
        //    List<Vector2> texcoords,
        //    List<MySectionInfo> sections,
        //    BoundingBox aabb)
        //{
        //    LODs = null;
        //    IsAnimated = false;
        //    // serialized now
        //    m_loadingStatus = MyAssetLoadingEnum.Waiting;

        //    LODs = new MyRenderLodInfo[1];
        //    LODs[0] = new MyRenderLodInfo();
        //    LODs[0].m_meshInfo = new MyRenderMeshInfo();
        //    LODs[0].LodNum = 0;
        //    LODs[0].Distance = 0;

        //    var meshInfo = LODs[0].m_meshInfo;
        //    meshInfo.VertexLayout = MyVertexInputLayout.Empty().Append(MyVertexInputComponentType.POSITION_PACKED).Append(MyVertexInputComponentType.NORMAL, 1)
        //                    .Append(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1)
        //                    .Append(MyVertexInputComponentType.TEXCOORD0, 1);


        //    ushort [] indicesArray = new ushort[indices.Count];
        //    for (int i = 0; i < indices.Count; i++ )
        //    {
        //        indicesArray[i] = (ushort)indices[i];
        //    }

        //    fixed (ushort* ptr = indicesArray)
        //    {
        //        meshInfo.IB = MyManagers.Buffers.CreateIndexBuffer(indicesArray.Length, Format.R16_UInt, new IntPtr(ptr));
        //    }
        //    meshInfo.Indices = indicesArray;

        //    meshInfo.VB = new VertexBufferId[2];

        //    var verticesNum = positions.Count;
        //    MyVertexFormatPositionHalf4[] positionsArray = new MyVertexFormatPositionHalf4[verticesNum];
        //    MyVertexFormatTexcoordNormalTangent[] vArray = new MyVertexFormatTexcoordNormalTangent[verticesNum];

        //    for(int i=0; i<verticesNum; i++)
        //    {
        //        positionsArray[i] = new MyVertexFormatPositionHalf4(positions[i]);
        //        vArray[i] = new MyVertexFormatTexcoordNormalTangent(
        //            texcoords[i], normals[i], tangents[i]);
        //    }

        //    fixed (MyVertexFormatPositionHalf4* ptr = positionsArray)
        //    {
        //        meshInfo.VB[0] = MyManagers.Buffers.CreateVertexBuffer(verticesNum, sizeof(MyVertexFormatPositionHalf4), new IntPtr(ptr));

        //    }
        //    fixed (MyVertexFormatTexcoordNormalTangent* ptr = vArray)
        //    {
        //        meshInfo.VB[1] = MyManagers.Buffers.CreateVertexBuffer(verticesNum, sizeof(MyVertexFormatTexcoordNormalTangent), new IntPtr(ptr));
        //    }
        //    meshInfo.VertexPositions = positionsArray;
        //    meshInfo.VertexExtendedData = vArray;

        //    meshInfo.IndicesNum = indices.Count;
        //    meshInfo.VerticesNum = verticesNum;

        //    var submeshes = new Dictionary<string, List<MyDrawSubmesh>>();
        //    var submeshesMeta = new List<MySubmeshInfo>();

        //    for(int i=0; i<sections.Count; i++)
        //    {
        //        var matId = MyMeshMaterials1.GetMaterialId(sections[i].MaterialName);

        //        var list = submeshes.SetDefault(MyMeshMaterials1.Table[matId.Index].Technique, new List<MyDrawSubmesh>());

        //        list.Add(new MyDrawSubmesh(sections[i].TriCount * 3, sections[i].IndexStart, 0, MyMeshMaterials1.GetProxyId(matId)));

        //        submeshesMeta.Add(new MySubmeshInfo
        //        {
        //            IndexCount = sections[i].TriCount * 3,
        //            StartIndex = sections[i].IndexStart,
        //            BaseVertex = 0,
        //            Material = sections[i].MaterialName,
        //            Technique = MyMeshMaterials1.Table[matId.Index].Technique
        //        });
        //    }

        //    meshInfo.Parts = submeshes.ToDictionary(x => x.Key, x => x.Value.ToArray());
        //    meshInfo.m_submeshes = submeshesMeta;

        //    meshInfo.BoundingBox = aabb;

        //    m_loadingStatus = MyAssetLoadingEnum.Ready;
        //}

        //#endregion

        //public void DestroyResource()
        //{
        //    ModelsDictionary.Remove(m_name);
        //    Release();
        //}
    }
}
