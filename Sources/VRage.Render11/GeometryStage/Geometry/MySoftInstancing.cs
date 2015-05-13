using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Common.Import;
using VRage.Common.Utils;
using VRage.Render11.Shaders;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector4 = VRageMath.Vector4;
using Matrix = VRageMath.Matrix;
using SharpDX.Direct3D;

namespace VRageRender
{
    struct MyDraw {
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
        internal string Model;
        internal int Lod;
        internal int Part;
    }

    class MyMeshTableSRV
    {
        #region Static
        internal static MyVertexInputLayout OneAndOnlySupportedVertexLayout;

        internal static void Init()
        {
            OneAndOnlySupportedVertexLayout = MyVertexInputLayout.Empty().Append(MyVertexInputComponentType.POSITION_PACKED)
                .Append(MyVertexInputComponentType.NORMAL, 1)
                .Append(MyVertexInputComponentType.TANGENT_BITANSGN, 1)
                .Append(MyVertexInputComponentType.TEXCOORD0, 1);
        }
        #endregion

        internal bool IsMergable(MyMesh mesh)
        {
            return mesh.LODs[0].m_meshInfo.VertexLayout == OneAndOnlySupportedVertexLayout;
        }

        List<MyVertexFormatPositionHalf4> m_vertexPositionList = new List<MyVertexFormatPositionHalf4>();
        List<MyVertexFormatTexcoordNormalTangent> m_vertexList = new List<MyVertexFormatTexcoordNormalTangent>();
        List<uint> m_indicesList = new List<uint>();

        int m_indexPageSize = 0;
        int m_pagesUsed = 0;

        MyStructuredBuffer m_VB_positions;
        MyStructuredBuffer m_VB_rest;
        MyStructuredBuffer m_IB;

        internal MyMeshTableSRV(int pageSize = 36)
        {
            m_indexPageSize = pageSize;
        }

        Dictionary<MyMeshTableEntry, MyMeshTableSRV_Entry> m_table = new Dictionary<MyMeshTableEntry, MyMeshTableSRV_Entry>();

        internal void AddData(
            MyMeshTableEntry key,
            MyVertexFormatPositionHalf4 [] positions, MyVertexFormatTexcoordNormalTangent [] vertices, uint [] indices)
        {
            var vertexOffset = (uint)m_vertexPositionList.Count;
            var indexOffset = (uint)m_indicesList.Count;

            m_vertexPositionList.AddArray(positions);
            m_vertexList.AddArray(vertices);

            var list = new List<int>();

            for (int k = 0; k < indices.Length; k += m_indexPageSize)
            {
                int iEnd = Math.Min(k + m_indexPageSize, indices.Length);

                for (int i = k; i < iEnd; i++)
                {
                    m_indicesList.Add(indices[i] + indexOffset);
                }

                list.Add(m_pagesUsed++);
            }

            if ((indices.Length % m_indexPageSize) != 0)
            {
                uint lastIndex = m_indicesList[m_indicesList.Count - 1];
                for (int i = indices.Length % m_indexPageSize; i < m_indexPageSize; i++)
                {
                    m_indicesList.Add(lastIndex);
                }
            }

            m_table.Add(key, new MyMeshTableSRV_Entry { Pages = list });
        }

        internal unsafe void MoveToGPU()
        {
            m_VB_positions = new MyStructuredBuffer(m_vertexPositionList.Count, sizeof(MyVertexFormatPositionHalf4), ResourceUsage.Default, CpuAccessFlags.None);
            m_VB_rest = new MyStructuredBuffer(m_vertexList.Count, sizeof(MyVertexFormatTexcoordNormalTangent), ResourceUsage.Default, CpuAccessFlags.None);
            m_IB = new MyStructuredBuffer(m_indicesList.Count, sizeof(uint), ResourceUsage.Default, CpuAccessFlags.None);
        }

        internal void Release()
        {
            if(m_VB_positions != null)
            {
                m_VB_positions.Release();
                m_VB_rest.Release();
                m_IB.Release();
            }

            m_VB_positions = null;
            m_VB_rest = null;
            m_IB = null;
        }
    }

    class MyMergeInstancing
    {
        string m_rootMaterial;
        MyMeshTableSRV m_data;

        MyStructuredBuffer m_indirectionBuffer;
        MyStructuredBuffer m_instanceBuffer;

        int m_pageSize = 36;
        int m_pagesUsed = 0;

        internal static void Init()
        {
            
        }

        internal bool IsMergable(MyMesh mesh)
        {
            // check if one and only spoorted vertex format
            // check if same material as the rest
            // check if has one part(!) 
            // check if has one lod - for now

            return
                mesh.LODs.Length == 1 &&
                mesh.LODs[0].m_meshInfo.PartsNum == 1 &&
                mesh.LODs[0].m_meshInfo.VertexLayout == MyMeshTableSRV.OneAndOnlySupportedVertexLayout &&
                MyMaterials.AreMergable(mesh.LODs[0].m_meshInfo.m_submeshes[MyMesh.DEFAULT_MESH_TECHNIQUE][0].Material, m_rootMaterial);
        }

        void AddMesh(MyMesh mesh)
        {
            Debug.Assert(IsMergable(mesh));

            // add to my big mesh
            // get info


        }

        internal void AddEntity(uint ID, string model)
        {

        }

        internal void UpdateEntity(uint ID, ref Matrix matrix)
        {

        }

        internal void RemoveEntity(uint ID)
        {
        }

        internal void TransferToGpu()
        {

        }

        static MyMergeInstancing m_test;
        internal static void TestModel(MyMesh mesh)
        {
            if (m_test == null)
            {
                m_test = new MyMergeInstancing();
                m_test.m_rootMaterial = MyAssetsLoader.GetModel("Models//Cubes//Large//StoneCube.mwm").LODs[0].m_meshInfo.m_submeshes[MyMesh.DEFAULT_MESH_TECHNIQUE][0].Material;
            }

            if (m_test.IsMergable(mesh))
            {
                Debug.WriteLine(String.Format("{0} mergable: {1}", mesh.Name, m_test.IsMergable(mesh)));
            }
        }
    }

    class MyParent
    {
        // list of nonmerged instances

        // or dict of
        internal MyMergeInstancing m_mergeGroup;
    }


    struct MyInstancedMeshPages
    {
        internal List<int> Pages;
    }

    struct MyInstancingTableEntry
    {
        internal int MeshId;
        internal int InstanceId;
    }

    struct MyPerInstanceData
    {
        internal Vector4 Row0;
        internal Vector4 Row1;
        internal Vector4 Row2;

        internal static MyPerInstanceData FromWorldMatrix(ref Matrix mat)
        {
            return new MyPerInstanceData
            {
                Row0 = new Vector4(mat.M11, mat.M21, mat.M31, mat.M41),
                Row1 = new Vector4(mat.M12, mat.M22, mat.M32, mat.M42),
                Row2 = new Vector4(mat.M13, mat.M23, mat.M33, mat.M43)
            };
        }
    }

    class MyInstancingMethod
    {
        internal virtual void AddMesh(string mesh)
        {

        }

        internal virtual int AddInstance(string mesh, Matrix matrix)
        {
            return -1;
        }

        internal virtual unsafe void SendToGpu()
        {

        }

        internal virtual void Draw()
        {

        }

        internal virtual void Clear()
        {

        }
    }

    // handles only models with one material (part) - can be extended later if necessary
    class MySoftInstancingGroup : MyInstancingMethod
    {
        Dictionary<string, List<int>> m_includedMeshes = new Dictionary<string, List<int>>();
        int m_pageSize = 36;

        int m_pagesUsed = 0;
        // every mesh has list of pages with its data

        List<MyVertexFormatPositionHalf4> m_vertexPositionList = new List<MyVertexFormatPositionHalf4>();
        List<MyVertexFormatTexcoordNormalTangent> m_vertexNormalList = new List<MyVertexFormatTexcoordNormalTangent>();
        List<MyPerInstanceData> m_perInstance = new List<MyPerInstanceData>();

        List<MyInstancingTableEntry> m_pageTable = new List<MyInstancingTableEntry>();
        int instancesCounter = 0;

        // big buffers for vertex data pages
        // vertex buffers 0...N - StructuredBuffers for sampled data (SoA? 2xAoS?)

        // instance buffer
        // buffer of <model id, instance id> tuples - indirection for sampling vertex pages
        // buffer of per instance data like matrix

        //List<>
        // instance world matrix

        MyStructuredBuffer m_indirectionBuffer;
        MyStructuredBuffer m_instanceBuffer;
        MyStructuredBuffer m_vertexPositionBuffer;
        MyStructuredBuffer m_vertexNormalBuffer;

        static MyVertexShader m_vs = MyShaderFactory.CreateVS("instancing.hlsl", "vs", MyShaderHelpers.FormatMacros(MyRender.ShaderMultisamplingDefine()), null);
        static MyPixelShader m_ps = MyShaderFactory.CreatePS("instancing.hlsl", "ps", MyShaderHelpers.FormatMacros(MyRender.ShaderMultisamplingDefine()));

        internal static void Init()
        {
        }

        internal override void Clear()
        {
            if(m_indirectionBuffer != null)
            {
                m_indirectionBuffer.Release();
                m_instanceBuffer.Release();
                m_vertexPositionBuffer.Release();
                m_vertexNormalBuffer.Release();
            }

            m_indirectionBuffer = null;
            m_instanceBuffer = null;
            m_vertexPositionBuffer = null;
            m_vertexNormalBuffer = null;
        }

        internal override void AddMesh(string mesh)
        {
            if (m_includedMeshes.Get(mesh) != null)
                return;

            var meshInfo = MyAssetsLoader.GetModel(mesh).LODs[0].m_meshInfo;
            var indices = meshInfo.Indices;

            List<int> meshPages = new List<int>();

            for (int k = 0; k < indices.Length; k += m_pageSize)
            {
                int iEnd = Math.Min(k + m_pageSize, indices.Length);

                for (int i = k; i < iEnd; i++)
                {
                    m_vertexPositionList.Add(meshInfo.VertexPositions[indices[i]]);
                    m_vertexNormalList.Add(meshInfo.VertexExtendedData[indices[i]]);
                }

                meshPages.Add(m_pagesUsed++);
            }

            if ((indices.Length % m_pageSize) != 0)
            {
                // add degenerate triangles
                for (int i = indices.Length % m_pageSize; i < m_pageSize; i++)
                {
                    m_vertexPositionList.Add(new MyVertexFormatPositionHalf4());
                    m_vertexNormalList.Add(new MyVertexFormatTexcoordNormalTangent());
                }
            }

            m_includedMeshes[mesh] = meshPages;
        }

        internal override int AddInstance(string mesh, Matrix matrix)
        {
            AddMesh(mesh);

            var instanceNum = instancesCounter;

            foreach(var page in m_includedMeshes[mesh])
            {
                m_pageTable.Add(new MyInstancingTableEntry { InstanceId = instanceNum, MeshId = page });
            }

            m_perInstance.Add(MyPerInstanceData.FromWorldMatrix(ref matrix));

            instancesCounter++;

            return 0;
        }

        internal override unsafe void SendToGpu()
        {
            {
                var array = m_pageTable.ToArray();

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_indirectionBuffer = new MyStructuredBuffer(m_pageTable.Count, sizeof(MyInstancingTableEntry), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
            {
                var array = m_perInstance.ToArray();

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_instanceBuffer = new MyStructuredBuffer(m_perInstance.Count, sizeof(MyPerInstanceData), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
            {
                var array = m_vertexPositionList.ToArray();

                fixed(void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_vertexPositionBuffer = new MyStructuredBuffer(m_vertexPositionList.Count, sizeof(MyVertexFormatPositionHalf4), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
            {
                var array = m_vertexNormalList.ToArray();

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_vertexNormalBuffer = new MyStructuredBuffer(m_vertexNormalList.Count, sizeof(MyVertexFormatTexcoordNormalTangent), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
        }

        internal override void Draw()
        {
            var RC = MyImmediateRC.RC;


            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var mapping = MyMapping.MapDiscard(RC.Context, MyCommon.ProjectionConstants.Buffer);
            mapping.stream.Write(Matrix.Transpose(MyEnvironment.ViewProjection));
            mapping.Unmap();

            RC.Context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

            RC.Context.PixelShader.SetSamplers(0, MyRender.StandardSamplers);

            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants.Buffer);
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants.Buffer);

            RC.SetVS(m_vs);
            RC.SetPS(m_ps);

            RC.BindRawSRV(0, m_indirectionBuffer.m_SRV, m_instanceBuffer.m_SRV, m_vertexPositionBuffer.m_SRV, m_vertexNormalBuffer.m_SRV);

            RC.BindGBufferForWrite(MyGBuffer.Main);

            RC.Context.Draw(m_pageSize * m_pageTable.Count, 0);
        }
    }

    class MySoftInstancingGroup2 : MyInstancingMethod
    {
        Dictionary<string, List<int>> m_includedMeshes = new Dictionary<string, List<int>>();
        int m_pageSize = 36;

        int m_pagesUsed = 0;

        List<uint> m_indicesList = new List<uint>();

        List<MyVertexFormatPositionHalf4> m_vertexPositionList = new List<MyVertexFormatPositionHalf4>();
        List<MyVertexFormatTexcoordNormalTangent> m_vertexNormalList = new List<MyVertexFormatTexcoordNormalTangent>();
        List<MyPerInstanceData> m_perInstance = new List<MyPerInstanceData>();

        List<MyInstancingTableEntry> m_pageTable = new List<MyInstancingTableEntry>();
        int instancesCounter = 0;

        MyStructuredBuffer m_indirectionBuffer;
        MyStructuredBuffer m_instanceBuffer;
        MyStructuredBuffer m_indicesBuffer;
        MyStructuredBuffer m_vertexPositionBuffer;
        MyStructuredBuffer m_vertexNormalBuffer;

        static MyVertexShader m_vs = MyShaderFactory.CreateVS("instancing2.hlsl", "vs", MyShaderHelpers.FormatMacros(MyRender.ShaderMultisamplingDefine()), null);
        static MyPixelShader m_ps = MyShaderFactory.CreatePS("instancing2.hlsl", "ps", MyShaderHelpers.FormatMacros(MyRender.ShaderMultisamplingDefine()));

        internal static void Init()
        {
        }

        internal override void Clear()
        {
            if (m_indirectionBuffer != null)
            {
                m_indirectionBuffer.Release();
                m_instanceBuffer.Release();
                m_indicesBuffer.Release();
                m_vertexPositionBuffer.Release();
                m_vertexNormalBuffer.Release();
            }

            m_indirectionBuffer = null;
            m_instanceBuffer = null;
            m_indicesBuffer = null;
            m_vertexPositionBuffer = null;
            m_vertexNormalBuffer = null;
        }

        internal override void AddMesh(string mesh)
        {
            if (m_includedMeshes.Get(mesh) != null)
                return;

            var meshInfo = MyAssetsLoader.GetModel(mesh).LODs[0].m_meshInfo;
            var indices = meshInfo.Indices;

            List<int> meshPages = new List<int>();

            uint indexOffset = (uint)m_vertexPositionList.Count;

            for (int v = 0; v < meshInfo.VertexPositions.Length; v++ )
            {
                m_vertexPositionList.Add(meshInfo.VertexPositions[v]);
                m_vertexNormalList.Add(meshInfo.VertexExtendedData[v]);
            }


            for (int k = 0; k < indices.Length; k += m_pageSize)
            {
                int iEnd = Math.Min(k + m_pageSize, indices.Length);

                for (int i = k; i < iEnd; i++)
                {
                    m_indicesList.Add(indices[i] + indexOffset);
                }

                meshPages.Add(m_pagesUsed++);
            }

            if ((indices.Length % m_pageSize) != 0)
            {
                uint lastIndex = m_indicesList[m_indicesList.Count - 1];
                for (int i = indices.Length % m_pageSize; i < m_pageSize; i++)
                {
                    m_indicesList.Add(lastIndex);
                }
            }

            m_includedMeshes[mesh] = meshPages;
        }

        internal override int AddInstance(string mesh, Matrix matrix)
        {
            AddMesh(mesh);

            var instanceNum = instancesCounter;

            foreach (var page in m_includedMeshes[mesh])
            {
                m_pageTable.Add(new MyInstancingTableEntry { InstanceId = instanceNum, MeshId = page });
            }

            m_perInstance.Add(MyPerInstanceData.FromWorldMatrix(ref matrix));

            instancesCounter++;

            return 0;
        }

        internal override unsafe void SendToGpu()
        {
            {
                var array = m_pageTable.ToArray();

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_indirectionBuffer = new MyStructuredBuffer(m_pageTable.Count, sizeof(MyInstancingTableEntry), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
            {
                var array = m_perInstance.ToArray();

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_instanceBuffer = new MyStructuredBuffer(m_perInstance.Count, sizeof(MyPerInstanceData), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
            {
                var array = m_indicesList.ToArray();

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_indicesBuffer = new MyStructuredBuffer(m_indicesList.Count, sizeof(uint), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
            {
                var array = m_vertexPositionList.ToArray();

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_vertexPositionBuffer = new MyStructuredBuffer(m_vertexPositionList.Count, sizeof(MyVertexFormatPositionHalf4), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
            {
                var array = m_vertexNormalList.ToArray();

                fixed (void* ptr = array)
                {
                    var intPtr = new IntPtr(ptr);
                    m_vertexNormalBuffer = new MyStructuredBuffer(m_vertexNormalList.Count, sizeof(MyVertexFormatTexcoordNormalTangent), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
                }
            }
        }

        internal override void Draw()
        {
            var RC = MyImmediateRC.RC;


            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var mapping = MyMapping.MapDiscard(RC.Context, MyCommon.ProjectionConstants.Buffer);
            mapping.stream.Write(Matrix.Transpose(MyEnvironment.ViewProjection));
            mapping.Unmap();

            RC.Context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

            RC.Context.PixelShader.SetSamplers(0, MyRender.StandardSamplers);

            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants.Buffer);
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants.Buffer);

            RC.SetVS(m_vs);
            RC.SetPS(m_ps);

            RC.BindRawSRV(0, m_indirectionBuffer.m_SRV, m_instanceBuffer.m_SRV, m_indicesBuffer.m_SRV, m_vertexPositionBuffer.m_SRV, m_vertexNormalBuffer.m_SRV);

            RC.BindGBufferForWrite(MyGBuffer.Main);

            RC.Context.Draw(m_pageSize * m_pageTable.Count, 0);
        }
    }

    class MyPerMeshInstancing
    {
        internal List<MyPerInstanceData> m_perInstance = new List<MyPerInstanceData>();
        internal MyStructuredBuffer m_instanceBuffer;
        internal int instancesCounter = 0;

        internal void Clear()
        {
            if (m_instanceBuffer != null)
            {
                m_instanceBuffer.Release();
            }

            m_instanceBuffer = null;
        }

        internal unsafe void Transfer()
        {
            var array = m_perInstance.ToArray();

            fixed (void* ptr = array)
            {
                var intPtr = new IntPtr(ptr);
                m_instanceBuffer = new MyStructuredBuffer(m_perInstance.Count, sizeof(MyPerInstanceData), ResourceUsage.Default, CpuAccessFlags.None, intPtr);
            }
        }
    }

    class MyClassicInstancing : MyInstancingMethod
    {
        Dictionary<string, MyPerMeshInstancing> m_meshes = new Dictionary<string, MyPerMeshInstancing>();

        static MyVertexShader m_vs = MyShaderFactory.CreateVS("instancing3.hlsl", "vs", MyShaderHelpers.FormatMacros(MyRender.ShaderMultisamplingDefine()), new OnCompileCallbackType(CreateInputLayout));
        static MyPixelShader m_ps = MyShaderFactory.CreatePS("instancing3.hlsl", "ps", MyShaderHelpers.FormatMacros(MyRender.ShaderMultisamplingDefine()));
        static InputLayout m_inputLayout;

        internal static void Init()
        {
        }

        internal static void CreateInputLayout(byte[] bytecode)
        {
            m_inputLayout = MyVertexInputLayout.CreateLayout(MyVertexInputLayout.Empty().Append(MyVertexInputComponentType.POSITION_PACKED)
                .Append(MyVertexInputComponentType.TEXCOORD0, 1).Append(MyVertexInputComponentType.NORMAL, 1).Append(MyVertexInputComponentType.TANGENT_BITANSGN, 1).Hash, bytecode);
        }

        internal override void Clear()
        {
            foreach(var m in m_meshes)
            {
                m.Value.Clear();
            }

            m_meshes.Clear();
        }

        internal override void AddMesh(string mesh)
        {
            if(!m_meshes.ContainsKey(mesh))
            {
                m_meshes[mesh] = new MyPerMeshInstancing();
            }
        }

        internal override int AddInstance(string mesh, Matrix matrix)
        {
            AddMesh(mesh);

            m_meshes[mesh].m_perInstance.Add(MyPerInstanceData.FromWorldMatrix(ref matrix));
            m_meshes[mesh].instancesCounter++;


            return 0;
        }

        internal override unsafe void SendToGpu()
        {
            foreach (var m in m_meshes)
            {
                m.Value.Transfer();
            }            
        }

        internal override void Draw()
        {
            var RC = MyImmediateRC.RC;

            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var mapping = MyMapping.MapDiscard(RC.Context, MyCommon.ProjectionConstants.Buffer);
            mapping.stream.Write(Matrix.Transpose(MyEnvironment.ViewProjection));
            mapping.Unmap();

            RC.Context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

            RC.Context.PixelShader.SetSamplers(0, MyRender.StandardSamplers);

            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants.Buffer);
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants.Buffer);

            RC.SetVS(m_vs);
            RC.SetPS(m_ps);
            RC.SetIL(m_inputLayout);

            RC.BindGBufferForWrite(MyGBuffer.Main);

            foreach (var m in m_meshes)
            {
                RC.BindRawSRV(0, m.Value.m_instanceBuffer.m_SRV);

                var meshInfo = MyAssetsLoader.GetModel(m.Key).LODs[0].m_meshInfo;

                RC.SetVB(meshInfo.VB.Select(x => x.Buffer).ToArray(), meshInfo.VB.Select(x => x.Stride).ToArray());
                RC.SetIB(meshInfo.IB.Buffer, meshInfo.IB.Format);

                RC.Context.DrawIndexedInstanced(meshInfo.IndicesNum, m.Value.instancesCounter, 0, 0, 0);
            }
        }
    }


    class MyInstancingTest
    {
        internal static void DrawNormal()
        {

        }

        static MyInstancingMethod m_group;

        internal static void DrawBatched()
        {
            bool rebuild = false;

            if(m_group == null || rebuild)
            {
                if(m_group != null)
                {
                    m_group.Clear();
                }

                //m_group = new MyClassicInstancing();
                m_group = new MySoftInstancingGroup2();

                HashSet<string> testModels = new HashSet<string>();
                testModels.Add("Models//Cubes//Large//StoneCube.mwm");
                testModels.Add("Models//Cubes//Large//BattlementAdvancedRoundLarge.mwm");
                testModels.Add("Models//Cubes//large//RoofTileCornerRoundTallFake.mwm");
                testModels.Add("Models//Cubes//Large//StoneSlopeStairs.mwm");
                testModels.Add("Models//Debris//Debris10.mwm");
                testModels.Add("Models//Cubes//small//RotorBlockCogWheel2.mwm");
                testModels.Add("Models//Cubes//Large//BattlementStoneCorner.mwm");
                testModels.Add("Models//Cubes//Large//GeneratedStoneBattlementCorner.mwm");
                testModels.Add("Models//Cubes//Large//HouseStoneRoundedFull.mwm");
                testModels.Add("Models//Cubes//large//RoofTileCornerRoundTallFake.mwm");
                testModels.Add("Models//Cubes//large//StoneBattlementAdvancedRound_5.mwm");
                testModels.Add("Models//Cubes//large//RoofTileSlopeWoodFakeTopRight.mwm");
                testModels.Add("Models//Cubes//Large//CrossRoad.mwm");

                var rnd = new Random();

                for (int i = 0; i < 100; i++)
                {
                    for(int j=0; j < 100; j++)
                    {
                        for (int k = 0; k < 2; k++ )
                        {
                            m_group.AddInstance(testModels.ElementAt(rnd.Next(0, testModels.Count)), Matrix.CreateTranslation(50.0f + i * 4.0f, 150.0f + k * 4.0f, 50.0f + (j + 0) * 4.0f));
                        }
                    }
                }

                m_group.SendToGpu();

                rebuild = false;
            }

            m_group.Draw();
        }

        internal static void Test()
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("SoftInstancingTest");
            MyGpuProfiler.IC_BeginBlock("SoftInstancingTest");
            DrawBatched();
            
            MyGpuProfiler.IC_EndBlock();
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }
    }
}
