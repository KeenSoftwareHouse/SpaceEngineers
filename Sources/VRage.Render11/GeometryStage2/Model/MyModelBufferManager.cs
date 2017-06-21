using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct3D11;
using VRage.Import;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender;
using VRageRender.Import;
using VRageRender.Vertex;

namespace VRage.Render11.GeometryStage2.Model
{
    class MyModelBufferManager: IManager, IManagerUnloadData
    {
        // the method is static, because of removal all of the references on the member variables
        static unsafe IVertexBuffer CreateSimpleVB0(MyMwmData mwmData)
        {
            string name = "VB0-" + mwmData.MwmFilepath;

            if (!mwmData.IsAnimated)
            {
                MyVertexFormatPositionH4[] vertices = new MyVertexFormatPositionH4[mwmData.VerticesCount];
                for (int vertexIndex = 0; vertexIndex < mwmData.VerticesCount; ++vertexIndex)
                {
                    vertices[vertexIndex].Position = mwmData.Positions[vertexIndex];
                }
                fixed (void* ptr = vertices)
                {
                    return MyManagers.Buffers.CreateVertexBuffer(name, vertices.Length, MyVertexFormatPositionH4.STRIDE, new IntPtr(ptr), ResourceUsage.Immutable);
                }
            }
            else
            {
                MyVertexFormatPositionSkinning[] vertices = new MyVertexFormatPositionSkinning[mwmData.VerticesCount];
                for (int vertexIndex = 0; vertexIndex < mwmData.VerticesCount; ++vertexIndex)
                {
                    vertices[vertexIndex].Position = mwmData.Positions[vertexIndex];
                    vertices[vertexIndex].BoneIndices = new Byte4(mwmData.BoneIndices[vertexIndex].X, mwmData.BoneIndices[vertexIndex].Y, mwmData.BoneIndices[vertexIndex].Z, mwmData.BoneIndices[vertexIndex].W);
                    vertices[vertexIndex].BoneWeights = new HalfVector4(mwmData.BoneWeights[vertexIndex]);
                }
                fixed (void* ptr = vertices)
                {
                    return MyManagers.Buffers.CreateVertexBuffer(name, vertices.Length, MyVertexFormatPositionSkinning.STRIDE, new IntPtr(ptr), ResourceUsage.Immutable);
                }
            }
        }


        // the method is static, because of removal all of the references on the member variables
        static unsafe IVertexBuffer CreateSimpleVB1(MyMwmData mwmData)
        {
            MyRenderProxy.Assert(mwmData.IsValid2ndStream);

            string name = "VB1-" + mwmData.MwmFilepath;
            //Byte4 texIndices = new Byte4(0, 0, 0, 0);

            var vertices = new MyVertexFormatTexcoordNormalTangent[mwmData.VerticesCount];
            fixed (MyVertexFormatTexcoordNormalTangent* destinationPointer = vertices)
            {
                for (int vertexIndex = 0; vertexIndex < mwmData.VerticesCount; ++vertexIndex)
                {
                    destinationPointer[vertexIndex].Normal = mwmData.Normals[vertexIndex];
                    destinationPointer[vertexIndex].Tangent = mwmData.Tangents[vertexIndex];
                    destinationPointer[vertexIndex].Texcoord = mwmData.Texcoords[vertexIndex];
                    //destinationPointer[vertexIndex].TexIndices = texIndices;
                }
            }
            fixed (void* ptr = vertices)
            {
                return MyManagers.Buffers.CreateVertexBuffer(name, vertices.Length, MyVertexFormatTexcoordNormalTangent.STRIDE, new IntPtr(ptr), ResourceUsage.Immutable);
            }
        }

        // the method is static, because of removal all of the references on the member variables
        static unsafe IIndexBuffer CreateSimpleIB(MyMwmData mwmData)
        {
            string name = "IB-" + mwmData.MwmFilepath;

            List<MyMeshPartInfo> partInfos = mwmData.PartInfos;
            List<int> indicesInt = new List<int>();
            int maxValue = Int32.MinValue;
            foreach (var partInfo in partInfos)
            {
                foreach (var index in partInfo.m_indices)
                {
                    indicesInt.Add(index);
                    maxValue = Math.Max(index, maxValue);
                }
            }
            if (maxValue < ushort.MaxValue)
            {
                List<ushort> indicesUShort = new List<ushort>(indicesInt.Count);
                for (int i = 0; i < indicesInt.Count; i++)
                    indicesUShort.Add((ushort)(uint)indicesInt[i]);
                fixed (void* ptr = indicesUShort.ToArray())
                {
                    return MyManagers.Buffers.CreateIndexBuffer(name, indicesInt.Count, new IntPtr(ptr),
                        MyIndexBufferFormat.UShort, ResourceUsage.Immutable);
                }
            }
            else
                fixed (void* ptr = indicesInt.ToArray())
                {
                    return MyManagers.Buffers.CreateIndexBuffer(name, indicesInt.Count, new IntPtr(ptr),
                        MyIndexBufferFormat.UInt, ResourceUsage.Immutable);
                }
        }

        Dictionary<string, IVertexBuffer> m_vb0s = new Dictionary<string, IVertexBuffer>();
        Dictionary<string, IVertexBuffer> m_vb1s = new Dictionary<string, IVertexBuffer>();
        Dictionary<string, IIndexBuffer> m_ibs = new Dictionary<string, IIndexBuffer>();

        public IVertexBuffer GetOrCreateVB0(MyMwmData mwmData)
        {
            string filepath = mwmData.MwmFilepath;
            if (m_vb0s.ContainsKey(filepath))
                return m_vb0s[filepath];

            IVertexBuffer vb0 = CreateSimpleVB0(mwmData);
            m_vb0s.Add(filepath, vb0);
            return m_vb0s[filepath];
        }

        public IVertexBuffer GetOrCreateVB1(MyMwmData mwmData)
        {
            string filepath = mwmData.MwmFilepath;
            if (m_vb1s.ContainsKey(filepath))
                return m_vb1s[filepath];

            IVertexBuffer vb1 = CreateSimpleVB1(mwmData);
            m_vb1s.Add(filepath, vb1);
            return m_vb1s[filepath];
        }

        public IIndexBuffer GetOrCreateIB(MyMwmData mwmData)
        {
            string filepath = mwmData.MwmFilepath;
            if (m_ibs.ContainsKey(filepath))
                return m_ibs[filepath];

            IIndexBuffer ib = CreateSimpleIB(mwmData);
            m_ibs.Add(filepath, ib);
            return m_ibs[filepath];
        }

        public unsafe List<MyVertexInputComponent> CreateStandardVertexInputComponents(MyMwmData mwmData)
        {            
            List<MyVertexInputComponent> vertexComponents = new List<MyVertexInputComponent>();

            Debug.Assert(sizeof(HalfVector4) == sizeof(MyVertexFormatPositionH4));

            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));

            if (mwmData.IsAnimated)
            {
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_WEIGHTS));
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_INDICES));
            }

            if (mwmData.IsValid2ndStream)
            {
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL));
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT));
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H));
            }

            return vertexComponents;
        }

        public unsafe List<MyVertexInputComponent> CreateShadowVertexInputComponents(MyMwmData mwmData)
        {
            List<MyVertexInputComponent> vertexComponents = new List<MyVertexInputComponent>();

            Debug.Assert(sizeof(HalfVector4) == sizeof(MyVertexFormatPositionH4));

            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));

            if (mwmData.IsAnimated)
            {
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_WEIGHTS));
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_INDICES));
            }

            return vertexComponents;
        }

        void IManagerUnloadData.OnUnloadData()
        {
            foreach (var it in m_vb0s)
                MyManagers.Buffers.Dispose(it.Value);
            m_vb0s.Clear();

            foreach (var it in m_vb1s)
                MyManagers.Buffers.Dispose(it.Value);
            m_vb1s.Clear();

            foreach (var it in m_ibs)
                MyManagers.Buffers.Dispose(it.Value);
            m_ibs.Clear();
        }
    }
}
