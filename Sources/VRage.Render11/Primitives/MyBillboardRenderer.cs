using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageMath;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Vector4 = VRageMath.Vector4;
using VRageRender.Vertex;
using VRageMath.PackedVector;
using VRageRender.Resources;
using VRage;
using VRage.Utils;
using System.IO;
using VRage.Library.Utils;
using VRage.Generics;
using System.Diagnostics;
using VRage.FileSystem;

namespace VRageRender
{
    struct MyBillboardBatch
    {
        internal int Offset;
        internal int Num;
        internal ShaderResourceView Texture;
        internal bool Lit;
    }

    struct MyBillboardData
    {
        internal int CustomProjectionID;
        internal Color Color;
        internal float Reflective;
        internal float _padding0;
        internal Vector3 Normal;
        internal float _padding1;
    }

    struct MyTextureAtlasElement
    {
        internal TexId TextureId;
        internal Vector4 UvOffsetScale;
    }

    class MyTextureAtlas
    {
        internal static void ParseAtlasDescription(string textureDir, string atlasFile, Dictionary<string, MyTextureAtlasElement> atlasDict)
        {
            try
            {
                //var atlas = new MyTextureAtlas(64);
                var fsPath = Path.Combine(MyFileSystem.ContentPath, atlasFile);
                using (var file = MyFileSystem.OpenRead(fsPath))
                using (StreamReader sr = new StreamReader(file))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (line.StartsWith("#"))
                            continue;
                        if (line.Trim(' ').Length == 0)
                            continue;

                        string[] parts = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

                        string name = parts[0];
                        string atlasName = parts[1];

                        Vector4 uv = new Vector4(
                            Convert.ToSingle(parts[4], System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToSingle(parts[5], System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToSingle(parts[7], System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToSingle(parts[8], System.Globalization.CultureInfo.InvariantCulture));

                        name = textureDir + System.IO.Path.GetFileName(name);
                        var atlasTexture = textureDir + atlasName;

                        var element = new MyTextureAtlasElement();
                        element.TextureId = MyTextures.GetTexture(atlasTexture, MyTextureEnum.GUI, true);
                        element.UvOffsetScale = uv;
                        atlasDict[name] = element;
                    }
                }

            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Warning: " + e.ToString());
            }
        }
    }

    internal static class MyBillboardsHelper
    {
        internal static MyBillboard SpawnBillboard()
        {
            var billboard = MyBillboardRenderer.m_billboardsOncePool.Allocate();
            MyBillboardRenderer.m_billboardsOnce.Add(billboard);
            return billboard;
        }

        internal static void AddPointBillboard(string material,
           Color color, Vector3D origin, float radius, float angle, int priority = 0, int customViewProjection = -1)
        {
            Debug.Assert(material != null);

            origin.AssertIsValid();
            angle.AssertIsValid();

            MyQuadD quad;
            if (MyUtils.GetBillboardQuadAdvancedRotated(out quad, origin, radius, angle, MyEnvironment.CameraPosition) != false)
            {
                MyBillboard billboard = SpawnBillboard();
                if (billboard == null)
                    return;

                billboard.Priority = priority;
                billboard.CustomViewProjection = customViewProjection;
                CreateBillboard(billboard, ref quad, material, ref color, ref origin);
            }
        }


        internal static void AddBillboardOriented(string material,
            Color color, Vector3D origin, Vector3 leftVector, Vector3 upVector, float radius, int priority = 0, int customViewProjection = -1)
        {
            Debug.Assert(material != null);

            origin.AssertIsValid();
            leftVector.AssertIsValid();
            upVector.AssertIsValid();
            radius.AssertIsValid();
            MyDebug.AssertDebug(radius > 0);

            MyBillboard billboard = SpawnBillboard();
            if (billboard == null)
                return;

            billboard.Priority = priority;
            billboard.CustomViewProjection = customViewProjection;

            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref origin, radius, ref leftVector, ref upVector);

            CreateBillboard(billboard, ref quad, material, ref color, ref origin);
        }

        internal static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material, ref Color color, ref Vector3D origin,
            string blendMaterial = "Test", float textureBlendRatio = 0, Vector2 uvOffset = new Vector2(), bool near = false, bool lowres = false, float reflectivity = 0)
        {
            Debug.Assert(material != null);
            Debug.Assert(blendMaterial != null);

            if (string.IsNullOrEmpty(material) || !MyTransparentMaterials.ContainsMaterial(material))
            {
                material = "ErrorMaterial";
                color = Vector4.One;
            }

            billboard.Material = material;
            billboard.BlendMaterial = blendMaterial;
            billboard.BlendTextureRatio = textureBlendRatio;

            quad.Point0.AssertIsValid();
            quad.Point1.AssertIsValid();
            quad.Point2.AssertIsValid();
            quad.Point3.AssertIsValid();


            //  Billboard vertexes
            billboard.Position0 = quad.Point0;
            billboard.Position1 = quad.Point1;
            billboard.Position2 = quad.Point2;
            billboard.Position3 = quad.Point3;

            billboard.UVOffset = uvOffset;

            //  Distance for sorting
            //  IMPORTANT: Must be calculated before we do color and alpha misting, because we need distance there
            billboard.DistanceSquared = (float)Vector3D.DistanceSquared(MyEnvironment.CameraPosition, origin);

            //  Color
            billboard.Color = color;
            billboard.Reflectivity = reflectivity;

            billboard.Near = near;
            billboard.Lowres = lowres;
            billboard.ParentID = -1;

            //  Alpha depends on distance to camera. Very close bilboards are more transparent, so player won't see billboard errors or rotating billboards
            var mat = MyTransparentMaterials.GetMaterial(billboard.Material);
            if (mat.AlphaMistingEnable)
                billboard.Color *= MathHelper.Clamp(((float)Math.Sqrt(billboard.DistanceSquared) - mat.AlphaMistingStart) / (mat.AlphaMistingEnd - mat.AlphaMistingStart), 0, 1);

            billboard.Color *= mat.Color;

            billboard.ContainedBillboards.Clear();
        }
    }

    class MyBillboardRenderer : MyImmediateRC
    {
        static HashSet<string> m_preloaded = new HashSet<string>();
        internal static void PreloadTexture(string texture)
        {
            if (!m_preloaded.Contains(texture))
            {
                m_preloaded.Add(texture);
                //MyTextureManager.GetTexture(texture);
                MyTextures.GetTexture(texture, MyTextureEnum.GUI, true);
            }
        }

        const int MaxBillboards = 32768;
        const int MaxCustomProjections = 32;

        static VertexShaderId m_vs;
        static PixelShaderId m_ps;
        static VertexShaderId m_vsLit;
        static PixelShaderId m_psLit;
        static InputLayoutId m_inputLayout;

        static IndexBufferId m_IB = IndexBufferId.NULL;
        static VertexBufferId m_VB;
        static StructuredBufferId m_SB;

        static MyBillboardData[] m_billboardData = new MyBillboardData[MaxBillboards];
        static MyVertexFormatPositionTextureH[] m_vertexData = new MyVertexFormatPositionTextureH[MaxBillboards * 4];
        static List<MyBillboardBatch> m_batches = new List<MyBillboardBatch>();
        static MyBillboard[] m_sortBuffer = new MyBillboard[MaxBillboards];
        static int m_sortedBillboardsNum;

        internal static List<MyBillboard> m_billboardsOnce = new List<MyBillboard>();
        internal static MyObjectsPoolSimple<MyBillboard> m_billboardsOncePool = new MyObjectsPoolSimple<MyBillboard>(MaxBillboards / 4);

        static Dictionary<string, MyTextureAtlasElement> m_atlasedTextures = new Dictionary<string, MyTextureAtlasElement>();

        internal unsafe static void Init()
        {
            m_vs = MyShaders.CreateVs("billboard.hlsl", "vs");
            m_ps = MyShaders.CreatePs("billboard.hlsl", "ps");
            m_vsLit = MyShaders.CreateVs("billboard.hlsl", "vs", MyShaderHelpers.FormatMacros("LIT_PARTICLE") + MyRender11.ShaderCascadesNumberHeader());
			m_psLit = MyShaders.CreatePs("billboard.hlsl", "ps", MyShaderHelpers.FormatMacros("LIT_PARTICLE") + MyRender11.ShaderCascadesNumberHeader());
            m_inputLayout = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.TEXCOORD0_H));

            //MyCallbacks.RegisterDeviceResetListener(new OnDeviceResetDelegate(OnDeviceRestart));

            InitBillboardsIndexBuffer(MaxBillboards);

            m_VB = MyHwBuffers.CreateVertexBuffer(MaxBillboards * 4, sizeof(MyVertexFormatPositionTextureH), BindFlags.VertexBuffer, ResourceUsage.Dynamic);

            var stride = sizeof(MyBillboardData);
            m_SB = MyHwBuffers.CreateStructuredBuffer(MaxBillboards, stride, true);

            MyTextureAtlas.ParseAtlasDescription("Textures\\Particles\\", "Textures\\Particles\\ParticlesAtlas.tai", m_atlasedTextures);
        }

        internal unsafe static void InitBillboardsIndexBuffer(int billboardsLimit)
        {
            if(m_IB != IndexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_IB);
            }

            uint[] indices = new uint[billboardsLimit * 6];
            for (int i = 0; i < billboardsLimit; i++)
            {
                indices[i * 6 + 0] = (uint)(i * 4 + 0);
                indices[i * 6 + 1] = (uint)(i * 4 + 1);
                indices[i * 6 + 2] = (uint)(i * 4 + 2);
                indices[i * 6 + 3] = (uint)(i * 4 + 0);
                indices[i * 6 + 4] = (uint)(i * 4 + 2);
                indices[i * 6 + 5] = (uint)(i * 4 + 3);
            }
            fixed (uint* ptr = indices)
            {
                m_IB = MyHwBuffers.CreateIndexBuffer(MaxBillboards * 6, SharpDX.DXGI.Format.R32_UInt, BindFlags.IndexBuffer, ResourceUsage.Immutable, new IntPtr(ptr));
            }
        }

        internal static void OnDeviceRestart()
        {
            InitBillboardsIndexBuffer(MaxBillboards);
        }

        static int m_sortedNum;
        static int m_sorted;
        static int m_unsorted;

        static void PreGatherList(List<MyBillboard> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var billboard = list[i];

                if (billboard.ContainedBillboards.Count == 0 && billboard.Material != null)
                {
                    var material = MyTransparentMaterials.GetMaterial(billboard.Material);
                    m_sortedNum += material.NeedSort ? 1 : 0;
                }

                for (int j = 0; j < billboard.ContainedBillboards.Count; j++)
                {
                    var material = MyTransparentMaterials.GetMaterial(billboard.ContainedBillboards[j].Material);
                    m_sortedNum += material.NeedSort ? 1 : 0;
                }

                
            }

        }

        static void GatherList(List<MyBillboard> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var billboard = list[i];

                if (billboard.ContainedBillboards.Count == 0 && billboard.Material != null)
                {
                    var material = MyTransparentMaterials.GetMaterial(billboard.Material);
                    if(material.Reflectivity > 0)
                    {

                    }
                    if (material.NeedSort)
                    {
                        m_sortBuffer[m_sorted++] = billboard;
                    }
                    else
                    {
                        m_sortBuffer[m_sortedNum + m_unsorted++] = billboard;
                    }
                }

                for (int j = 0; j < billboard.ContainedBillboards.Count; j++)
                {
                    var material = MyTransparentMaterials.GetMaterial(billboard.ContainedBillboards[j].Material);

                    if (material.NeedSort)
                    {
                        m_sortBuffer[m_sorted++] = billboard.ContainedBillboards[j];
                    }
                    else
                    {
                        m_sortBuffer[m_sortedNum + m_unsorted++] = billboard.ContainedBillboards[j];
                    }
                }
            }
        }

        static void Gather()
        {
            // counting sorted billboards
            m_batches.Clear();
            m_sortedNum = 0;

            PreGatherList(MyRenderProxy.BillboardsRead);
            PreGatherList(m_billboardsOnce);

            m_sortedBillboardsNum = m_sortedNum;

            m_unsorted = 0;
            m_sorted = 0;

            GatherList(MyRenderProxy.BillboardsRead);
            GatherList(m_billboardsOnce);
            
            Array.Sort(m_sortBuffer, 0, m_sortedNum);
            //Array.Reverse(m_sortBuffer, 0, m_sortedNum);
            //Array.Sort(m_sortBuffer, m_sortedNum, m_unsorted);

            var N = m_sorted + m_unsorted;

            var batch = new MyBillboardBatch();
            //MyAssetTexture prevTexture = null;
            var prevTexId = TexId.NULL;
            int currentOffset = 0;

            if(N > 0)
            {
                var material = MyTransparentMaterials.GetMaterial(m_sortBuffer[0].Material);

                if(material.UseAtlas)
                {
                    var item = m_atlasedTextures[material.Texture];
                    prevTexId = item.TextureId;
                }
                else
                {
                    PreloadTexture(material.Texture);
                    //prevTexture = MyTextureManager.GetTextureFast(material.Texture);
                    prevTexId = MyTextures.GetTexture(material.Texture, MyTextureEnum.GUI, true);
                }
            }

            TexId batchTexId = TexId.NULL;
            MyTransparentMaterial prevMaterial = null;

            for(int i=0; i<N; i++)
            {
                var billboard = m_sortBuffer[i];
                var material = MyTransparentMaterials.GetMaterial(billboard.Material);

                var billboardData = new MyBillboardData();

                billboardData.CustomProjectionID = billboard.CustomViewProjection;
                billboardData.Color = billboard.Color;
                if (material.UseAtlas)
                {
                    var atlasItem = m_atlasedTextures[material.Texture];
                    //billboardData.UvModifiers = new HalfVector4(atlasItem.UvOffsetScale);
                    batchTexId = atlasItem.TextureId;
                }
                else
                {
                    batchTexId = MyTextures.GetTexture(material.Texture, MyTextureEnum.GUI, true);
                }

                billboardData.Reflective = billboard.Reflectivity;
                Vector3D pos0 = billboard.Position0;
                Vector3D pos1 = billboard.Position1;
                Vector3D pos2 = billboard.Position2;
                Vector3D pos3 = billboard.Position3;

                if (billboard.ParentID != -1)
                {
                    if (MyIDTracker<MyActor>.FindByID((uint)billboard.ParentID) != null)
                    {
                        var matrix = MyIDTracker<MyActor>.FindByID((uint)billboard.ParentID).WorldMatrix;
                        Vector3D.Transform(ref pos0, ref matrix, out pos0);
                        Vector3D.Transform(ref pos1, ref matrix, out pos1);
                        Vector3D.Transform(ref pos2, ref matrix, out pos2);
                        Vector3D.Transform(ref pos3, ref matrix, out pos3);
                    }
                }

                if (billboard.CustomViewProjection != -1)
                {
                    var billboardViewProjection = MyRenderProxy.BillboardsViewProjectionRead[billboard.CustomViewProjection];

                    //pos0 -= MyEnvironment.CameraPosition;
                    //pos1 -= MyEnvironment.CameraPosition;
                    //pos2 -= MyEnvironment.CameraPosition;
                    //pos3 -= MyEnvironment.CameraPosition;
                }
                else
                {
                    pos0 -= MyEnvironment.CameraPosition;
                    pos1 -= MyEnvironment.CameraPosition;
                    pos2 -= MyEnvironment.CameraPosition;
                    pos3 -= MyEnvironment.CameraPosition;
                }

                var normal = Vector3.Cross(pos1 - pos0, pos2 - pos0);
                normal.Normalize();

                billboardData.Normal = normal;

                m_vertexData[i * 4 + 0].Position = pos0;
                m_vertexData[i * 4 + 1].Position = pos1;
                m_vertexData[i * 4 + 2].Position = pos2;
                m_vertexData[i * 4 + 3].Position = pos3;

                var uv0 = new Vector2(material.UVOffset.X + billboard.UVOffset.X, material.UVOffset.Y + billboard.UVOffset.Y);
                var uv1 = new Vector2(material.UVOffset.X + material.UVSize.X + billboard.UVOffset.X, material.UVOffset.Y + billboard.UVOffset.Y);
                var uv2 = new Vector2(material.UVOffset.X + material.UVSize.X + billboard.UVOffset.X, material.UVOffset.Y + material.UVSize.Y + billboard.UVOffset.Y);
                var uv3 = new Vector2(material.UVOffset.X + billboard.UVOffset.X, material.UVOffset.Y + material.UVSize.Y + billboard.UVOffset.Y);

                if (material.UseAtlas)
                {
                    var atlasItem = m_atlasedTextures[material.Texture];

                    uv0 = uv0 * new Vector2(atlasItem.UvOffsetScale.Z, atlasItem.UvOffsetScale.W) + new Vector2(atlasItem.UvOffsetScale.X, atlasItem.UvOffsetScale.Y);
                    uv1 = uv1 * new Vector2(atlasItem.UvOffsetScale.Z, atlasItem.UvOffsetScale.W) + new Vector2(atlasItem.UvOffsetScale.X, atlasItem.UvOffsetScale.Y);
                    uv2 = uv2 * new Vector2(atlasItem.UvOffsetScale.Z, atlasItem.UvOffsetScale.W) + new Vector2(atlasItem.UvOffsetScale.X, atlasItem.UvOffsetScale.Y);
                    uv3 = uv3 * new Vector2(atlasItem.UvOffsetScale.Z, atlasItem.UvOffsetScale.W) + new Vector2(atlasItem.UvOffsetScale.X, atlasItem.UvOffsetScale.Y);
                }

                m_vertexData[i * 4 + 0].Texcoord = new HalfVector2(uv0);
                m_vertexData[i * 4 + 1].Texcoord = new HalfVector2(uv1);
                m_vertexData[i * 4 + 2].Texcoord = new HalfVector2(uv2);
                m_vertexData[i * 4 + 3].Texcoord = new HalfVector2(uv3);

                pos0.AssertIsValid();
                pos1.AssertIsValid();
                pos2.AssertIsValid();
                pos3.AssertIsValid();


                MyTriangleBillboard triBillboard = billboard as MyTriangleBillboard;
                if(triBillboard != null)
                {
                    m_vertexData[i * 4 + 3].Position = pos2; // second triangle will die in rasterizer

                    m_vertexData[i * 4 + 0].Texcoord = new HalfVector2(triBillboard.UV0); 
                    m_vertexData[i * 4 + 1].Texcoord = new HalfVector2(triBillboard.UV1);
                    m_vertexData[i * 4 + 2].Texcoord = new HalfVector2(triBillboard.UV2);

                    billboardData.Normal = triBillboard.Normal0; // pew pew pew :O
                }

                m_billboardData[i] = billboardData;

                bool closeBatch = (batchTexId != prevTexId) || ((i == m_sortedNum) && (i > 0));

                if(closeBatch)
                {
                    batch = new MyBillboardBatch();

                    batch.Offset = currentOffset;
                    batch.Num = i - currentOffset;
                    batch.Texture = prevTexId != TexId.NULL ? MyTextures.Views[prevTexId.Index] : null;

                    batch.Lit = prevMaterial.CanBeAffectedByOtherLights;

                    m_batches.Add(batch);
                    currentOffset = i;
                }

                prevTexId = batchTexId;
                prevMaterial = material;
            }

            if(N > 0)
            {
                batch = new MyBillboardBatch();
                batch.Offset = currentOffset;
                batch.Num = N - currentOffset;
                batch.Texture = prevTexId != TexId.NULL ? MyTextures.GetView(prevTexId) : null;

                batch.Lit = prevMaterial.CanBeAffectedByOtherLights;

                m_batches.Add(batch);
            }
        }

        static unsafe void TransferData()
        {
            var mapping = MyMapping.MapDiscard(RC.Context, MyCommon.GetObjectCB(sizeof(Matrix) * MaxCustomProjections));
            for (int i = 0; i < MyRenderProxy.BillboardsViewProjectionRead.Count; i++)
            {
                var view = MyRenderProxy.BillboardsViewProjectionRead[i].View;
                var projection = MyRenderProxy.BillboardsViewProjectionRead[i].Projection;

                var scaleX = MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.Width / (float)MyRender11.ViewportResolution.X;
                var scaleY = MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.Height / (float)MyRender11.ViewportResolution.Y;
                var offsetX = MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.OffsetX / (float)MyRender11.ViewportResolution.X;
                var offsetY = (MyRender11.ViewportResolution.Y - MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.OffsetY - MyRenderProxy.BillboardsViewProjectionRead[i].Viewport.Height)
                    / (float)MyRender11.ViewportResolution.Y;

                var viewportTransformation = new Matrix(
                    scaleX, 0, 0, 0,
                    0, scaleY, 0, 0,
                    0, 0, 1, 0,
                    offsetX, offsetY, 0, 1
                    );

                mapping.stream.Write(Matrix.Transpose(view * projection * viewportTransformation));
            }
            for (int i = MyRenderProxy.BillboardsViewProjectionRead.Count; i < MaxCustomProjections; i++)
            {
                mapping.stream.Write(Matrix.Identity);
            }
            mapping.Unmap();

            mapping = MyMapping.MapDiscard(RC.Context, m_VB.Buffer);
            fixed(void * ptr = m_vertexData)
            {
                mapping.stream.Write(new IntPtr(ptr), 0, (sizeof(MyVertexFormatPosition) * MaxBillboards * 4));
            }
            mapping.Unmap();

            mapping = MyMapping.MapDiscard(RC.Context, m_SB.Buffer);
            fixed(void *ptr = m_billboardData)
            {
                mapping.stream.Write(new IntPtr(ptr), 0, (sizeof(MyBillboardData) * MaxBillboards));
            }
            mapping.Unmap();
        }

        internal unsafe static void Render(MyBindableResource dst, MyBindableResource depth, MyBindableResource depthRead)
        {
            MyRender11.GetRenderProfiler().StartProfilingBlock("Gather");
            Gather();
            MyRender11.GetRenderProfiler().EndProfilingBlock();


            MyRender11.GetRenderProfiler().StartProfilingBlock("Draw");
            TransferData();

            RC.SetupScreenViewport();
            RC.BindDepthRT(depth, DepthStencilAccess.ReadOnly, dst);
            RC.SetBS(MyRender11.BlendAlphaPremult);
            RC.SetRS(MyRender11.m_nocullRasterizerState);
            RC.BindRawSRV(104, m_SB.Srv);
            RC.BindSRV(1, depthRead);
            RC.SetCB(2, MyCommon.GetObjectCB(sizeof(Matrix) * MaxCustomProjections));
            RC.SetDS(MyDepthStencilState.DefaultDepthState);

            RC.SetCB(4, MyShadows.m_csmConstants);
            RC.Context.VertexShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);
            RC.Context.VertexShader.SetShaderResource(MyCommon.CASCADES_SM_SLOT, MyShadows.m_cascadeShadowmapArray.ShaderView);
            RC.Context.VertexShader.SetSamplers(0, MyRender11.StandardSamplers);


            RC.Context.VertexShader.SetShaderResource(MyCommon.SKYBOX_IBL_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.DaySkyboxPrefiltered, MyTextureEnum.CUBEMAP, true)));
            RC.Context.PixelShader.SetShaderResource(MyCommon.SKYBOX2_IBL_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.NightSkyboxPrefiltered, MyTextureEnum.CUBEMAP, true)));

            RC.SetVB(0, m_VB.Buffer, m_VB.Stride);
            RC.SetIB(m_IB.Buffer, m_IB.Format);

            RC.SetIL(m_inputLayout);

            for(int i=0; i<m_batches.Count; i++)
            {
                // first part of batches contain sorted billboards and they read depth
                // second part of batches contain unsorted billboards and they ignore depth
                // switch here:
                if (m_batches[i].Offset == m_sortedBillboardsNum)
                {
                    //RC.BindDepthRT(null, DepthStencilAccess.ReadOnly, dst);
                }

                if(m_batches[i].Lit)
                {
                    RC.SetVS(m_vsLit);
                    RC.SetPS(m_psLit);
                }
                else
                {
                    RC.SetVS(m_vs);
                    RC.SetPS(m_ps);
                }

                RC.BindRawSRV(0, m_batches[i].Texture);
                RC.Context.DrawIndexed(m_batches[i].Num * 6, m_batches[i].Offset * 6, 0);
            }

            RC.SetRS(null);
            m_batches.Clear();
            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        internal static void OnFrameStart()
        {
            m_billboardsOncePool.ClearAllAllocated();
            m_billboardsOnce.Clear();
        }
    }

    class MyBlendTargets : MyScreenPass
    {
        static PixelShaderId m_ps = PixelShaderId.NULL;
        static PixelShaderId m_psPixelStencil = PixelShaderId.NULL;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("postprocess.hlsl", "copy");
            m_psPixelStencil = MyShaders.CreatePs("postprocess.hlsl", "copyWithStencilTest");
        }

        internal static void Run(MyBindableResource dst, MyBindableResource src, BlendState bs = null)
        {
            //RC.SetBS(MyRender.BlendStateAdditive);
            RC.SetBS(bs);
            RC.SetRS(null);
            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, dst);
            RC.BindSRV(0, src);
            RC.SetPS(m_ps);

            DrawFullscreenQuad();
            RC.SetBS(null);
        }

        internal static void RunWithStencil(MyBindableResource dst, MyBindableResource src, BlendState bs = null)
        {
            RC.SetDS(MyDepthStencilState.TestOutlineMeshStencil, 0x40);
            RC.SetBS(bs);
            RC.SetRS(null);
            RC.BindDepthRT(MyGBuffer.Main.DepthStencil, DepthStencilAccess.ReadOnly, dst);
            RC.BindSRV(0, src);
            RC.SetPS(m_ps);

            DrawFullscreenQuad();
            RC.SetBS(null);
        }

        internal static void RunWithPixelStencilTest(MyBindableResource dst, MyBindableResource src, BlendState bs = null)
        {
            RC.SetDS(null);
            RC.SetBS(bs);
            RC.SetRS(null);
            RC.BindDepthRT(null, DepthStencilAccess.ReadOnly, dst);
            RC.BindSRV(0, src);
            RC.BindSRV(1, MyGBuffer.Main.DepthStencil.Stencil);
            RC.SetPS(m_psPixelStencil);

            DrawFullscreenQuad();
            RC.SetBS(null);
        }
    }
}
