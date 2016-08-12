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
using System.Runtime.InteropServices;

namespace VRageRender
{
    struct MyBillboardRendererBatch
    {
        internal int Offset;
        internal int Num;
        internal IShaderResourceBindable Texture;
        internal bool Lit;
        internal bool AlphaCutout;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
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

    [StructLayoutAttribute(LayoutKind.Sequential)]
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
           Color color, Vector3D origin, float radius, float angle, int priority = 0, int customViewProjection = -1)
        {
            if (!MyRender11.DebugOverrides.BillboardsDynamic)
                return;
            Debug.Assert(material != null);

            origin.AssertIsValid();
            angle.AssertIsValid();

            MyQuadD quad;
            if (MyUtils.GetBillboardQuadAdvancedRotated(out quad, origin, radius, angle, MyRender11.Environment.CameraPosition) != false)
            {
                MyBillboard billboard = MyBillboardRenderer.AddBillboardOnce();
                if (billboard == null)
                    return;

                billboard.Priority = priority;
                billboard.CustomViewProjection = customViewProjection;
                CreateBillboard(billboard, ref quad, material, ref color, ref origin);
            }
        }

        internal static void AddBillboardOriented(string material,
            Color color, Vector3D origin, Vector3 leftVector, Vector3 upVector, float radius, int priority = 0, float softParticleDistanceScale = 1.0f, 
            int customViewProjection = -1)
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

            billboard.Priority = priority;
            billboard.CustomViewProjection = customViewProjection;

            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref origin, radius, ref leftVector, ref upVector);

            CreateBillboard(billboard, ref quad, material, ref color, ref origin, softParticleDistanceScale);
        }

        internal static void CreateBillboard(VRageRender.MyBillboard billboard, ref MyQuadD quad, string material, ref Color color, ref Vector3D origin, 
            float softParticleDistanceScale = 1.0f, string blendMaterial = "Test", float textureBlendRatio = 0, Vector2 uvOffset = new Vector2(), 
            bool near = false, bool lowres = false, float reflectivity = 0)
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
            billboard.UVSize = Vector2.One;

            //  Distance for sorting
            //  IMPORTANT: Must be calculated before we do color and alpha misting, because we need distance there
            billboard.DistanceSquared = (float)Vector3D.DistanceSquared(MyRender11.Environment.CameraPosition, origin);

            //  Color
            billboard.Color = color;
            billboard.Reflectivity = reflectivity;
            billboard.ColorIntensity = 1;

            billboard.Near = near;
            billboard.Lowres = lowres;
            billboard.ParentID = -1;

            //  Alpha depends on distance to camera. Very close bilboards are more transparent, so player won't see billboard errors or rotating billboards
            var mat = MyTransparentMaterials.GetMaterial(billboard.Material);
            if (mat.AlphaMistingEnable)
                billboard.Color *= MathHelper.Clamp(((float)Math.Sqrt(billboard.DistanceSquared) - mat.AlphaMistingStart) / (mat.AlphaMistingEnd - mat.AlphaMistingStart), 0, 1);

            billboard.Color *= mat.Color;
            billboard.SoftParticleDistanceScale = softParticleDistanceScale;

            billboard.ContainedBillboards.Clear();
        }
    }

    class MyBillboardRenderer : MyImmediateRC
    {
        const int BILLBOARDS_INIT_SIZE = 8192;
        const int WINDOWS_INIT_SIZE = 2048;
        const int MAX_BILLBOARDS_SIZE = 32768;
        const int MAX_CUSTOM_PROJECTIONS_SIZE = 32;

        static ConstantsBufferId m_cbCustomProjections;

        static VertexShaderId m_vs;
        static VertexShaderId m_vsDepthOnly;
        static PixelShaderId m_ps;
        static PixelShaderId m_psDepthOnly;
        static PixelShaderId m_psOIT;
        static VertexShaderId m_vsLit;
        static PixelShaderId m_psLit;
        static PixelShaderId m_psLitOIT;
        static PixelShaderId m_psAlphaCutout;
        static PixelShaderId m_psAlphaCutoutAndLit;
        static PixelShaderId m_psAlphaCutoutOIT;
        static PixelShaderId m_psAlphaCutoutAndLitOIT;
        static InputLayoutId m_inputLayout;

        static IndexBufferId m_IB = IndexBufferId.NULL;
        static VertexBufferId m_VB;
        static StructuredBufferId m_SB;

        static int m_sortedCount;
        static int m_unsortedCount;
        static int m_windowCount;

        static List<MyBillboardRendererBatch> m_batches = new List<MyBillboardRendererBatch>();
        static MyBillboard[] m_sortedBuffer = new MyBillboard[BILLBOARDS_INIT_SIZE];

        static MyBillboardDataArray m_arrayDataBillboards = new MyBillboardDataArray(BILLBOARDS_INIT_SIZE);
        static MyBillboardDataArray m_arrayDataWindows = new MyBillboardDataArray(WINDOWS_INIT_SIZE);

        private static MyPassStats m_stats = new MyPassStats();

        static List<MyBillboard> m_billboardsOnce = new List<MyBillboard>();
        static MyObjectsPoolSimple<MyBillboard> m_billboardsOncePool = new MyObjectsPoolSimple<MyBillboard>(BILLBOARDS_INIT_SIZE / 4);
        private static MyTextureAtlas m_atlas;

        internal unsafe static void Init()
        {
            m_cbCustomProjections = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix) * MAX_CUSTOM_PROJECTIONS_SIZE, "BilloardCustomProjections");

            m_vs = MyShaders.CreateVs("billboard.hlsl");
            m_vsDepthOnly = MyShaders.CreateVs("billboard_depth_only.hlsl"); 
            m_ps = MyShaders.CreatePs("billboard.hlsl");
            m_psDepthOnly = MyShaders.CreatePs("billboard_depth_only.hlsl"); 
            m_psOIT = MyShaders.CreatePs("billboard.hlsl", new[] { new ShaderMacro("OIT", null) });
            m_vsLit = MyShaders.CreateVs("billboard.hlsl", new[] { new ShaderMacro("LIT_PARTICLE", null) });
            m_psLit = MyShaders.CreatePs("billboard.hlsl", new[] { new ShaderMacro("LIT_PARTICLE", null) });
            m_psLitOIT = MyShaders.CreatePs("billboard.hlsl", new[] { new ShaderMacro("LIT_PARTICLE", null), new ShaderMacro("OIT", null) });

            m_psAlphaCutout = MyShaders.CreatePs("billboard.hlsl", new[] { new ShaderMacro("ALPHA_CUTOUT", null) });
            m_psAlphaCutoutAndLit = MyShaders.CreatePs("billboard.hlsl", 
                new[] { new ShaderMacro("ALPHA_CUTOUT", null), new ShaderMacro("LIT_PARTICLE", null) });
            m_psAlphaCutoutOIT = MyShaders.CreatePs("billboard.hlsl", 
                new[] { new ShaderMacro("ALPHA_CUTOUT", null), new ShaderMacro("OIT", null) });
            m_psAlphaCutoutAndLitOIT = MyShaders.CreatePs("billboard.hlsl", 
                new[] { new ShaderMacro("ALPHA_CUTOUT", null), new ShaderMacro("LIT_PARTICLE", null), new ShaderMacro("OIT", null) });

            m_inputLayout = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.TEXCOORD0_H));

            InitBillboardsIndexBuffer();

            m_VB = MyHwBuffers.CreateVertexBuffer(MAX_BILLBOARDS_SIZE * 4, sizeof(MyVertexFormatPositionTextureH), BindFlags.VertexBuffer, ResourceUsage.Dynamic, null, "MyBillboardRenderer");

            var stride = sizeof(MyBillboardData);
            m_SB = MyHwBuffers.CreateStructuredBuffer(MAX_BILLBOARDS_SIZE, stride, true, null, "MyBillboardRenderer");
            m_atlas = new MyTextureAtlas("Textures\\Particles\\", "Textures\\Particles\\ParticlesAtlas.tai");
        }

        internal static void OnFrameStart()
        {
            m_billboardsOncePool.ClearAllAllocated();
            m_billboardsOnce.Clear();
        }

        unsafe static void InitBillboardsIndexBuffer()
        {
            if(m_IB != IndexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_IB);
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
                m_IB = MyHwBuffers.CreateIndexBuffer(MAX_BILLBOARDS_SIZE * 6, SharpDX.DXGI.Format.R32_UInt, BindFlags.IndexBuffer, ResourceUsage.Immutable, new IntPtr(ptr), "MyBillboardRenderer");
            }
        }

        internal static void OnDeviceRestart()
        {
            InitBillboardsIndexBuffer();
        }

        static void PreGatherList(List<MyBillboard> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var billboard = list[i];

                if (billboard.ContainedBillboards.Count == 0 && billboard.Material != null)
                    PreGatherBillboard(billboard);

                for (int j = 0; j < billboard.ContainedBillboards.Count; j++)
                {
                    var containedBillboard = billboard.ContainedBillboards[j];
                    if (containedBillboard.Material != null)
                        PreGatherBillboard(containedBillboard);
                }
            }
        }

        static void PreGatherBillboard(MyBillboard billboard)
        {
            var material = MyTransparentMaterials.GetMaterial(billboard.Material);
            if (material.NeedSort)
                m_sortedCount++;
            else
                m_unsortedCount++;

            if (billboard.Window && MyScreenDecals.HasEntityDecals((uint)billboard.ParentID))
                m_windowCount++;
        }

        static void GatherList(List<MyBillboard> list, ref int sortedIndex, ref int unsortedIndex)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var billboard = list[i];

                if (billboard.ContainedBillboards.Count == 0 && billboard.Material != null)
                {
                    var material = MyTransparentMaterials.GetMaterial(billboard.Material);
                    if (material.NeedSort)
                        m_sortedBuffer[sortedIndex++] = billboard;
                    else
                        m_sortedBuffer[m_sortedCount + unsortedIndex++] = billboard;
                }

                for (int j = 0; j < billboard.ContainedBillboards.Count; j++)
                {
                    if (billboard.ContainedBillboards[j].Material != null)
                    {
                        var material = MyTransparentMaterials.GetMaterial(billboard.ContainedBillboards[j].Material);
                        if (material.NeedSort)
                            m_sortedBuffer[sortedIndex++] = billboard.ContainedBillboards[j];
                        else
                            m_sortedBuffer[m_sortedCount + unsortedIndex++] = billboard.ContainedBillboards[j];
                    }
                }
            }
        }

        static void Gather()
        {
            m_batches.Clear();

            // counting sorted billboards
            m_sortedCount = 0;
            m_unsortedCount = 0;
            m_windowCount = 0;
            PreGatherList(MyRenderProxy.BillboardsRead);
            PreGatherList(m_billboardsOnce);

            if (BillboardCount == 0)
                return;

            ResizeStorage();

            int sortedIndex = 0;
            int unsortedIndex = 0;
            GatherList(MyRenderProxy.BillboardsRead, ref sortedIndex, ref unsortedIndex);
            GatherList(m_billboardsOnce, ref sortedIndex, ref unsortedIndex);
            
            Array.Sort(m_sortedBuffer, 0, m_sortedCount);

            int i = 0;
            int windowidx = 0;
            var N = BillboardCountSafe;
            int currentOffset = 0;
            TexId prevTexId = TexId.NULL;
            TexId batchTexId = TexId.NULL;
            MyTransparentMaterial prevMaterial = null;
            while (true)
            {
                if (i == N)
                {
                    AddBatch(N, currentOffset, prevTexId, prevMaterial);
                    break;
                }

                MyBillboard billboard = m_sortedBuffer[i];
                MyTransparentMaterial material = MyTransparentMaterials.GetMaterial(billboard.Material);
                if (material.UseAtlas)
                {
                    var atlasItem = m_atlas.FindElement(material.Texture);
                    batchTexId = atlasItem.TextureId;
                }
                else
                {
                    batchTexId = MyTextures.GetTexture(material.Texture, MyTextureEnum.GUI, true);
                }

                bool closeBatch = i > 0 && (batchTexId != prevTexId || i == m_sortedCount);
                if (closeBatch)
                {
                    AddBatch(i, currentOffset, prevTexId, prevMaterial);
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
                    if (MyIDTracker<MyActor>.FindByID((uint)billboard.ParentID) != null)
                    {
                        var matrix = MyIDTracker<MyActor>.FindByID((uint)billboard.ParentID).WorldMatrix;
                        Vector3D.Transform(ref pos0, ref matrix, out pos0);
                        Vector3D.Transform(ref pos1, ref matrix, out pos1);
                        Vector3D.Transform(ref pos2, ref matrix, out pos2);
                        Vector3D.Transform(ref pos3, ref matrix, out pos3);
                    }
                }

                MyEnvironmentMatrices envMatrices = MyRender11.Environment;
                if (MyStereoRender.Enable)
                {
                    if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                        envMatrices = MyStereoRender.EnvMatricesLeftEye;
                    else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                        envMatrices = MyStereoRender.EnvMatricesRightEye;
                }

                if (billboard.CustomViewProjection != -1)
                {
                    var billboardViewProjection = MyRenderProxy.BillboardsViewProjectionRead[billboard.CustomViewProjection];

                    //pos0 -= envMatrices.CameraPosition;
                    //pos1 -= envMatrices.CameraPosition;
                    //pos2 -= envMatrices.CameraPosition;
                    //pos3 -= envMatrices.CameraPosition;
                }
                else
                {
                    pos0 -= envMatrices.CameraPosition;
                    pos1 -= envMatrices.CameraPosition;
                    pos2 -= envMatrices.CameraPosition;
                    pos3 -= envMatrices.CameraPosition;
                }

                var normal = Vector3.Cross(pos1 - pos0, pos2 - pos0);
                normal.Normalize();

                billboardData.Normal = normal;

                billboardVertices.V0.Position = pos0;
                billboardVertices.V1.Position = pos1;
                billboardVertices.V2.Position = pos2;
                billboardVertices.V3.Position = pos3;

                var uv0 = new Vector2(material.UVOffset.X + billboard.UVOffset.X, material.UVOffset.Y + billboard.UVOffset.Y);
                var uv1 = new Vector2(material.UVOffset.X + material.UVSize.X * billboard.UVSize.X + billboard.UVOffset.X , material.UVOffset.Y + billboard.UVOffset.Y);
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

                if (billboard.Window && MyScreenDecals.HasEntityDecals((uint)billboard.ParentID))
                {
                    m_arrayDataWindows.Data[windowidx] = billboardData;
                    m_arrayDataWindows.Vertex[windowidx] = billboardVertices;
                    windowidx++;
                }

                prevTexId = batchTexId;
                prevMaterial = material;
                i++;
            }
        }

        static void AddBatch(int counter, int offset, TexId prevTexture, MyTransparentMaterial prevMaterial)
        {
            MyBillboardRendererBatch batch = new MyBillboardRendererBatch();

            batch.Offset = offset;
            batch.Num = counter - offset;
            batch.Texture = prevTexture;

            batch.Lit = prevMaterial.CanBeAffectedByOtherLights;
            batch.AlphaCutout = prevMaterial.AlphaCutout;

            m_batches.Add(batch);
        }

        static void TransferDataCustomProjections()
        {
            var mapping = MyMapping.MapDiscard(RC.DeviceContext, m_cbCustomProjections);
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

        static unsafe void TransferDataBillboards(int billboardCount, ref MyBillboardDataArray array)
        {
            var mapping = MyMapping.MapDiscard(RC.DeviceContext, m_SB.Buffer);
            mapping.WriteAndPosition(array.Data, 0, billboardCount);
            mapping.Unmap();

            mapping = MyMapping.MapDiscard(RC.DeviceContext, m_VB.Buffer);
            mapping.WriteAndPosition(array.Vertex, 0, billboardCount);
            mapping.Unmap();
        }

        internal unsafe static void Render(MyBindableResource depthRead)
        {
            if (!MyRender11.DebugOverrides.BillboardsDynamic && !MyRender11.DebugOverrides.BillboardsStatic)
                return;

            m_stats.Clear();

            MyRender11.GetRenderProfiler().StartProfilingBlock("Gather");
            Gather();
            MyRender11.GetRenderProfiler().EndProfilingBlock();

            if (m_batches.Count == 0)
                return;

            MyRender11.GetRenderProfiler().StartProfilingBlock("Draw");
            TransferDataCustomProjections();
            TransferDataBillboards(BillboardCountSafe, ref m_arrayDataBillboards);

            RC.BindSRV(1, depthRead);
            BindResourcesCommon();

            for (int i = 0; i < m_batches.Count; i++)
            {
                // first part of batches contain sorted billboards and they read depth
                // second part of batches contain unsorted billboards and they ignore depth
                // switch here:
                if (m_batches[i].Offset == m_sortedCount)
                {
                    //RC.BindDepthRT(null, DepthStencilAccess.ReadOnly, dst);
                }

                if (m_batches[i].Lit)
                {
                    RC.SetVS(m_vsLit);

                    if (m_batches[i].AlphaCutout)
                        RC.SetPS(MyRender11.DebugOverrides.OIT ? m_psAlphaCutoutAndLitOIT : m_psAlphaCutoutAndLit);
                    else
                        RC.SetPS(MyRender11.DebugOverrides.OIT ? m_psLitOIT : m_psLit);
                }
                else if (m_batches[i].AlphaCutout)
                {
                    RC.SetVS(m_vs);
                    RC.SetPS(MyRender11.DebugOverrides.OIT ? m_psAlphaCutoutOIT : m_psAlphaCutout);
                }
                else
                {
                    RC.SetVS(m_vs);
                    RC.SetPS(MyRender11.DebugOverrides.OIT ? m_psOIT : m_ps);
                }

                RC.BindRawSRV(0, m_batches[i].Texture);
                if (!MyStereoRender.Enable)
                    RC.DeviceContext.DrawIndexed(m_batches[i].Num * 6, m_batches[i].Offset * 6, 0);
                else
                    MyStereoRender.DrawIndexedBillboards(RC, m_batches[i].Num * 6, m_batches[i].Offset * 6, 0);
                ++RC.Stats.BillboardDrawIndexed;
            }

            m_stats.Billboards += BillboardCountSafe;

            RC.SetRS(null);
            MyRender11.GatherStats(m_stats);
            MyRender11.GetRenderProfiler().EndProfilingBlock();
        }

        /// <summary>
        /// Render depth and normals of windows to the specified target
        /// REMARK: Only on the windows with decals
        /// </summary>
        /// <returns>True if windows to be rendered found</returns>
        internal static bool RenderWindowsDepthOnly(MyDepthStencil depthStencil, MyBindableResource gbuffer1)
        {
            if (m_windowCount == 0)
                return false;

            TransferDataBillboards(WindowCountSafe, ref m_arrayDataWindows);

            RC.BindDepthRT(depthStencil, DepthStencilAccess.ReadWrite, gbuffer1);
            BindResourcesCommon();

            RC.SetBS(null);
            RC.SetVS(m_vsDepthOnly);
            RC.SetPS(m_psDepthOnly);


            if (!MyStereoRender.Enable)
                RC.DeviceContext.DrawIndexed(m_windowCount * 6, 0, 0);
            else
                MyStereoRender.DrawIndexedBillboards(RC, m_windowCount * 6, 0, 0);

            RC.SetRS(null);
            return true;
        }

        internal static MyBillboard AddBillboardOnce()
        {
            var billboard = m_billboardsOncePool.Allocate();
            m_billboardsOnce.Add(billboard);
            return billboard;
        }

        static void BindResourcesCommon()
        {
            RC.SetRS(MyRender11.m_nocullRasterizerState);
            RC.BindRawSRV(104, m_SB);
            RC.SetCB(2, m_cbCustomProjections);
            if (MyStereoRender.Enable && MyStereoRender.EnableUsingStencilMask)
                RC.SetDS(MyDepthStencilState.StereoDefaultDepthState);
            else
                RC.SetDS(MyDepthStencilState.DefaultDepthState);

            RC.SetCB(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.DeviceContext.VertexShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, SamplerStates.m_shadowmap);
            RC.DeviceContext.VertexShader.SetShaderResource(MyCommon.CASCADES_SM_SLOT, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapArray.SRV);
            RC.DeviceContext.VertexShader.SetSamplers(0, SamplerStates.StandardSamplers);

            RC.DeviceContext.VertexShader.SetShaderResource(MyCommon.SKYBOX_IBL_SLOT,
                MyRender11.IsIntelBrokenCubemapsWorkaround ? MyTextures.GetView(MyTextures.IntelFallbackCubeTexId) : MyEnvironmentProbe.Instance.cubemapPrefiltered.SRV);
            RC.DeviceContext.VertexShader.SetShaderResource(MyCommon.SKYBOX2_IBL_SLOT,
                MyTextures.GetView(MyTextures.GetTexture(MyRender11.Environment.NightSkyboxPrefiltered, MyTextureEnum.CUBEMAP, true)));

            RC.SetVB(0, m_VB.Buffer, m_VB.Stride);
            RC.SetIB(m_IB.Buffer, m_IB.Format);
            RC.SetIL(m_inputLayout);
        }

        static void ResizeStorage()
        {
            int size = m_sortedBuffer.Length;
            while (BillboardCount > size)
                size *= 2;
            Array.Resize(ref m_sortedBuffer, size);

            size = m_arrayDataBillboards.Length;
            while (BillboardCountSafe > size)
                size *= 2;
            m_arrayDataBillboards.Resize(size);

            size = m_arrayDataWindows.Length;
            while (WindowCountSafe > size)
                size *= 2;
            m_arrayDataWindows.Resize(size);
        }

        private static int BillboardCount
        {
            get { return m_sortedCount + m_unsortedCount; }
        }

        private static int BillboardCountSafe
        {
            get
            {
                int count = m_sortedCount + m_unsortedCount;
                return count > MAX_BILLBOARDS_SIZE ? MAX_BILLBOARDS_SIZE : count;
            }
        }

        private static int WindowCountSafe
        {
            get { return m_windowCount > MAX_BILLBOARDS_SIZE ? MAX_BILLBOARDS_SIZE : m_windowCount; }
        }
    }
}
