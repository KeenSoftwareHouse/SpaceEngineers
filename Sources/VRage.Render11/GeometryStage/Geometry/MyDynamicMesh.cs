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
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;

namespace VRageRender
{
    //class MyDynamicMesh : MyMesh
    //{
    //    internal static List<MyDynamicMesh> m_list = new List<MyDynamicMesh>();

    //    internal static void RemoveAll()
    //    {
    //        foreach(var item in m_list)
    //        {
    //            item.Release();
    //        }
    //        m_list.Clear();
    //    }

    //    internal MyDynamicMesh()
    //    {
    //        LODs = new MyRenderLodInfo[1];
    //        LODs[0] = new MyRenderLodInfo();
    //        LODs[0].m_meshInfo = new MyRenderMeshInfo();
    //        LODs[0].LodNum = 0;
    //        LODs[0].Distance = 0;

    //        m_list.Add(this);
    //    }

    //    internal unsafe void UpdateData(
    //        int lod, 
    //        ushort[] indices,
    //        MyVertexFormatPositionHalf4[] stream0, 
    //        MyVertexFormatTexcoordNormalTangent[] stream1,
    //        VRageMath.BoundingBox ? aabb)
    //    {
    //        var meshInfo = LODs[lod].m_meshInfo;
    //        meshInfo.VertexLayout = MyVertexInputLayout.Empty().Append(MyVertexInputComponentType.POSITION_PACKED).Append(MyVertexInputComponentType.NORMAL, 1)
    //                        .Append(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1)
    //                        .Append(MyVertexInputComponentType.TEXCOORD0, 1);

    //        if (meshInfo.IB == IndexBufferId.NULL || meshInfo.IB.Capacity < indices.Length)
    //        {
    //            if (meshInfo.IB != IndexBufferId.NULL) { 
    //                //meshInfo.IB.Dispose();
    //                MyHwBuffers.DestroyIndexBuffer(meshInfo.IB);
    //                meshInfo.IB = IndexBufferId.NULL;
    //            }

    //            fixed(ushort *ptr = indices)
    //            {
    //                meshInfo.IB = MyHwBuffers.CreateIndexBuffer(indices.Length, Format.R16_UInt, new IntPtr(ptr));
    //            }
    //        }
    //        else
    //        {
    //            MyRender11.ImmediateContext.UpdateSubresource(indices, meshInfo.IB.Buffer);
    //        }

    //        if (meshInfo.VB == null || meshInfo.VB[0].Capacity < stream0.Length)
    //        {
    //            if (meshInfo.VB != null)
    //            {
    //                //meshInfo.VB[0].Dispose();
    //                //meshInfo.VB[1].Dispose();
    //                foreach(var vb in meshInfo.VB)
    //                {
    //                    MyHwBuffers.Destroy(vb);
    //                }
    //            }
    //            else
    //            {
    //                meshInfo.VB = new VertexBufferId[2];
    //            }

    //            fixed (MyVertexFormatPositionHalf4* ptr = stream0)
    //            {
    //                meshInfo.VB[0] = MyHwBuffers.CreateVertexBuffer(stream0.Length, sizeof(MyVertexFormatPositionHalf4), new IntPtr(ptr));
                    
    //            }
    //            fixed(MyVertexFormatTexcoordNormalTangent * ptr = stream1)
    //            {
    //                meshInfo.VB[1] = MyHwBuffers.CreateVertexBuffer(stream0.Length, sizeof(MyVertexFormatTexcoordNormalTangent), new IntPtr(ptr));
    //            }
    //        }
    //        else
    //        {
    //            MyRender11.ImmediateContext.UpdateSubresource(stream0, meshInfo.VB[0].Buffer);
    //            MyRender11.ImmediateContext.UpdateSubresource(stream1, meshInfo.VB[1].Buffer);
    //        }

    //        meshInfo.IndicesNum = indices.Length;
    //        meshInfo.VerticesNum = stream0.Length;

    //        var matId = MyMeshMaterials1.GetMaterialId("__ROPE_MATERIAL", "Textures/rope_cm.dds", "Textures/rope_ng.dds", "Textures/rope_add.dds", MyMesh.DEFAULT_MESH_TECHNIQUE);

    //        var submeshes = meshInfo.Parts.SetDefault(MyRenderableComponent.DEFAULT_MATERIAL_TAG, new MyDrawSubmesh[1]);
    //        submeshes[0] = new MyDrawSubmesh(indices.Length, 0, 0, MyMeshMaterials1.GetProxyId(matId));

    //        meshInfo.BoundingBox = aabb;

    //        m_loadingStatus = MyAssetLoadingEnum.Ready;
    //    }

    //    internal void Remove()
    //    {
    //        Release();
    //        // linear but how many dynamic meshes can we have?
    //        m_list.Remove(this);
    //    }
    //}

    class MyLineHelpers
    {
        static void AddTriangle(List<ushort> I, int i0, int i1, int i2)
        {
            I.Add((ushort)i0);
            I.Add((ushort)i1);
            I.Add((ushort)i2);
        }

        internal static ushort[] GenerateIndices(int verticesNum)
        {
            List<ushort> indices = new List<ushort>();

            ushort vertexCounter = 0;

            AddTriangle(indices, vertexCounter + 0, vertexCounter + 1, vertexCounter + 2);
            AddTriangle(indices, vertexCounter + 0, vertexCounter + 2, vertexCounter + 3);

            for (; vertexCounter < (verticesNum - 4); vertexCounter += 4)
            {
                AddTriangle(indices, vertexCounter + 0, vertexCounter + 4, vertexCounter + 5);
                AddTriangle(indices, vertexCounter + 0, vertexCounter + 5, vertexCounter + 1);
                AddTriangle(indices, vertexCounter + 1, vertexCounter + 5, vertexCounter + 6);
                AddTriangle(indices, vertexCounter + 1, vertexCounter + 6, vertexCounter + 2);
                AddTriangle(indices, vertexCounter + 2, vertexCounter + 6, vertexCounter + 7);
                AddTriangle(indices, vertexCounter + 2, vertexCounter + 7, vertexCounter + 3);
                AddTriangle(indices, vertexCounter + 3, vertexCounter + 7, vertexCounter + 4);
                AddTriangle(indices, vertexCounter + 3, vertexCounter + 4, vertexCounter + 0); 
            }

            AddTriangle(indices, vertexCounter + 2, vertexCounter + 1, vertexCounter + 0);
            AddTriangle(indices, vertexCounter + 3, vertexCounter + 2, vertexCounter + 0);

            return indices.ToArray();
        }

        internal static VRageMath.BoundingBoxD GetBoundingBox(ref Vector3D worldPointA, ref Vector3D worldPointB)
        {
            var worldPosition = (worldPointA + worldPointB) * 0.5f;
            var pointA = worldPointA - worldPosition;
            var pointB = worldPointB - worldPosition;

            var aabb = BoundingBoxD.CreateInvalid();
            aabb.Include(ref pointA);
            aabb.Include(ref pointB);
            aabb.Inflate(0.25);
            //aabb.Translate(worldPosition);
            return aabb;
        }

        internal static void GenerateVertexData(ref Vector3D worldPointA, ref Vector3D worldPointB,
            out MyVertexFormatPositionH4[] stream0, out MyVertexFormatTexcoordNormalTangent[] stream1)
        {
            var worldPosition = (worldPointA + worldPointB) * 0.5f;
            var pointA = (Vector3)(worldPointA - worldPosition);
            var pointB = (Vector3)(worldPointB - worldPosition);

            var length = (pointA - pointB).Length() * 10.0f;

            Vector3 lineTangent, normal, binormal;
            lineTangent = pointB - pointA;
            VRageMath.Vector3.Normalize(ref lineTangent, out lineTangent);
            lineTangent.CalculatePerpendicularVector(out normal);
            Vector3.Cross(ref lineTangent, ref normal, out binormal);

            var offsetX = normal * 0.025f;
            var offsetY = binormal * 0.025f;

            List<MyVertexFormatPositionH4> vertexPositionList = new List<MyVertexFormatPositionH4>();
            List<MyVertexFormatTexcoordNormalTangent> vertexList = new List<MyVertexFormatTexcoordNormalTangent>();

            unsafe
            {
                Vector3* points = stackalloc Vector3[2];
                points[0] = pointA;
                points[1] = pointB;
                int vertexCounter = 0;
                for (int i = 0; i < 2; ++i)
                {
                    int baseVertex = i * 4;
                    float texCoordX = (i - 0.5f) * length;

                    vertexPositionList.Add(new MyVertexFormatPositionH4(points[i] + offsetX));
                    vertexList.Add(new MyVertexFormatTexcoordNormalTangent(new Vector2(texCoordX, 0.0f), normal, Vector3.Cross(lineTangent, normal)));

                    vertexPositionList.Add(new MyVertexFormatPositionH4(points[i] + offsetY));
                    vertexList.Add(new MyVertexFormatTexcoordNormalTangent(new Vector2(texCoordX, 0.33333f), binormal, Vector3.Cross(lineTangent, binormal)));

                    vertexPositionList.Add(new MyVertexFormatPositionH4(points[i] - offsetX));
                    vertexList.Add(new MyVertexFormatTexcoordNormalTangent(new Vector2(texCoordX, 0.66667f), -normal, Vector3.Cross(lineTangent, -normal)));

                    vertexPositionList.Add(new MyVertexFormatPositionH4(points[i] - offsetY));
                    vertexList.Add(new MyVertexFormatTexcoordNormalTangent(new Vector2(texCoordX, 1.0f), -binormal, Vector3.Cross(lineTangent, -binormal)));
                }
            }

            stream0 = vertexPositionList.ToArray();
            stream1 = vertexList.ToArray();
        }
    }
}
