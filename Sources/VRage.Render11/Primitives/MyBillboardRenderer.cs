using System;
using System.Collections.Generic;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageMath;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using Vector4 = VRageMath.Vector4;
using VRageRender.Vertex;
using VRageMath.PackedVector;
using VRage.Utils;
using VRage.Generics;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Render11.Tools;

namespace VRageRender
{
    struct MyBillboardRendererBatch
    {
        internal int Offset;
        internal int Num;
        internal ISrvBindable Texture;
        internal bool Lit;
        internal bool AlphaCutout;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    struct MyBillboardData
    {
        internal Vector4 Color;

        internal int CustomProjectionID;
        internal float Reflective;
        internal float AlphaSaturation;
        internal float AlphaCutout;

        internal Vector3 Normal;
        internal float SoftParticleDistanceScale;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    struct MyBillboardVertexData
    {
        public MyVertexFormatPositionTextureH V0;
        public MyVertexFormatPositionTextureH V1;
        public MyVertexFormatPositionTextureH V2;
        public MyVertexFormatPositionTextureH V3;
    }

    struct MyBillboardDataArray
    {
        public MyBillboardData[] Data;
        public MyBillboardVertexData[] Vertex;

        public MyBillboardDataArray(int size)
        {
            Data = new MyBillboardData[size];
            Vertex = new MyBillboardVertexData[size];
        }

        public void Resize(int size)
        {
            if (size == Length)
                return;

            Array.Resize(ref Data, size);
            Array.Resize(ref Vertex, size);
        }

        public int Length
        {
            get { return Data.Length; }
        }
    }

    internal static class MyBillboardsHelper
    {
        internal static void AddPointBillboard(string material,
           Vector4 color, Vector3D origin, float radius, float angle, int customViewProjection = -1)
        {
            if (!MyRender11.DebugOverrides.BillboardsDynamic)
                return;
            Debug.Assert(material != null);

            origin.AssertIsValid();
            angle.AssertIsValid();

            MyQuadD quad;
            if (MyUtils.GetBillboardQuadAdvancedRotated(out quad, origin, radius, angle, MyRender11.Environment.Matrices.CameraPosition) != false)
            {
                MyBillboard billboard = MyBillboardRenderer.AddBillboardOnce();
                if (billboard == null)
                    return;

                billboard.BlendType = MyBillboard.BlenType.Standard;
                billboard.CustomViewProjection = customViewProjection;
                CreateBillboard(billboard, ref quad, material, ref color, ref origin);
            }
        }

        internal static void AddBillboardOriented(string material, Vector4 color, Vector3D origin, Vector3 leftVector, Vector3 upVector, float radius, 
            MyBillboard.BlenType blendType = MyBillboard.BlenType.Standard, float softParticleDistanceScale = 1.0f, int customViewProjection = -1)
        {
            if (!MyRender11.DebugOverrides.BillboardsDynamic)
                return;

            Debug.Assert(material != null);

            origin.AssertIsValid();
            leftVector.AssertIsValid();
            upVector.AssertIsValid();
            radius.AssertIsValid();
            MyDebug.AssertDebug(radius > 0);

            MyBillboard billboard = MyBillboardRenderer.AddBillboardOnce();
            if (billboard == null)
                return;

            billboard.CustomViewProjection = customViewProjection;
            billboard.BlendType = blendType;

            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref origin, radius, ref leftVector, ref upVector);

            CreateBillboard(billboard, ref quad, material, ref color, ref origin, softParticleDistanceScale);
        }

        private static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material, ref Vector4 color, ref Vector3D origin,
            float softParticleDistanceScale = 1.0f, Vector2 uvOffset = new Vector2(), float reflectivity = 0)
        {
            Debug.Assert(material != null);

            if (string.IsNullOrEmpty(material) || !MyTransparentMaterials.ContainsMaterial(material))
            {
                material = "ErrorMaterial";
                color = Vector4.One;
            }

            billboard.Material = material;

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
            billboard.UVSize = Vector2.One;

            //  Distance for sorting
            //  IMPORTANT: Must be calculated before we do color and alpha misting, because we need distance there
            billboard.DistanceSquared = (float)Vector3D.DistanceSquared(MyRender11.Environment.Matrices.CameraPosition, origin);

            //  Color
            billboard.Color = color;
            billboard.Reflectivity = reflectivity;
            billboard.ColorIntensity = 1;

            billboard.ParentID = -1;

            //  Alpha depends on distance to camera. Very close bilboards are more transparent, so player won't see billboard errors or rotating billboards
            var mat = MyTransparentMaterials.GetMaterial(billboard.Material);
            if (mat.AlphaMistingEnable)
                billboard.Color *= MathHelper.Clamp(((float)Math.Sqrt(billboard.DistanceSquared) - mat.AlphaMistingStart) / (mat.AlphaMistingEnd - mat.AlphaMistingStart), 0, 1);

            billboard.Color *= mat.Color;
            billboard.SoftParticleDistanceScale = softParticleDistanceScale;
        }
    }

    class MyBillboardRenderer : MyImmediateRC
    {
        const int BILLBOARDS_INIT_SIZE = 8192;
        const int MAX_BILLBOARDS_SIZE = 32768;
        const int MAX_CUSTOM_PROJECTIONS_SIZE = 32;

        static IConstantBuffer m_cbCustomProjections;

        static VertexShaderId m_vs;
        static VertexShaderId m_vsLit;

        static InputLayoutId m_inputLayout;

        static IIndexBuffer m_IB;
        static IVertexBuffer m_VB;
        static ISrvBuffer m_SB;

        private const int BUCKETS_COUNT = 4;
        private static readonly int[] m_bucketCounts = new int[BUCKETS_COUNT];
        private static readonly int[] m_bucketIndices = new int[BUCKETS_COUNT];
        private static int m_billboardCountSafe;

        private struct MyBucketBatches
        {
            public int StartIndex;
            public int Count;
        }
        private static readonly MyBucketBatches[] m_bucketBatches = new MyBucketBatches[BUCKETS_COUNT];

        static readonly Dictionary<string, ISrvBindable> m_fileTextures = new Dictionary<string, ISrvBindable>();
        static readonly List<MyBillboardRendererBatch> m_batches = new List<MyBillboardRendererBatch>();
        static MyBillboard[] m_tempBuffer = new MyBillboard[BILLBOARDS_INIT_SIZE];

        static MyBillboardDataArray m_arrayDataBillboards = new MyBillboardDataArray(BILLBOARDS_INIT_SIZE);

        private static MyPassStats m_stats = new MyPassStats();

        static readonly List<MyBillboard> m_billboardsOnce = new List<MyBillboard>();
        static readonly MyObjectsPoolSimple<MyBillboard> m_billboardsOncePool = new MyObjectsPoolSimple<MyBillboard>(BILLBOARDS_INIT_SIZE / 4);
        private static MyTextureAtlas m_atlas;
        private static int m_lastBatchOffset;

        [Flags]
        enum PixelShaderFlags
        {
            OIT = 1 << 0,
            LIT_PARTICLE = 1 << 1,
            ALPHA_CUTOUT = 1 << 2,
            SOFT_PARTICLE = 1 << 3,
            DEBUG_UNIFORM_ACCUM = 1 << 4,

            Max = (1 << 5) - 1
        }
        static readonly PixelShaderId[] m_psBundle = new PixelShaderId[(int)PixelShaderFlags.Max];

        private static void GeneratePS()
        {
            var macros = new List<ShaderMacro> ();
            for (int i = 0; i < (int)PixelShaderFlags.Max; i++)
            {
                macros.Clear();
                int j = 1;
                while (j < (int)PixelShaderFlags.Max)
                {
                    if ((i & j) > 0)
                    {
                        var flag = (PixelShaderFlags)j;
                        var name = flag.ToString();
                        macros.Add(new ShaderMacro(name, null));
                    }
                    j <<= 1;
                }
                m_psBundle[i] = MyShaders.CreatePs("Transparent/Billboards.hlsl", macros.ToArray());
            }
        }

        internal static unsafe void Init()
        {
            m_cbCustomProjections = MyManagers.Buffers.CreateConstantBuffer("BilloardCustomProjections", sizeof(Matrix) * MAX_CUSTOM_PROJECTIONS_SIZE, usage: ResourceUsage.Dynamic);

            GeneratePS();
            m_vs = MyShaders.CreateVs("Transparent/Billboards.hlsl");
            m_vsLit = MyShaders.CreateVs("Transparent/Billboards.hlsl", new[] { new ShaderMacro("LIT_PARTICLE", null) });

            m_inputLayout = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.TEXCOORD0_H));

            InitBillboardsIndexBuffer();

            m_VB = MyManagers.Buffers.CreateVertexBuffer("MyBillboardRenderer", MAX_BILLBOARDS_SIZE * 4, sizeof(MyVertexFormatPositionTextureH), usage: ResourceUsage.Dynamic);

            var stride = sizeof(MyBillboardData);
            m_SB = MyManagers.Buffers.CreateSrv(
                "MyBillboardRenderer", MAX_BILLBOARDS_SIZE, stride,
                usage: ResourceUsage.Dynamic);
            m_atlas = new MyTextureAtlas("Textures\\Particles\\", "Textures\\Particles\\ParticlesAtlas.tai");
        }

        internal static void OnFrameStart()
        {
            m_billboardsOncePool.ClearAllAllocated();
            m_billboardsOnce.Clear();
        }

        static unsafe void InitBillboardsIndexBuffer()
        {
            if (m_IB != null)
            {
                MyManagers.Buffers.Dispose(m_IB);
            }

            uint[] indices = new uint[MAX_BILLBOARDS_SIZE * 6];
            for (int i = 0; i < MAX_BILLBOARDS_SIZE; i++)
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
                m_IB = MyManagers.Buffers.CreateIndexBuffer(
                    "MyBillboardRenderer", MAX_BILLBOARDS_SIZE * 6, new IntPtr(ptr),
                    MyIndexBufferFormat.UInt, ResourceUsage.Immutable);
            }
        }

        internal static void OnDeviceReset()
        {
            InitBillboardsIndexBuffer();
            m_fileTextures.Clear();
        }

        internal static void OnSessionEnd()
        {
            m_fileTextures.Clear();
        }

        private static void PreGatherList(List<MyBillboard> list)
        {
            foreach (var billboard in list)
                m_bucketCounts[GetBillboardBucket(billboard)]++;
        }
        
        private static int GetBillboardBucket(MyBillboard billboard)
        {
            return (int) billboard.BlendType;
        }

        static void GatherList(List<MyBillboard> list)
        {
            foreach (var billboard in list)
            {
                int bucketIndex = GetBillboardBucket(billboard);
                int bufferIndex = m_bucketIndices[bucketIndex]++;
                m_tempBuffer[bufferIndex] = billboard;
            }
        }

        static bool GatherInternal()
        {
            m_batches.Clear();

            // counting sorted billboards
            ClearBucketCounts();
            PreGatherList(MyRenderProxy.BillboardsRead);
            PreGatherList(m_billboardsOnce);

            int billboardCount = 0;
            for (int j = 0; j < BUCKETS_COUNT; j++)
                billboardCount += m_bucketCounts[j];
            if (billboardCount == 0)
                return false;

            m_billboardCountSafe = billboardCount > MAX_BILLBOARDS_SIZE ? MAX_BILLBOARDS_SIZE : billboardCount;

            InitGatherList(billboardCount, m_billboardCountSafe);
            InitBucketIndices();

            GatherList(MyRenderProxy.BillboardsRead);
            GatherList(m_billboardsOnce);

            InitBucketIndices();

            int i;
            for (i = 0; i < BUCKETS_COUNT; i++)
                Array.Sort(m_tempBuffer, m_bucketIndices[i], m_bucketCounts[i]);

            bool resetBindings = false;
            int currentOffset = 0;
            ISrvBindable prevTex = null;
            MyTransparentMaterial prevMaterial = null;
            for (i = 0; i < m_billboardCountSafe; i++)
            {
                MyBillboard billboard = m_tempBuffer[i];
                MyTransparentMaterial material = MyTransparentMaterials.GetMaterial(billboard.Material);
                ISrvBindable batchTex = null;
                if (material.UseAtlas)
                {
                    var atlasItem = m_atlas.FindElement(material.Texture);
                    batchTex = atlasItem.Texture;
                }
                else
                {
                    MyFileTextureManager texManager = MyManagers.FileTextures;
                    switch (material.TextureType)
                    {
                        case MyTransparentMaterialTextureType.FileTexture:
                            if (material.Texture == null || !m_fileTextures.TryGetValue(material.Texture, out batchTex))
                            {
                                batchTex = texManager.GetTexture(material.Texture, MyFileTextureEnum.GUI, true);
                                if (material.Texture != null)
                                {
                                    m_fileTextures.Add(material.Texture, batchTex);
                                }
                                else
                                {
                                    MyRenderProxy.Fail("Material: " + material.Name + " is missing a texture.");
                                }
                            }

                            break;
                        case MyTransparentMaterialTextureType.RenderTarget:
                            batchTex = MyRender11.DrawSpritesOffscreen(material.Name, material.TargetSize.X, material.TargetSize.Y);
                            resetBindings = true;
                            break;
                        default:
                            throw new Exception();
                    }
                }

                bool boundary = IsBucketBoundary(i);
                bool closeBatch = i > 0 && (batchTex != prevTex || boundary);
                if (closeBatch)
                {
                    AddBatch(i, currentOffset, prevTex, prevMaterial);
                    currentOffset = i;
                }


                var billboardData = new MyBillboardData();
                var billboardVertices = new MyBillboardVertexData();

                billboardData.CustomProjectionID = billboard.CustomViewProjection;
                billboardData.Color = billboard.Color;
                billboardData.Color.X *= billboard.ColorIntensity;
                billboardData.Color.Y *= billboard.ColorIntensity;
                billboardData.Color.Z *= billboard.ColorIntensity;
                billboardData.AlphaCutout = billboard.AlphaCutout;
                billboardData.AlphaSaturation = material.AlphaSaturation;
                billboardData.SoftParticleDistanceScale = billboard.SoftParticleDistanceScale * material.SoftParticleDistanceScale;

                billboardData.Reflective = billboard.Reflectivity;
                Vector3D pos0 = billboard.Position0;
                Vector3D pos1 = billboard.Position1;
                Vector3D pos2 = billboard.Position2;
                Vector3D pos3 = billboard.Position3;

                if (billboard.ParentID != -1)
                {
                    var parent = MyIDTracker<MyActor>.FindByID((uint) billboard.ParentID);
                    if (parent != null)
                    {
                        var matrix = parent.WorldMatrix;
                        Vector3D.Transform(ref pos0, ref matrix, out pos0);
                        Vector3D.Transform(ref pos1, ref matrix, out pos1);
                        Vector3D.Transform(ref pos2, ref matrix, out pos2);
                        Vector3D.Transform(ref pos3, ref matrix, out pos3);
                    }
                }

                MyEnvironmentMatrices envMatrices = MyRender11.Environment.Matrices;
                if (MyStereoRender.Enable)
                {
                    if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                        envMatrices = MyStereoRender.EnvMatricesLeftEye;
                    else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                        envMatrices = MyStereoRender.EnvMatricesRightEye;
                }

                if (billboard.CustomViewProjection == -1)
                {
                    pos0 -= envMatrices.CameraPosition;
                    pos1 -= envMatrices.CameraPosition;
                    pos2 -= envMatrices.CameraPosition;
                    pos3 -= envMatrices.CameraPosition;
                }

                var normal = Vector3D.Cross(pos1 - pos0, pos2 - pos0);
                normal.Normalize();

                billboardData.Normal = normal;

                billboardVertices.V0.Position = pos0;
                billboardVertices.V1.Position = pos1;
                billboardVertices.V2.Position = pos2;
                billboardVertices.V3.Position = pos3;

                var uv0 = new Vector2(material.UVOffset.X + billboard.UVOffset.X, material.UVOffset.Y + billboard.UVOffset.Y);
                var uv1 = new Vector2(material.UVOffset.X + material.UVSize.X * billboard.UVSize.X + billboard.UVOffset.X, material.UVOffset.Y + billboard.UVOffset.Y);
                var uv2 = new Vector2(material.UVOffset.X + material.UVSize.X * billboard.UVSize.X + billboard.UVOffset.X, material.UVOffset.Y + billboard.UVOffset.Y + material.UVSize.Y * billboard.UVSize.Y);
                var uv3 = new Vector2(material.UVOffset.X + billboard.UVOffset.X, material.UVOffset.Y + billboard.UVOffset.Y + material.UVSize.Y * billboard.UVSize.Y);

                if (material.UseAtlas)
                {
                    var atlasItem = m_atlas.FindElement(material.Texture);

                    uv0 = uv0 * new Vector2(atlasItem.UvOffsetScale.Z, atlasItem.UvOffsetScale.W) + new Vector2(atlasItem.UvOffsetScale.X, atlasItem.UvOffsetScale.Y);
                    uv1 = uv1 * new Vector2(atlasItem.UvOffsetScale.Z, atlasItem.UvOffsetScale.W) + new Vector2(atlasItem.UvOffsetScale.X, atlasItem.UvOffsetScale.Y);
                    uv2 = uv2 * new Vector2(atlasItem.UvOffsetScale.Z, atlasItem.UvOffsetScale.W) + new Vector2(atlasItem.UvOffsetScale.X, atlasItem.UvOffsetScale.Y);
                    uv3 = uv3 * new Vector2(atlasItem.UvOffsetScale.Z, atlasItem.UvOffsetScale.W) + new Vector2(atlasItem.UvOffsetScale.X, atlasItem.UvOffsetScale.Y);
                }

                billboardVertices.V0.Texcoord = new HalfVector2(uv0);
                billboardVertices.V1.Texcoord = new HalfVector2(uv1);
                billboardVertices.V2.Texcoord = new HalfVector2(uv2);
                billboardVertices.V3.Texcoord = new HalfVector2(uv3);

                pos0.AssertIsValid();
                pos1.AssertIsValid();
                pos2.AssertIsValid();
                pos3.AssertIsValid();


                MyTriangleBillboard triBillboard = billboard as MyTriangleBillboard;
                if (triBillboard != null)
                {
                    billboardVertices.V3.Position = pos2; // second triangle will die in rasterizer

                    billboardVertices.V0.Texcoord = new HalfVector2(triBillboard.UV0);
                    billboardVertices.V1.Texcoord = new HalfVector2(triBillboard.UV1);
                    billboardVertices.V2.Texcoord = new HalfVector2(triBillboard.UV2);

                    billboardData.Normal = triBillboard.Normal0;
                }

                m_arrayDataBillboards.Data[i] = billboardData;
                m_arrayDataBillboards.Vertex[i] = billboardVertices;

                prevTex = batchTex;
                prevMaterial = material;
            }
            AddBatch(m_billboardCountSafe, currentOffset, prevTex, prevMaterial);

            TransferDataCustomProjections();
            TransferDataBillboards(0, m_billboardCountSafe, ref m_arrayDataBillboards);

            return resetBindings;
        }

        private static int GetBucketIndex(int i)
        {
            for (int k = 0; k < (BUCKETS_COUNT - 1); k++)
                if (i < m_bucketIndices[k + 1])
                    return k;
            return -1;
        }

        private static bool IsBucketBoundary(int i)
        {
            for (int k = 0; k < (BUCKETS_COUNT - 1); k++)
                if (i == m_bucketIndices[k + 1])
                    return true;
            return false;
        }

        private static void ClearBucketCounts()
        {
            for (int i = 0; i < BUCKETS_COUNT; i++)
                m_bucketCounts[i] = 0;
        }

        private static void AddBatch(int counter, int offset, ISrvBindable prevTexture, MyTransparentMaterial prevMaterial)
        {
            MyBillboardRendererBatch batch = new MyBillboardRendererBatch();

            batch.Offset = offset;
            batch.Num = counter - offset;
            batch.Texture = prevTexture;

            batch.Lit = prevMaterial.CanBeAffectedByOtherLights;
            batch.AlphaCutout = prevMaterial.AlphaCutout;

            m_batches.Add(batch);

            bool boundary = counter == m_billboardCountSafe || IsBucketBoundary(counter);
            if (boundary)
            {
                int currentBatchOffset = m_batches.Count;
                m_bucketBatches[GetBucketIndex(counter - 1)] = new MyBucketBatches
                {
                    StartIndex = m_lastBatchOffset,
                    Count = currentBatchOffset - m_lastBatchOffset
                };
                m_lastBatchOffset = currentBatchOffset;
            }

        }

        private static void TransferDataCustomProjections()
        {
            var mapping = MyMapping.MapDiscard(RC, m_cbCustomProjections);
            for (int i = 0; i < MyRenderProxy.BillboardsViewProjectionRead.Count; i++)
            {
                MyBillboardViewProjection viewprojection = MyRenderProxy.BillboardsViewProjectionRead[i];

                var scaleX = viewprojection.Viewport.Width / (float)MyRender11.ViewportResolution.X;
                var scaleY = viewprojection.Viewport.Height / (float)MyRender11.ViewportResolution.Y;
                var offsetX = viewprojection.Viewport.OffsetX / (float)MyRender11.ViewportResolution.X;
                var offsetY = (MyRender11.ViewportResolution.Y - viewprojection.Viewport.OffsetY - viewprojection.Viewport.Height)
                    / (float)MyRender11.ViewportResolution.Y;

                var viewportTransformation = new Matrix(
                    scaleX, 0, 0, 0,
                    0, scaleY, 0, 0,
                    0, 0, 1, 0,
                    offsetX, offsetY, 0, 1
                    );

                var transpose = Matrix.Transpose(viewprojection.ViewAtZero * viewprojection.Projection * viewportTransformation);
                mapping.WriteAndPosition(ref transpose);
            }

            for (int i = MyRenderProxy.BillboardsViewProjectionRead.Count; i < MAX_CUSTOM_PROJECTIONS_SIZE; i++)
                mapping.WriteAndPosition(ref Matrix.Identity);

            mapping.Unmap();
        }

        private static void TransferDataBillboards(int offset, int billboardCount, ref MyBillboardDataArray array)
        {
            var mapping = MyMapping.MapDiscard(RC, m_SB);
            mapping.WriteAndPosition(array.Data, billboardCount, offset);
            mapping.Unmap();

            mapping = MyMapping.MapDiscard(RC, m_VB);
            mapping.WriteAndPosition(array.Vertex, billboardCount, offset);
            mapping.Unmap();
        }

        /// <param name="handleWindow">Handle function for window billboards: decides if
        /// keeping it in separate storage list</param>
        /// <returns>True if the transparent geometry bindings must be reset</returns>
        public static bool Gather()
        {
            m_stats.Clear();

            MyRender11.GetRenderProfiler().StartProfilingBlock("Gather");
            bool resetBindings = GatherInternal();
            MyRender11.GetRenderProfiler().EndProfilingBlock();
            return resetBindings;
        }

        public static void RenderAdditveBottom(ISrvBindable depthRead)
        {
            if (MyRender11.DebugOverrides.Clouds)
            {
                RC.SetBlendState(MyBlendStateManager.BlendAdditive);
                RC.SetRtvs(MyGBuffer.Main.ResolvedDepthStencil, VRage.Render11.RenderContext.MyDepthStencilAccess.ReadOnly, MyGBuffer.Main.LBuffer);

                Render(depthRead, m_bucketBatches[(int) MyBillboard.BlenType.AdditiveBottom], false);
            }
        }

        public static void RenderAdditveTop(ISrvBindable depthRead)
        {
            RC.SetBlendState(MyBlendStateManager.BlendAdditive);
            RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            RC.SetRtv(MyGBuffer.Main.LBuffer);

            Render(null, m_bucketBatches[(int)MyBillboard.BlenType.AdditiveTop], false);
        }

        internal static void RenderStandard(ISrvBindable depthRead)
        {
            Render(depthRead, m_bucketBatches[(int)MyBillboard.BlenType.Standard], true);
        }

        private static void Render(ISrvBindable depthRead, MyBucketBatches bucketBatches, bool oit)
        {
            if (!MyRender11.DebugOverrides.BillboardsDynamic && !MyRender11.DebugOverrides.BillboardsStatic)
                return;

            if (m_batches.Count == 0)
                return;

            MyRender11.GetRenderProfiler().StartProfilingBlock("Draw");

            RC.PixelShader.SetSrv(1, depthRead);
            BindResourcesCommon();

            PixelShaderFlags basePsFlags = (MyRender11.Settings.DisplayTransparencyHeatMap ? PixelShaderFlags.DEBUG_UNIFORM_ACCUM : 0) |
                ((MyRender11.DebugOverrides.OIT && oit) ? PixelShaderFlags.OIT : 0) |
                (depthRead != null ? PixelShaderFlags.SOFT_PARTICLE : 0);
            for (int i = bucketBatches.StartIndex; i < bucketBatches.StartIndex + bucketBatches.Count; i++)
            {
                var ps = m_psBundle[(int)(basePsFlags |
                    (m_batches[i].Lit ? PixelShaderFlags.LIT_PARTICLE : 0) |
                    (m_batches[i].AlphaCutout ? PixelShaderFlags.ALPHA_CUTOUT : 0))];
                RC.VertexShader.Set(m_batches[i].Lit ? m_vsLit : m_vs);
                RC.PixelShader.Set(ps);
            
                ISrvBindable texture = m_batches[i].Texture;
                RC.PixelShader.SetSrv(0, texture);
                if (!MyStereoRender.Enable)
                    RC.DrawIndexed(m_batches[i].Num * 6, m_batches[i].Offset * 6, 0);
                else
                    MyStereoRender.DrawIndexedBillboards(RC, m_batches[i].Num * 6, m_batches[i].Offset * 6, 0);

                IBorrowedRtvTexture borrowedTexture = texture as IBorrowedRtvTexture;
                if (borrowedTexture != null)
                    borrowedTexture.Release();

                MyStatsUpdater.Passes.DrawBillboards++;
            }

            m_stats.Billboards += m_billboardCountSafe;

            RC.SetRasterizerState(null);
            MyRender11.GatherPassStats(1298737, "Billboards", m_stats);
            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        internal static MyBillboard AddBillboardOnce()
        {
            var billboard = m_billboardsOncePool.Allocate();
            m_billboardsOnce.Add(billboard);
            return billboard;
        }

        private static void BindResourcesCommon()
        {
            RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
            RC.AllShaderStages.SetSrv(30, m_SB);
            RC.AllShaderStages.SetConstantBuffer(2, m_cbCustomProjections);
            if (MyStereoRender.Enable && MyStereoRender.EnableUsingStencilMask)
                RC.SetDepthStencilState(MyDepthStencilStateManager.StereoDefaultDepthState);
            else
                RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);

            RC.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.AllShaderStages.SetConstantBuffer(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.VertexShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MySamplerStateManager.Shadowmap);
            RC.VertexShader.SetSrv(MyCommon.CASCADES_SM_SLOT, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapArray);
            RC.VertexShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

            ISrvBindable skybox = MyRender11.IsIntelBrokenCubemapsWorkaround
                ? MyGeneratedTextureManager.IntelFallbackCubeTex
                : (ISrvBindable)MyManagers.EnvironmentProbe.Cubemap;
            RC.VertexShader.SetSrv(MyCommon.SKYBOX_IBL_SLOT, skybox);
            MyFileTextureManager texManager = MyManagers.FileTextures;

            RC.SetVertexBuffer(0, m_VB);
            RC.SetIndexBuffer(m_IB);
            RC.SetInputLayout(m_inputLayout);
        }

        private static void InitGatherList(int count, int countSafe)
        {
            int size = m_tempBuffer.Length;
            while (count > size)
                size *= 2;
            Array.Resize(ref m_tempBuffer, size);

            size = m_arrayDataBillboards.Length;
            while (countSafe > size)
                size *= 2;
            m_arrayDataBillboards.Resize(size);

            for (int i = 0; i < BUCKETS_COUNT; i++)
                m_bucketBatches[i] = new MyBucketBatches();

            m_lastBatchOffset = 0;
        }

        private static void InitBucketIndices()
        {
            m_bucketIndices[0] = 0;
            for (int i = 1; i < BUCKETS_COUNT; i++)
                m_bucketIndices[i] = m_bucketIndices[i - 1] + m_bucketCounts[i - 1];
        }
    }
}
