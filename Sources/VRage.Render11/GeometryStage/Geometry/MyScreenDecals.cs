using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    struct MyScreenDecal
    {
        internal Matrix LocalOBB;
        internal uint ID; // backref
        internal uint ParentID;
        internal MyStringId Material;
    }

    struct MyDecalMaterial
    {
        internal MyScreenDecalType DecalType;
        internal TexId NormalmapTexture;
        internal TexId ColorMetalTexture;
        internal TexId AlphamaskTexture;
        internal string NormalmapTextureName;
        internal string ColorMetalTextureName;
        internal string AlphamaskTextureName;
    }

    internal class MyScreenDecalComparer : IComparer<int>
    {
        MyFreelist<MyScreenDecal> m_freelist;

        public MyScreenDecalComparer(MyFreelist<MyScreenDecal> freelist)
        {
            m_freelist = freelist;
        }

        public int Compare(int x, int y)
        {
            if (m_freelist.Data[x].Material.GetHashCode() == m_freelist.Data[y].Material.GetHashCode())
            {
                return x.CompareTo(y);
            }
            return m_freelist.Data[x].Material.GetHashCode().CompareTo(m_freelist.Data[y].Material.GetHashCode());
        }
    }

    struct MyDecalConstants
    {
        internal Matrix World;
        internal Matrix InvWorld;
    }
    
    static class MyScreenDecals
    {
        const int MAX_DECALS = 512;

        static VertexShaderId m_vs = VertexShaderId.NULL;
        static PixelShaderId m_ps = PixelShaderId.NULL;
        static IndexBufferId m_IB = IndexBufferId.NULL;

        internal static Dictionary<uint, int> IdIndex = new Dictionary<uint,int>();
        internal static Dictionary<uint, List<int>> EntityDecals = new Dictionary<uint, List<int>>();
        internal static MyFreelist<MyScreenDecal> Decals = new MyFreelist<MyScreenDecal>(1024);

        internal static Dictionary<MyStringId, MyDecalMaterial> Materials = new Dictionary<MyStringId, MyDecalMaterial>(MyStringId.Comparer);

        public static MyScreenDecalComparer DecalsMaterialComparer = new MyScreenDecalComparer(Decals);

        internal static void Init()
        {
            m_vs = MyShaders.CreateVs("decal.hlsl", "vs");
            m_ps = MyShaders.CreatePs("decal.hlsl", "ps");

            InitIB();
        }

        internal static void OnResourcesRequesting()
        {
            Materials[MyStringId.NullOrEmpty] = new MyDecalMaterial
            {
                DecalType = MyScreenDecalType.ScreenDecalColor,
                //NormalmapTexture = MyTextures.GetTexture("Textures/decals/impact_1x5_ng.dds", MyTextureEnum.NORMALMAP_GLOSS),
                NormalmapTexture = TexId.NULL,
                ColorMetalTexture = MyTextures.GetTexture("Textures/decals/impact_1_cm.dds", MyTextureEnum.COLOR_METAL),
                AlphamaskTexture = MyTextures.GetTexture("Textures/decals/impact_1_alphamask.dds", MyTextureEnum.ALPHAMASK),
            };
        }

        internal static void OnSessionEnd()
        {
            IdIndex.Clear();
            EntityDecals.Clear();
            Decals.Clear();
            Materials.Clear();
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();
            InitIB();
        }

        internal static void OnDeviceEnd()
        {
            if (m_IB != IndexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_IB);
                m_IB = IndexBufferId.NULL;
            }
        }

        static unsafe void InitIB()
        {
            ushort[] indices = new ushort[]
            {
                // 0 1 2 3
                0, 1, 2, 0, 2, 3,
                // 1 5 6 2
                1, 5, 6, 1, 6, 2,
                // 5 4 7 6
                5, 4, 7, 5, 7, 6,
                // 4 0 3 7
                4, 0, 3, 4, 3, 7,
                // 3 2 6 7
                3, 2, 6, 3, 6, 7,
                // 1 0 4 5
                1, 0, 4, 1, 4, 5
            };

            ushort[] indicesData = new ushort[MAX_DECALS * indices.Length];
            var instanceLen = indices.Length;
            for (int i = 0; i < MAX_DECALS; i++ )
            {
                for (int j = 0; j < instanceLen; j++)
                { 
                    indicesData[i * instanceLen + j] = (ushort)(indices[j] + 8 * i);
                }
            }

            if (m_IB == IndexBufferId.NULL)
            {
                fixed (ushort* I = indicesData)
                {
                    m_IB = MyHwBuffers.CreateIndexBuffer(indicesData.Length, Format.R16_UInt, BindFlags.IndexBuffer, ResourceUsage.Immutable, new IntPtr(I));
                }
            }
        }

        internal static void AddDecal(uint ID, uint ParentID, Matrix localOBB, string material)
        {
            var handle = Decals.Allocate();

            Decals.Data[handle].ID = ID;
            Decals.Data[handle].ParentID = ParentID;
            Decals.Data[handle].LocalOBB = localOBB;
            Decals.Data[handle].Material = X.TEXT(material);

            IdIndex[ID] = handle;
            
            if(!EntityDecals.ContainsKey(ParentID))
            {
                EntityDecals[ParentID] = new List<int>();
            }
            EntityDecals[ParentID].Add(handle);

            Debug.Assert(MyIDTracker<MyActor>.FindByID(ParentID) != null, "Decal added to non-existing render entity");
        }

        internal static void RemoveDecal(uint ID)
        {
            if(IdIndex.ContainsKey(ID))
            {
                var handle = IdIndex[ID];
                var parent = Decals.Data[handle].ParentID;
                EntityDecals[parent].Remove(handle);
                Decals.Free(handle);
                IdIndex.Remove(ID);
            }
            else
            {
                Debug.Assert(true, "Decal already removed");
            }
        }

        internal static void RemoveDecalByHandle(int handle)
        {
            IdIndex.Remove(Decals.Data[handle].ID);
            Decals.Free(handle);
        }

        internal static void RemoveEntityDecals(uint id)
        {
            if (!EntityDecals.ContainsKey(id))
            {
                return;
            }

            foreach (var handle in EntityDecals[id])
            {
                RemoveDecalByHandle(handle);
            }

            EntityDecals[id] = null;
        }

        internal static void RegisterMaterials(List<string> names, List<MyDecalMaterialDesc> descriptions)
        {
            Debug.Assert(names.Count == descriptions.Count);

            for(int i=0; i<names.Count; ++i)
            {
                Materials[X.TEXT(names[i])] = new MyDecalMaterial { 
                    DecalType = descriptions[i].DecalType,
                    NormalmapTexture = MyTextures.GetTexture(descriptions[i].NormalmapTexture, MyTextureEnum.NORMALMAP_GLOSS),
                    ColorMetalTexture = MyTextures.GetTexture(descriptions[i].ColorMetalTexture, MyTextureEnum.COLOR_METAL),
                    AlphamaskTexture = MyTextures.GetTexture(descriptions[i].AlphamaskTexture, MyTextureEnum.ALPHAMASK),
                };
            }
        }

        static List<Matrix> m_matrices = new List<Matrix>();

        unsafe static void DrawBatch(MyScreenDecalType decalType)
        {
            if(m_matrices.Count > 0)
            {
                var decalCb = MyCommon.GetObjectCB(sizeof(MyDecalConstants) * MAX_DECALS);

                var N = (int)(Math.Min(MAX_DECALS, m_matrices.Count));
                var mapping = MyMapping.MapDiscard(decalCb);
                for (int i = 0; i < N; ++i)
                {
                    MyMatrix4x3 worldMatrix = new MyMatrix4x3();
                    worldMatrix.Matrix4x4 = m_matrices[i];

                    mapping.stream.Write(worldMatrix);
                    mapping.stream.Write(new Vector4(decalType == MyScreenDecalType.ScreenDecalBump ? 1 : 0, 0, 0, 0));
                    mapping.stream.Write(Matrix.Transpose(Matrix.Invert(m_matrices[i])));
                }
                mapping.Unmap();

                MyImmediateRC.RC.Context.DrawIndexed(36 * N, 0, 0);
            }
            m_matrices.Clear();
        }

        // can be on another job
        internal unsafe static void Draw()
        {
            var decals = IdIndex.Values.ToArray();
            // sort visible decals by material
            Array.Sort(decals, DecalsMaterialComparer);

            ///
            // copy gbuffer with normals for read (uhoh)
            // bind copy and depth for read
            // bind gbuffer for write

            var RC = MyImmediateRC.RC;
            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetIB(m_IB.Buffer, m_IB.Format);
            RC.SetIL(null);
            RC.Context.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.BindDepthRT(
                MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.DepthReadOnly,
                MyGBuffer.Main.Get(MyGbufferSlot.GBuffer0),
                MyGBuffer.Main.Get(MyGbufferSlot.GBuffer1),
                MyGBuffer.Main.Get(MyGbufferSlot.GBuffer2));
            RC.SetVS(m_vs);
            RC.SetPS(m_ps);
            RC.SetDS(MyDepthStencilState.DepthTest);
            RC.Context.PixelShader.SetSamplers(0, MyRender11.StandardSamplers);

            RC.BindSRV(0, MyGBuffer.Main.DepthStencil.Depth);
            RC.Context.PixelShader.SetShaderResources(1, MyRender11.m_gbuffer1Copy.ShaderView);

            var decalCb = MyCommon.GetObjectCB(sizeof(MyDecalConstants) * MAX_DECALS);
            RC.SetCB(2, decalCb);
            
            var prevMaterial = new MyStringId();
            MyScreenDecalType decalType = MyScreenDecalType.ScreenDecalBump;

            for (int i = 0; i < decals.Length; ++i)
            {
                var index = decals[i];

                var material = Decals.Data[index].Material;
                if(i == 0 || material != prevMaterial)
                {
                    DrawBatch(decalType);

                    var matDesc = Materials[material];

                    decalType = matDesc.DecalType;
                    // factor 1 makes overwriting of gbuffer color & subtracting from ao
                    RC.SetBS(MyRender11.BlendDecal, matDesc.DecalType == MyScreenDecalType.ScreenDecalBump ? new SharpDX.Color4(0) : SharpDX.Color4.White);
                    RC.Context.PixelShader.SetShaderResources(3, MyTextures.GetView(matDesc.AlphamaskTexture), MyTextures.GetView(matDesc.ColorMetalTexture), MyTextures.GetView(matDesc.NormalmapTexture));
                }

                var parent = MyIDTracker<MyActor>.FindByID(Decals.Data[index].ParentID);
                if(parent != null)
                {
                    var parentMatrix = parent.WorldMatrix;
                    var volumeMatrix = Decals.Data[index].LocalOBB * parentMatrix * Matrix.CreateTranslation(-MyEnvironment.CameraPosition);

                    m_matrices.Add(volumeMatrix);
                }
            }
            DrawBatch(decalType);

            RC.SetBS(null);
        }

    }
}
