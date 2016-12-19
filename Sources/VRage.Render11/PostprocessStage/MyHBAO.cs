using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using VRage.Library.Utils;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender.Messages;

namespace VRageRender
{
    class MyHBAO : MyImmediateRC
    {
        internal static MyHBAOData Params { get; set; }
        private static MyHBAOData m_lastParams;

        // constant buffers
        struct GlobalConstantBuffer
        {
            internal Vector2 InvQuarterResolution;
            internal Vector2 InvFullResolution;

            internal Vector2 UVToViewA;
            internal Vector2 UVToViewB;

            internal float RadiusToScreen;
            internal float R2;
            internal float NegInvR2;
            internal float NDotVBias;

            internal float SmallScaleAOAmount;
            internal float LargeScaleAOAmount;
            internal float PowExponent;
            int _pad2;

            internal float BlurViewDepth0;
            internal float BlurViewDepth1;
            internal float BlurSharpness0;
            internal float BlurSharpness1;

            internal float LinearizeDepthA;
            internal float LinearizeDepthB;
            internal float InverseDepthRangeA;
            internal float InverseDepthRangeB;

            internal Vector2 InputViewportTopLeft;
            internal float ViewDepthThresholdNegInv;
            internal float ViewDepthThresholdSharpness;

            internal float BackgroundAORadiusPixels;
            internal float ForegroundAORadiusPixels;
            internal int DebugNormalComponent;
            float _pad0;

            // HLSLcc has a bug with float3x4 so use float4x4 instead
            internal Matrix NormalMatrix;
            internal float NormalDecodeScale;
            internal float NormalDecodeBias;
            Vector2 _pad1;
        };
        unsafe static readonly int GLOBALCONSTANTBUFFERSIZE = sizeof(GlobalConstantBuffer);

        // Must match the GS from Shaders_GL.cpp
        struct PerPassConstantBuffer
        {
            internal Vector4 Jitter;

            internal Vector2 Offset;
            internal float SliceIndexFloat;
            internal uint SliceIndexInt;
        };
        static readonly unsafe int PERPASSCONSTANTBUFFERSIZE = sizeof(PerPassConstantBuffer);

        const int MAX_NUM_MRTS = 8;
        const int NUM_DIRECTIONS = 8;
        const int NUM_SLICES = 16;

        private static PixelShaderId m_linearizeDepthPS = PixelShaderId.NULL;
        private static PixelShaderId m_deinterleaveDepthPS = PixelShaderId.NULL;
        private static PixelShaderId m_coarseAOPS = PixelShaderId.NULL;
        private static PixelShaderId m_copyPS = PixelShaderId.NULL;
        private static PixelShaderId m_reinterleaveAOPS = PixelShaderId.NULL;
        private static PixelShaderId m_reinterleaveAOPS_PreBlur = PixelShaderId.NULL;
        private static PixelShaderId m_blurXPS = PixelShaderId.NULL;
        private static PixelShaderId m_blurYPS = PixelShaderId.NULL;

        private static IConstantBuffer m_dataCB;
        private static IConstantBuffer[] m_perPassCBs = new IConstantBuffer[NUM_SLICES];

        private static IRtvTexture m_fullResViewDepthTarget;
        private static IRtvTexture m_fullResNormalTexture;
        private static IRtvTexture m_fullResAOZTexture;
        private static IRtvTexture m_fullResAOZTexture2;

        private static IRtvArrayTexture m_quarterResViewDepthTextureArray;
        private static IRtvArrayTexture m_quarterResAOTextureArray;

        internal static void Run(IRtvTexture dst, MyGBuffer gbuffer, MyViewport? viewport = null)
        {
            CompilePS();
            
            if (!viewport.HasValue)
            {
                viewport = new MyViewport(0, 0, MyRender11.m_resolution.X, MyRender11.m_resolution.Y);
            }
            var data = InitConstantBuffer(viewport.Value);

            var mapping = MyMapping.MapDiscard(m_dataCB);
            mapping.WriteAndPosition(ref data);
            mapping.Unmap();
            RC.PixelShader.SetConstantBuffer(0, m_dataCB);

            RC.PixelShader.SetSamplers(0, MySamplerStateManager.PointHBAOClamp);
            RC.SetBlendState(null);
            RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            DrawLinearDepthPS(gbuffer.ResolvedDepthStencil.SrvDepth, m_fullResViewDepthTarget, viewport.Value);

            DrawDeinterleavedDepth(viewport.Value);

            DrawCoarseAO(gbuffer, viewport.Value);

            if (Params.BlurEnable)
            {
                Resolve(true, m_fullResAOZTexture2, viewport.Value);

                DrawBlurXPS(viewport.Value);
                DrawBlurYPS(dst, viewport.Value);
            }
            else Resolve(false, dst, viewport.Value);

            RC.SetRtv(null);
        }

        static void DrawLinearDepthPS(ISrvBindable resolvedDepth, IRtvBindable rtv, MyViewport viewport)
        {
            RC.PixelShader.Set(m_linearizeDepthPS);
            //RC.SetRtv(m_fullResViewDepthTarget);
            RC.SetRtv(rtv);
            RC.PixelShader.SetSrv(0, resolvedDepth);
            MyScreenPass.DrawFullscreenQuad(viewport);
            RC.SetRtv(null);
        }

        static MyViewport GetQuarterViewport(MyViewport viewport)
        {
            MyViewport qViewport;
            qViewport.OffsetX = 0;
            qViewport.OffsetY = 0;
            qViewport.Width = DivUp((int)viewport.Width, 4);
            qViewport.Height = DivUp((int)viewport.Height, 4);
            return qViewport;
        }

        static void DrawDeinterleavedDepth(MyViewport viewport)
        {
            var qViewport = GetQuarterViewport(viewport);
            RC.PixelShader.Set(m_deinterleaveDepthPS);
            RC.PixelShader.SetSrv(0, m_fullResViewDepthTarget);

            var rtvs = new IRtvBindable[MAX_NUM_MRTS];
            for (int sliceIndex = 0; sliceIndex < NUM_SLICES; sliceIndex += MAX_NUM_MRTS)
            {
                for (int i = 0; i < MAX_NUM_MRTS; i++)
                    rtvs[i] = m_quarterResViewDepthTextureArray.SubresourceRtv(sliceIndex + i);
                RC.SetRtvs(rtvs);
                RC.PixelShader.SetConstantBuffer(1, m_perPassCBs[sliceIndex]);

                MyScreenPass.DrawFullscreenQuad(qViewport);
            }
        }

        static void DrawCoarseAO(MyGBuffer gbuffer, MyViewport viewport)
        {
            var qViewport = GetQuarterViewport(viewport);

            RC.PixelShader.Set(m_coarseAOPS);
            RC.PixelShader.SetSamplers(0, Params.DepthClampToEdge ? MySamplerStateManager.PointHBAOClamp : MySamplerStateManager.PointHBAOBorder);
            RC.PixelShader.SetSamplers(1, MySamplerStateManager.PointHBAOClamp);
            RC.PixelShader.SetSrv(1, gbuffer.GBuffer1);

            for (int sliceIndex = 0; sliceIndex < NUM_SLICES; ++sliceIndex)
            {
                RC.PixelShader.SetSrv(0, m_quarterResViewDepthTextureArray.SubresourceSrv(sliceIndex));

                RC.PixelShader.SetConstantBuffer(1, m_perPassCBs[sliceIndex]);
                RC.GeometryShader.SetConstantBuffer(1, m_perPassCBs[sliceIndex]);

                RC.SetRtv(m_quarterResAOTextureArray.SubresourceRtv(sliceIndex));
                MyScreenPass.DrawFullscreenQuad(qViewport);
            }

            RC.GeometryShader.Set(null);
        }

        static void Resolve(bool blur, IRtvBindable dst, MyViewport viewport)
        {
            RC.SetRtv(dst);
            RC.PixelShader.SetSrv(0, m_quarterResAOTextureArray);
            if (blur)
            {
                RC.PixelShader.Set(m_reinterleaveAOPS_PreBlur);
                RC.PixelShader.SetSrv(1, m_fullResViewDepthTarget);
            }
            else
            {
                RC.PixelShader.Set(m_reinterleaveAOPS);
            }
            RC.PixelShader.SetSamplers(0, MySamplerStateManager.PointHBAOClamp);
            MyScreenPass.DrawFullscreenQuad(viewport);
        }

        static void DrawBlurXPS(MyViewport viewport)
        {
            RC.SetRtv(m_fullResAOZTexture);
            RC.PixelShader.Set(m_blurXPS);
            RC.PixelShader.SetSrv(0, m_fullResAOZTexture2);
            RC.PixelShader.SetSamplers(0, MySamplerStateManager.Point);
            RC.PixelShader.SetSamplers(1, MySamplerStateManager.Linear);

            MyScreenPass.DrawFullscreenQuad(viewport);
        }

        static void DrawBlurYPS(IRtvBindable dst, MyViewport viewport)
        {
            RC.SetRtv(dst);
            RC.PixelShader.Set(m_blurYPS);
            RC.PixelShader.SetSrv(0, m_fullResAOZTexture);

            MyScreenPass.DrawFullscreenQuad(viewport);
        }

        private static void DebugDraw(ISrvBindable src, IRtvBindable dst, MyViewport viewport)
        {
            RC.PixelShader.Set(m_copyPS);
            RC.SetRtv(dst);
            RC.PixelShader.SetSrv(0, src);
            MyScreenPass.DrawFullscreenQuad(viewport);
        }

        static MyHBAO()
        {
            Params = MyHBAOData.Default;
        }

        private static readonly List<SharpDX.Direct3D.ShaderMacro> m_macros = new List<SharpDX.Direct3D.ShaderMacro>();
        internal static void CompilePS()
        {
            if (m_coarseAOPS == PixelShaderId.NULL || m_lastParams.BackgroundAOEnable != Params.BackgroundAOEnable ||
                m_lastParams.ForegroundAOEnable != Params.ForegroundAOEnable ||
                m_lastParams.DepthThresholdEnable != Params.DepthThresholdEnable)
            {
                m_macros.Clear();
                m_macros.Add(new SharpDX.Direct3D.ShaderMacro("FETCH_GBUFFER_NORMAL", 1));
                if (Params.BackgroundAOEnable)
                    m_macros.Add(new SharpDX.Direct3D.ShaderMacro("ENABLE_BACKGROUND_AO", 1));
                if (Params.ForegroundAOEnable)
                    m_macros.Add(new SharpDX.Direct3D.ShaderMacro("ENABLE_FOREGROUND_AO", 1));
                if (Params.DepthThresholdEnable)
                    m_macros.Add(new SharpDX.Direct3D.ShaderMacro("ENABLE_DEPTH_THRESHOLD", 1));
                m_coarseAOPS = MyShaders.CreatePs("Postprocess/HBAO/CoarseAO.hlsl", m_macros.ToArray());
            }

            if (m_blurXPS == PixelShaderId.NULL || m_blurYPS == PixelShaderId.NULL ||
                m_lastParams.BlurSharpnessFunctionEnable != Params.BlurSharpnessFunctionEnable ||
                m_lastParams.BlurRadius4 != Params.BlurRadius4)
            {
                m_macros.Clear();
                if (Params.BlurSharpnessFunctionEnable)
                    m_macros.Add(new SharpDX.Direct3D.ShaderMacro("ENABLE_SHARPNESS_PROFILE", 1));
                if (Params.BlurRadius4)
                    m_macros.Add(new SharpDX.Direct3D.ShaderMacro("KERNEL_RADIUS", 4));
                else m_macros.Add(new SharpDX.Direct3D.ShaderMacro("KERNEL_RADIUS", 2));
                m_blurXPS = MyShaders.CreatePs("Postprocess/HBAO/BlurX.hlsl", m_macros.ToArray());
                m_blurYPS = MyShaders.CreatePs("Postprocess/HBAO/BlurY.hlsl", m_macros.ToArray());
            }
            m_lastParams = Params;
        }

        internal static void Init()
        {
            m_linearizeDepthPS = MyShaders.CreatePs("Postprocess/HBAO/LinearizeDepth.hlsl");
            m_deinterleaveDepthPS = MyShaders.CreatePs("Postprocess/HBAO/DeinterleaveDepth.hlsl");
            m_reinterleaveAOPS = MyShaders.CreatePs("Postprocess/HBAO/ReinterleaveAO.hlsl");
            m_reinterleaveAOPS_PreBlur = MyShaders.CreatePs("Postprocess/HBAO/ReinterleaveAO.hlsl",
                new SharpDX.Direct3D.ShaderMacro[] { new SharpDX.Direct3D.ShaderMacro("ENABLE_BLUR", 1) });
            m_copyPS = MyShaders.CreatePs("Postprocess/HBAO/Copy.hlsl");

            m_dataCB = MyManagers.Buffers.CreateConstantBuffer("MyHBAO::dataCB", GLOBALCONSTANTBUFFERSIZE, usage: ResourceUsage.Dynamic);

            for (int it = 0; it < NUM_SLICES; it++)
                m_perPassCBs[it] = null;

            InitializeConstantBuffer();
        }

        public static void InitializeConstantBuffer(int? randomSeed = null)
        {
            MyRandom random;
            if (randomSeed.HasValue)
                random = new MyRandom(randomSeed.Value);
            else
                random = new MyRandom();

            const int JITTERSIZE = 4 * 4;
            var jitters = new Vector4[JITTERSIZE];
            for (int i = 0; i < JITTERSIZE; i++)
            {
                float angle = 2.0f * (float)Math.PI * random.NextFloat() / NUM_DIRECTIONS;
                jitters[i].X = (float)Math.Cos(angle);
                jitters[i].Y = (float)Math.Sin(angle);
                jitters[i].Z = random.NextFloat();
                jitters[i].W = random.NextFloat();
            }
            PerPassConstantBuffer data;
            for (uint sliceIndex = 0; sliceIndex < NUM_SLICES; ++sliceIndex)
            {
                data.Offset.X = (float)(sliceIndex % 4) + 0.5f;
                data.Offset.Y = (float)(sliceIndex / 4) + 0.5f;
                data.Jitter = jitters[sliceIndex];
                data.SliceIndexFloat = (float)sliceIndex;
                data.SliceIndexInt = sliceIndex;

                var buffer = m_perPassCBs[sliceIndex];
                if (buffer == null)
                {
                    buffer = MyManagers.Buffers.CreateConstantBuffer("MyHBAO::passCB " + sliceIndex, PERPASSCONSTANTBUFFERSIZE, usage: ResourceUsage.Dynamic);
                    m_perPassCBs[sliceIndex] = buffer;
                }

                var mapping = MyMapping.MapDiscard(buffer);
                mapping.WriteAndPosition(ref data);
                mapping.Unmap();
            }
        }

        private static int DivUp(int a, int b)
        {
            return ((a + b - 1) / b);
        }

        internal static void InitScreenResources()
        {
            MyRwTextureManager rwManager = MyManagers.RwTextures;
            m_fullResViewDepthTarget = rwManager.CreateRtv("MyHBAO.FullResViewDepthTarget", MyRender11.m_resolution.X, MyRender11.m_resolution.Y,
                SharpDX.DXGI.Format.R32_Float, 1, 0);
            m_fullResNormalTexture = rwManager.CreateRtv("MyHBAO.FullResNormalTexture", MyRender11.m_resolution.X, MyRender11.m_resolution.Y,
                SharpDX.DXGI.Format.R8G8B8A8_UNorm, 1, 0);
            m_fullResAOZTexture = rwManager.CreateRtv("MyHBAO.FullResAOZTexture", MyRender11.m_resolution.X, MyRender11.m_resolution.Y,
                SharpDX.DXGI.Format.R16G16_Float, 1, 0);
            m_fullResAOZTexture2 = rwManager.CreateRtv("MyHBAO.FullResAOZTexture2", MyRender11.m_resolution.X, MyRender11.m_resolution.Y,
                SharpDX.DXGI.Format.R16G16_Float, 1, 0);

            MyArrayTextureManager arrayManager = MyManagers.ArrayTextures;
            m_quarterResViewDepthTextureArray = arrayManager.CreateRtvArray("MyHBAO.QuarterResViewDepthTextureArray",
                DivUp(MyRender11.m_resolution.X, 4), DivUp(MyRender11.m_resolution.Y, 4), NUM_SLICES, SharpDX.DXGI.Format.R16_Float);
            m_quarterResAOTextureArray = arrayManager.CreateRtvArray("MyHBAO.QuarterResAOTextureArray",
                DivUp(MyRender11.m_resolution.X, 4), DivUp(MyRender11.m_resolution.Y, 4), NUM_SLICES, SharpDX.DXGI.Format.R8_UNorm);
        }

        internal static void ReleaseScreenResources()
        {
            if (m_fullResViewDepthTarget != null)
            {
                MyRwTextureManager rwManager = MyManagers.RwTextures;
                rwManager.DisposeTex(ref m_fullResViewDepthTarget);
                rwManager.DisposeTex(ref m_fullResNormalTexture);
                rwManager.DisposeTex(ref m_fullResAOZTexture);
                rwManager.DisposeTex(ref m_fullResAOZTexture2);

                MyArrayTextureManager arrayManager = MyManagers.ArrayTextures;
                arrayManager.DisposeTex(ref m_quarterResViewDepthTextureArray);
                arrayManager.DisposeTex(ref m_quarterResAOTextureArray);
            }
        }

        static float METERS_TO_VIEW_SPACE_UNITS = 1.0f;
        static GlobalConstantBuffer InitConstantBuffer(MyViewport viewport)
        {
            GlobalConstantBuffer m_Data = new GlobalConstantBuffer();
            Matrix m = MyRender11.Environment.Matrices.Projection;

            // ProjectionMatrixInfo
            // In matrices generated with D3DXMatrixPerspectiveFovRH
            // A = zf/(zn-zf)
            // B = zn*zf/(zn-zf)
            // C = -1
            float A = m.M33;
            float B = m.M43;

            // Rely on INFs to be generated in case of any divisions by zero
            float zNear = B / A;
            float zFar = B / (A + 1);
            // Some matrices may use negative m00 or m11 terms to flip X/Y axises
            float tanHalfFovX = 1 / Math.Abs(m.M11);
            float tanHalfFovY = 1 / Math.Abs(m.M22);

            // SetDepthLinearizationConstants
            const float EPSILON = 1e-6f;
            float inverseZNear = Math.Max(1 / zNear, EPSILON);
            float inverseZFar = Math.Max(1 / zFar, EPSILON);
            m_Data.LinearizeDepthA = inverseZFar - inverseZNear;
            m_Data.LinearizeDepthB = inverseZNear;

            // SetViewportConstants
            m_Data.InverseDepthRangeA = 1f;
            m_Data.InverseDepthRangeB = 0f;

            m_Data.InputViewportTopLeft.X = viewport.OffsetX;
            m_Data.InputViewportTopLeft.Y = viewport.OffsetY;

            // SetProjectionConstants
            m_Data.UVToViewA.X = 2 * tanHalfFovX;
            m_Data.UVToViewA.Y = -2 * tanHalfFovY;
            m_Data.UVToViewB.X = -1 * tanHalfFovX;
            m_Data.UVToViewB.Y = 1 * tanHalfFovY;

            // SetResolutionConstants
            m_Data.InvFullResolution.X = 1.0f / viewport.Width;
            m_Data.InvFullResolution.Y = 1.0f / viewport.Height;
            m_Data.InvQuarterResolution.X = 1.0f / DivUp((int)viewport.Width, 4);
            m_Data.InvQuarterResolution.Y = 1.0f / DivUp((int)viewport.Height, 4);

            // SetNormalData
            m_Data.NormalMatrix = MyRender11.Environment.Matrices.ViewAt0;
            m_Data.NormalDecodeScale = 2;
            m_Data.NormalDecodeBias = -1;

            // SetAORadiusConstants
            float radiusInMeters = Math.Max(Params.Radius, EPSILON);
            float r = radiusInMeters * METERS_TO_VIEW_SPACE_UNITS;
            m_Data.R2 = r * r;
            m_Data.NegInvR2 = -1 / m_Data.R2;

            m_Data.RadiusToScreen = r * 0.5f / tanHalfFovY * viewport.Height;

            float backgroundViewDepth = Math.Max(Params.BackgroundViewDepth, EPSILON);
            if (Params.AdaptToFOV)
            {
                // use larger background view depth for low FOV values (less then 30 degrees)
                float factor = Math.Min(1.0f, MyRender11.Environment.Matrices.FovH / MathHelper.ToRadians(30));
                backgroundViewDepth = MathHelper.Lerp(6000, backgroundViewDepth, factor);
            }
            m_Data.BackgroundAORadiusPixels = m_Data.RadiusToScreen / backgroundViewDepth;

            float foregroundViewDepth = Math.Max(Params.ForegroundViewDepth, EPSILON);
            m_Data.ForegroundAORadiusPixels = m_Data.RadiusToScreen / foregroundViewDepth;

            // SetBlurConstants
            float BaseSharpness = Math.Max(Params.BlurSharpness, 0);
            BaseSharpness /= METERS_TO_VIEW_SPACE_UNITS;

            if (Params.BlurSharpnessFunctionEnable)
            {
                m_Data.BlurViewDepth0 = Math.Max(Params.BlurSharpnessFunctionForegroundViewDepth, 0);
                m_Data.BlurViewDepth1 = Math.Max(Params.BlurSharpnessFunctionBackgroundViewDepth, m_Data.BlurViewDepth0 + EPSILON);
                m_Data.BlurSharpness0 = BaseSharpness * Math.Max(Params.BlurSharpnessFunctionForegroundScale, 0);
                m_Data.BlurSharpness1 = BaseSharpness;
            }
            else
            {
                m_Data.BlurSharpness0 = BaseSharpness;
                m_Data.BlurSharpness1 = BaseSharpness;
                m_Data.BlurViewDepth0 = 0;
                m_Data.BlurViewDepth1 = 1;
            }

            // SetDepthThresholdConstants
            if (Params.DepthThresholdEnable)
            {
                m_Data.ViewDepthThresholdNegInv = -1 / Math.Max(Params.DepthThreshold, EPSILON);
                m_Data.ViewDepthThresholdSharpness = Math.Max(Params.DepthThresholdSharpness, 0);
            }
            else
            {
                m_Data.ViewDepthThresholdNegInv = 0;
                m_Data.ViewDepthThresholdSharpness = 1;
            }

            // SetAOParameters
            m_Data.PowExponent = Math.Min(Math.Max(Params.PowerExponent, 1), 8);
            m_Data.NDotVBias = Math.Min(Math.Max(Params.Bias, 0.0f), 0.5f);

            float aoAmountScaleFactor = 1 / (1 - m_Data.NDotVBias);
            m_Data.SmallScaleAOAmount = Math.Min(Math.Max(Params.SmallScaleAO, 0), 4) * aoAmountScaleFactor * 2;
            m_Data.LargeScaleAOAmount = Math.Min(Math.Max(Params.LargeScaleAO, 0), 4) * aoAmountScaleFactor;

            return m_Data;
        }
    }
}
