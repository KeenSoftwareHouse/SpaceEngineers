using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;
using VRage.Profiler;

namespace VRage.Render11.LightingStage
{
    internal class MyLightsRendering : MyImmediateRC
    {
        private static PixelShaderId m_directionalEnvironmentLightNoShadow = PixelShaderId.NULL;
        private static PixelShaderId m_directionalEnvironmentLightPixel = PixelShaderId.NULL;
        private static PixelShaderId m_directionalEnvironmentLightSample = PixelShaderId.NULL;

        private static PixelShaderId m_pointlightsTiledPixel;
        private static PixelShaderId m_pointlightsTiledSample;

        private static ComputeShaderId m_preparePointLights;

        private static VertexShaderId m_spotlightProxyVs;
        private static PixelShaderId m_spotlightPsPixel;
        private static PixelShaderId m_spotlightPsSample;
        private static InputLayoutId m_spotlightProxyIl;

        private static bool m_lastFrameVisiblePointlights = true;

        private const int TILE_SIZE = 16;

        private static ISrvUavBuffer m_tileIndices;

        private static int m_tilesNum;
        private static int m_tilesX;
        private static int m_tilesY;

        private static readonly MyPointlightConstants[] m_pointlightsCullBuffer = new MyPointlightConstants[MyRender11Constants.MAX_POINT_LIGHTS];
        private static ISrvBuffer m_pointlightCullHwBuffer;

        internal static readonly List<LightId> VisiblePointlights = new List<LightId>();
        internal static readonly List<LightId> VisibleSpotlights = new List<LightId>();
        private const int SPOTLIGHTS_MAX = 32;

        internal static unsafe void Init()
        {
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));

            m_directionalEnvironmentLightPixel = MyShaders.CreatePs("Lighting/LightDir.hlsl");
            m_directionalEnvironmentLightSample = MyShaders.CreatePs("Lighting/LightDir.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            m_pointlightsTiledPixel = MyShaders.CreatePs("Lighting/LightPoint.hlsl");
            m_pointlightsTiledSample = MyShaders.CreatePs("Lighting/LightPoint.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            m_preparePointLights = MyShaders.CreateCs("Lighting/PrepareLights.hlsl", new[] { new ShaderMacro("NUMTHREADS", TILE_SIZE) });

            m_spotlightProxyVs = MyShaders.CreateVs("Lighting/LightSpot.hlsl");
            m_spotlightPsPixel = MyShaders.CreatePs("Lighting/LightSpot.hlsl");
            m_spotlightPsSample = MyShaders.CreatePs("Lighting/LightSpot.hlsl", MyRender11.ShaderSampleFrequencyDefine());
            m_spotlightProxyIl = MyShaders.CreateIL(m_spotlightProxyVs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION_PACKED));

            m_pointlightCullHwBuffer = MyManagers.Buffers.CreateSrv(
                "MyLightRendering", MyRender11Constants.MAX_POINT_LIGHTS, sizeof(MyPointlightConstants),
                usage: ResourceUsage.Dynamic);
        }

        internal static void DrawFlares()
        {
            foreach (var id in MyLights.DistantFlaresWithoutLight)
            {
                if (id.FlareId != FlareId.NULL)
                    MyFlareRenderer.Draw(id.FlareId, id.PointPosition);
            }
            foreach (var id in VisiblePointlights)
            {
                if (id.FlareId != FlareId.NULL && !MyLights.GetSpotlights()[id.Index].Enabled)
                    MyFlareRenderer.Draw(id.FlareId, id.PointPosition);
            }
            foreach (var id in VisibleSpotlights)
            {
                if (id.FlareId != FlareId.NULL)
                    MyFlareRenderer.Draw(id.FlareId, id.SpotPosition);
            }
        }

        private static void PreparePointLights()
        {
            bool visiblePointlights = VisiblePointlights.Count != 0;
            if (!visiblePointlights && !m_lastFrameVisiblePointlights)
                return;

            m_lastFrameVisiblePointlights = visiblePointlights;

            if (VisiblePointlights.Count > MyRender11Constants.MAX_POINT_LIGHTS)
            {
                VisiblePointlights.Sort((x, y) => x.ViewerDistanceSquared.CompareTo(y.ViewerDistanceSquared));

                while (VisiblePointlights.Count > MyRender11Constants.MAX_POINT_LIGHTS)
                {
                    VisiblePointlights.RemoveAtFast(VisiblePointlights.Count - 1);
                }
            }

            var activePointlights = 0;
            foreach (var light in VisiblePointlights)
            {
                MyLights.WritePointlightConstants(light, ref m_pointlightsCullBuffer[activePointlights]);

                activePointlights++;
                Debug.Assert(activePointlights <= MyRender11Constants.MAX_POINT_LIGHTS);
            }
            for (int lightIndex = activePointlights; lightIndex < MyRender11Constants.MAX_POINT_LIGHTS; ++lightIndex)
            {
                MyLights.WritePointlightConstants(LightId.NULL, ref m_pointlightsCullBuffer[lightIndex]);
            }

            var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(16));
            mapping.WriteAndPosition(ref activePointlights);
            mapping.Unmap();

            mapping = MyMapping.MapDiscard(m_pointlightCullHwBuffer);
            mapping.WriteAndPosition(m_pointlightsCullBuffer, MyRender11Constants.MAX_POINT_LIGHTS);
            mapping.Unmap();

            if (!MyStereoRender.Enable)
                RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else
                MyStereoRender.CSBindRawCB_FrameConstants(RC);
            RC.ComputeShader.SetConstantBuffer(1, MyCommon.GetObjectCB(16));

            RC.ComputeShader.SetUav(0, m_tileIndices);
            RC.ComputeShader.SetSrvs(0, MyGBuffer.Main);
            RC.ComputeShader.SetSrv(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer);
            RC.ComputeShader.Set(m_preparePointLights);
            Vector2I tiles = new Vector2I(m_tilesX, m_tilesY);
            if (MyStereoRender.Enable && MyStereoRender.RenderRegion != MyStereoRegion.FULLSCREEN)
                tiles.X /= 2;

            RC.Dispatch(tiles.X, tiles.Y, 1);
            RC.ComputeShader.Set(null);
            RC.ComputeShader.SetUav(0, null);
            RC.ComputeShader.ResetSrvs(0, MyGBufferSrvFilter.ALL);
        }

        private static unsafe void RenderSpotlights()
        {
            RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, MyGBuffer.Main.LBuffer);
            RC.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            if (MyStereoRender.Enable)
            {
                MyStereoRender.PSBindRawCB_FrameConstants(RC);
                MyStereoRender.SetViewport(RC);
            }

            var coneMesh = MyMeshes.GetMeshId(X.TEXT_("Models/Debug/Cone.mwm"), 1.0f);
            var buffers = MyMeshes.GetLodMesh(coneMesh, 0).Buffers;
            RC.SetVertexBuffer(0, buffers.VB0);
            RC.SetIndexBuffer(buffers.IB);

            RC.VertexShader.Set(m_spotlightProxyVs);
            RC.SetInputLayout(m_spotlightProxyIl);
            RC.PixelShader.Set(m_spotlightPsPixel);

            RC.SetRasterizerState(MyRasterizerStateManager.InvTriRasterizerState);

            var cb = MyCommon.GetObjectCB(sizeof(SpotlightConstants));
            RC.AllShaderStages.SetConstantBuffer(1, cb);
            RC.PixelShader.SetSampler(13, MySamplerStateManager.Alphamask);
            RC.PixelShader.SetSampler(14, MySamplerStateManager.Shadowmap);
            RC.PixelShader.SetSampler(15, MySamplerStateManager.Shadowmap);

            int index = 0;
            int casterIndex = 0;

            foreach (var id in VisibleSpotlights)
            {
                SpotlightConstants spotlight = new SpotlightConstants();
                var reflectorTexture = MyLights.WriteSpotlightConstants(id, ref spotlight);

                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref spotlight);
                mapping.Unmap();

                RC.PixelShader.SetSrv(13, reflectorTexture);

                if (id.CastsShadowsThisFrame)
                {
                    RC.PixelShader.SetSrv(14, MyRender11.DynamicShadows.ShadowmapsPool[casterIndex]);
                    casterIndex++;
                }

                if (MyRender11.MultisamplingEnabled)
                {
                    RC.SetDepthStencilState(MyDepthStencilStateManager.TestEdgeStencil, 0);
                    RC.PixelShader.Set(m_spotlightPsPixel);
                }
                RC.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);

                if (MyRender11.MultisamplingEnabled)
                {
                    RC.PixelShader.Set(m_spotlightPsSample);
                    RC.SetDepthStencilState(MyDepthStencilStateManager.TestEdgeStencil, 0x80);
                    RC.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);
                }

                index++;
                if (index >= SPOTLIGHTS_MAX)
                    break;
            }

            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);
            }

            RC.SetRasterizerState(null);
            RC.SetRtv(null);
        }

        internal static void Render(ISrvTexture postProcessedShadows, IRtvTexture ambientOcclusion)
        {
            ProfilerShort.Begin("PreparePointLights");
            MyGpuProfiler.IC_BeginBlock("Map lights to tiles");
            if (MyRender11.DebugOverrides.PointLights)
                PreparePointLights();
            MyGpuProfiler.IC_BeginNextBlock("Apply point lights");
            ProfilerShort.End();

            ProfilerShort.Begin("RenderPointlightsTiled");
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            if (!MyStereoRender.Enable)
            {
                RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            }
            else
            {
                MyStereoRender.CSBindRawCB_FrameConstants(RC);
                MyStereoRender.PSBindRawCB_FrameConstants(RC);
            }

            RC.PixelShader.SetSrvs(0, MyGBuffer.Main);
            RC.AllShaderStages.SetSrv(MyCommon.MATERIAL_BUFFER_SLOT, MySceneMaterials.m_buffer);
            RC.SetBlendState(MyBlendStateManager.BlendAdditive);
            RC.SetDepthStencilState(!MyStereoRender.Enable ? MyDepthStencilStateManager.IgnoreDepthStencil : MyDepthStencilStateManager.StereoIgnoreDepthStencil);
            RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

            if (MyRender11.DebugOverrides.PointLights)
                RenderPointlightsTiled(ambientOcclusion);
            ProfilerShort.End();

            ProfilerShort.Begin("RenderSpotlights");
            MyGpuProfiler.IC_BeginNextBlock("Apply spotlights");
            if (MyRender11.DebugOverrides.SpotLights)
                RenderSpotlights();
            ProfilerShort.End();

            ProfilerShort.Begin("RenderDirectionalEnvironmentLight");
            MyGpuProfiler.IC_BeginNextBlock("Apply directional light");
            if (MyRender11.DebugOverrides.EnvLight)
                RenderDirectionalEnvironmentLight(postProcessedShadows, ambientOcclusion);
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();

            // Because of BindGBufferForRead:
            RC.AllShaderStages.SetSrv(0, null);
            RC.AllShaderStages.SetSrv(1, null);
            RC.AllShaderStages.SetSrv(2, null);
            RC.AllShaderStages.SetSrv(3, null);
            RC.AllShaderStages.SetSrv(4, null);
            RC.SetBlendState(null);
            RC.SetRtv(null);
        }

        private static void RenderPointlightsTiled(IRtvTexture ambientOcclusion)
        {
            RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.PixelShader.SetSrv(MyCommon.TILE_LIGHT_INDICES_SLOT, m_tileIndices);
            RC.AllShaderStages.SetSrv(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer);
            RC.PixelShader.SetSrv(MyCommon.AO_SLOT, ambientOcclusion);

            RC.PixelShader.Set(m_pointlightsTiledPixel);
            MyScreenPass.RunFullscreenPixelFreq(MyGBuffer.Main.LBuffer);
            if (MyRender11.MultisamplingEnabled)
            {
                RC.PixelShader.Set(m_pointlightsTiledSample);
                MyScreenPass.RunFullscreenSampleFreq(MyGBuffer.Main.LBuffer);
            }
        }

        private static void RenderDirectionalEnvironmentLight(ISrvTexture postProcessedShadows, IRtvTexture ambientOcclusion)
        {
            PixelShaderId directionalPixelShader;
            MyShadowsQuality shadowsQuality = MyRender11.Settings.User.ShadowQuality.GetShadowsQuality();
            if (!MyRender11.Settings.EnableShadows || !MyRender11.DebugOverrides.Shadows || shadowsQuality == MyShadowsQuality.DISABLED)
            {
                if (m_directionalEnvironmentLightNoShadow == PixelShaderId.NULL)
                    m_directionalEnvironmentLightNoShadow = MyShaders.CreatePs("Lighting/LightDir.hlsl", new[] { new ShaderMacro("NO_SHADOWS", null) });

                directionalPixelShader = m_directionalEnvironmentLightNoShadow;
            }
            else
                directionalPixelShader = m_directionalEnvironmentLightPixel;

            //context.VertexShader.Set(MyCommon.FullscreenShader.VertexShader);
            RC.PixelShader.Set(directionalPixelShader);
            RC.AllShaderStages.SetConstantBuffer(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MySamplerStateManager.Shadowmap);

            MyFileTextureManager texManager = MyManagers.FileTextures;
            RC.PixelShader.SetSrv(MyCommon.SKYBOX_SLOT, texManager.GetTexture(MyRender11.Environment.Data.Skybox, MyFileTextureEnum.CUBEMAP, true));

            ISrvBindable skybox = MyRender11.IsIntelBrokenCubemapsWorkaround
                ? MyGeneratedTextureManager.IntelFallbackCubeTex
                : (ISrvBindable)MyManagers.EnvironmentProbe.Cubemap;
            RC.PixelShader.SetSrv(MyCommon.SKYBOX_IBL_SLOT, skybox);

            RC.PixelShader.SetSrv(MyCommon.CASCADES_SM_SLOT, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapArray);
            RC.PixelShader.SetSrv(MyCommon.SHADOW_SLOT, postProcessedShadows);

            RC.PixelShader.SetSrv(MyCommon.AMBIENT_BRDF_LUT_SLOT,
                MyCommon.GetAmbientBrdfLut());

            RC.PixelShader.SetSrv(MyCommon.AO_SLOT, ambientOcclusion);

            MyScreenPass.RunFullscreenPixelFreq(MyGBuffer.Main.LBuffer);
            if (MyRender11.MultisamplingEnabled)
            {
                RC.PixelShader.Set(m_directionalEnvironmentLightSample);
                MyScreenPass.RunFullscreenSampleFreq(MyGBuffer.Main.LBuffer);
            }
            RC.PixelShader.SetSrv(MyCommon.SHADOW_SLOT, null);
        }

        internal static void Resize(int width, int height)
        {
            m_tilesX = (width + TILE_SIZE - 1) / TILE_SIZE;
            m_tilesY = (height + TILE_SIZE - 1) / TILE_SIZE;
            m_tilesNum = m_tilesX * m_tilesY;

            if (m_tileIndices != null)
                MyManagers.Buffers.Dispose(m_tileIndices);

            m_tileIndices = MyManagers.Buffers.CreateSrvUav("MyScreenDependants::tileIndices", m_tilesNum + m_tilesNum * MyRender11Constants.MAX_POINT_LIGHTS, sizeof(uint));
        }

        public static int GetTilesNum()
        {
            return m_tilesNum;
        }

        public static int GetTilesX()
        {
            return m_tilesX;
        }
    }
}
