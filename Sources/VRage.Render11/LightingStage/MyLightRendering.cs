using SharpDX.Direct3D;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRage.Utils;
using VRageMath;
using Vector3 = VRageMath.Vector3;

namespace VRageRender
{
    class MyLightRendering : MyImmediateRC
    {
        static PixelShaderId DirectionalEnvironmentLight_NoShadow = PixelShaderId.NULL;
        static PixelShaderId DirectionalEnvironmentLight_Pixel = PixelShaderId.NULL;
        static PixelShaderId DirectionalEnvironmentLight_Sample = PixelShaderId.NULL;

        static PixelShaderId PointlightsTiled_Pixel;
        static PixelShaderId PointlightsTiled_Sample;

        internal const int TILE_SIZE = 16;
        internal static ComputeShaderId m_preparePointLights;

        static VertexShaderId SpotlightProxyVs;
        static PixelShaderId SpotlightPs_Pixel;
        static PixelShaderId SpotlightPs_Sample;
        static InputLayoutId SpotlightProxyIL;

        private static bool m_lastFrameVisiblePointlights = true;

        internal static unsafe void Init()
        {
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));

			DirectionalEnvironmentLight_Pixel = MyShaders.CreatePs("Lighting/LightDir.hlsl");
            DirectionalEnvironmentLight_Sample = MyShaders.CreatePs("Lighting/LightDir.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            PointlightsTiled_Pixel = MyShaders.CreatePs("Lighting/LightPoint.hlsl");
            PointlightsTiled_Sample = MyShaders.CreatePs("Lighting/LightPoint.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            m_preparePointLights = MyShaders.CreateCs("Lighting/PrepareLights.hlsl", new[] { new ShaderMacro("NUMTHREADS", TILE_SIZE) });

            SpotlightProxyVs = MyShaders.CreateVs("Lighting/LightSpot.hlsl");
            SpotlightPs_Pixel = MyShaders.CreatePs("Lighting/LightSpot.hlsl");
            SpotlightPs_Sample = MyShaders.CreatePs("Lighting/LightSpot.hlsl", MyRender11.ShaderSampleFrequencyDefine());
            SpotlightProxyIL = MyShaders.CreateIL(SpotlightProxyVs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION_PACKED));

            m_pointlightCullHwBuffer = MyHwBuffers.CreateStructuredBuffer(MyRender11Constants.MAX_POINT_LIGHTS, sizeof(MyPointlightConstants), true, null, "MyLightRendering");
        }

        static MyPointlightConstants[] m_pointlightsCullBuffer = new MyPointlightConstants[MyRender11Constants.MAX_POINT_LIGHTS];
        static StructuredBufferId m_pointlightCullHwBuffer;

        internal readonly static List<LightId> VisiblePointlights = new List<LightId>();
        internal readonly static List<LightId> VisibleSpotlights = new List<LightId>();
        private const int SPOTLIGHTS_MAX = 32;

        internal static void DrawFlares()
        {
            foreach (var id in VisiblePointlights)
            {
                DrawFlare(id);
            }
            foreach (var id in VisibleSpotlights)
            {
                DrawFlare(id);
            }
        }
        internal static void DrawFlare(LightId id)
        {
            if (id.FlareId != FlareId.NULL)
                MyFlareRenderer.Draw(id.FlareId, id.SpotPosition);
        }

        internal static void PreparePointLights()
        {
            var activePointlights = 0;

            MyLights.Update();
            BoundingFrustumD viewFrustumClippedD = MyRender11.Environment.Matrices.ViewFrustumClippedD;
            if (MyStereoRender.Enable)
            {
                if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                    viewFrustumClippedD = MyStereoRender.EnvMatricesLeftEye.ViewFrustumClippedD;
                else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                    viewFrustumClippedD = MyStereoRender.EnvMatricesRightEye.ViewFrustumClippedD;
            }
            MyLights.PointlightsBvh.OverlapAllFrustum(ref viewFrustumClippedD, VisiblePointlights);

            bool visiblePointlights = VisiblePointlights.Count != 0;
            if (!visiblePointlights && !m_lastFrameVisiblePointlights)
                return;

            m_lastFrameVisiblePointlights = visiblePointlights;

            if (VisiblePointlights.Count > MyRender11Constants.MAX_POINT_LIGHTS)
            {
                VisiblePointlights.Sort((x, y) => x.ViewerDistanceSquared.CompareTo(y.ViewerDistanceSquared));

                while(VisiblePointlights.Count > MyRender11Constants.MAX_POINT_LIGHTS)
                {
                    VisiblePointlights.RemoveAtFast(VisiblePointlights.Count - 1);
                }
            }

            foreach (var light in VisiblePointlights)
            {
                MyLights.WritePointlightConstants(light, ref m_pointlightsCullBuffer[activePointlights]);

                activePointlights++;
                Debug.Assert(activePointlights <= MyRender11Constants.MAX_POINT_LIGHTS);
            }
            for(int lightIndex = activePointlights; lightIndex < MyRender11Constants.MAX_POINT_LIGHTS; ++lightIndex)
            {
                MyLights.WritePointlightConstants(LightId.NULL, ref m_pointlightsCullBuffer[lightIndex]);
            }

            var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(16));
            mapping.WriteAndPosition(ref activePointlights);
            mapping.Unmap();

            mapping = MyMapping.MapDiscard(m_pointlightCullHwBuffer.Buffer);
            mapping.WriteAndPosition(m_pointlightsCullBuffer, 0, MyRender11Constants.MAX_POINT_LIGHTS);
            mapping.Unmap();

            if (!MyStereoRender.Enable)
                RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else
                MyStereoRender.CSBindRawCB_FrameConstants(RC);
            RC.ComputeShader.SetConstantBuffer(1, MyCommon.GetObjectCB(16));

            //RC.BindUAV(0, MyScreenDependants.m_test);
            RC.ComputeShader.SetRawUav(0, MyScreenDependants.m_tileIndices.m_uav);
            RC.ComputeShader.SetSrvs(0, MyGBuffer.Main);
            RC.ComputeShader.SetRawSrv(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer.Srv);
            RC.ComputeShader.Set(m_preparePointLights);
            Vector2I tiles = new Vector2I(MyScreenDependants.TilesX, MyScreenDependants.TilesY);
            if (MyStereoRender.Enable && MyStereoRender.RenderRegion != MyStereoRegion.FULLSCREEN)
                tiles.X /= 2;

            RC.Dispatch(tiles.X, tiles.Y, 1);
            RC.ComputeShader.Set(null);
            RC.ComputeShader.SetRawUav(0, null);
            RC.ComputeShader.ResetSrvs(0, MyGBufferSrvFilter.ALL);
        }

        internal unsafe static void RenderSpotlights()
        {
            RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, MyGBuffer.Main.LBuffer);
            RC.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            RC.SetPrimitiveTopology(SharpDX.Direct3D.PrimitiveTopology.TriangleList);
            if (MyStereoRender.Enable)
            {
                MyStereoRender.PSBindRawCB_FrameConstants(RC);
                MyStereoRender.SetViewport(RC);
            }

            var coneMesh = MyMeshes.GetMeshId(X.TEXT_("Models/Debug/Cone.mwm"), 1.0f);
            var buffers = MyMeshes.GetLodMesh(coneMesh, 0).Buffers;
            RC.SetVertexBuffer(0, buffers.VB0.Buffer, buffers.VB0.Stride);
            RC.SetIndexBuffer(buffers.IB.Buffer, buffers.IB.Format);

            RC.VertexShader.Set(SpotlightProxyVs);
            RC.SetInputLayout(SpotlightProxyIL);
            RC.PixelShader.Set(SpotlightPs_Pixel);

            RC.SetRasterizerState(MyRasterizerStateManager.InvTriRasterizerState);

            var cb = MyCommon.GetObjectCB(sizeof(SpotlightConstants));
            RC.AllShaderStages.SetConstantBuffer(1, cb);
            RC.PixelShader.SetSampler(13, MySamplerStateManager.Alphamask);
            RC.PixelShader.SetSampler(14, MySamplerStateManager.Shadowmap);
            RC.PixelShader.SetSampler(15, MySamplerStateManager.Shadowmap);

            int index = 0;
            int casterIndex = 0;

            foreach(var id in VisibleSpotlights)
            {
                SpotlightConstants spotlight = new SpotlightConstants();
                MyLights.WriteSpotlightConstants(id, ref spotlight);

                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref spotlight);
                mapping.Unmap();

                RC.PixelShader.SetSrv(13, MyLights.Spotlights[id.Index].ReflectorTexture);

                if(id.CastsShadowsThisFrame)
                {
                    RC.PixelShader.SetSrv(14, MyRender11.DynamicShadows.ShadowmapsPool[casterIndex]);
                    casterIndex++;
                }

                if (MyRender11.MultisamplingEnabled)
                {
                    RC.SetDepthStencilState(MyDepthStencilStateManager.TestEdgeStencil, 0);
                    RC.PixelShader.Set(SpotlightPs_Pixel);
                }
                RC.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);

                if (MyRender11.MultisamplingEnabled)
                {
                    RC.PixelShader.Set(SpotlightPs_Sample);
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

        internal static void Render(ISrvTexture postProcessedShadows)
        {
            MyLights.Update();
            
            MyGpuProfiler.IC_BeginBlock("Map lights to tiles");
            if (MyRender11.DebugOverrides.PointLights)
                PreparePointLights();
            MyGpuProfiler.IC_BeginNextBlock("Apply point lights");

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
            RC.AllShaderStages.SetRawSrv(MyCommon.MATERIAL_BUFFER_SLOT, MySceneMaterials.m_buffer.Srv);
            RC.SetBlendState(MyBlendStateManager.BlendAdditive);
            if (!MyStereoRender.Enable)
                RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            else
                RC.SetDepthStencilState(MyDepthStencilStateManager.StereoIgnoreDepthStencil);
            RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

            if (MyRender11.DebugOverrides.PointLights)
                RenderPointlightsTiled();

            MyGpuProfiler.IC_BeginNextBlock("Apply spotlights");
            if (MyRender11.DebugOverrides.SpotLights)
                RenderSpotlights();

            MyGpuProfiler.IC_BeginNextBlock("Apply directional light");
            if (MyRender11.DebugOverrides.EnvLight)
                RenderDirectionalEnvironmentLight(postProcessedShadows);
            MyGpuProfiler.IC_EndBlock();

            // Because of BindGBufferForRead:
            RC.AllShaderStages.SetSrv(0, null);
            RC.AllShaderStages.SetSrv(1, null);
            RC.AllShaderStages.SetSrv(2, null);
            RC.AllShaderStages.SetSrv(3, null);
            RC.AllShaderStages.SetSrv(4, null);
            RC.SetBlendState(null);
            RC.SetRtv(null);
        }

        static void RenderPointlightsTiled()
        {
            RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.PixelShader.SetRawSrv(MyCommon.TILE_LIGHT_INDICES_SLOT, MyScreenDependants.m_tileIndices.m_srv);
            RC.AllShaderStages.SetRawSrv(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer.Srv);
            RC.PixelShader.SetSrv(MyCommon.AO_SLOT, MyScreenDependants.m_ambientOcclusion);

            RC.PixelShader.Set(PointlightsTiled_Pixel);
            MyScreenPass.RunFullscreenPixelFreq(MyGBuffer.Main.LBuffer);
            if (MyRender11.MultisamplingEnabled)
            {
                RC.PixelShader.Set(PointlightsTiled_Sample);
                MyScreenPass.RunFullscreenSampleFreq(MyGBuffer.Main.LBuffer);
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct MySunlightConstantsLayout
        {
            [FieldOffset(0)]
            internal Vector3 Direction;
            [FieldOffset(16)]
            internal Vector3 Color;
        }

        static void RenderDirectionalEnvironmentLight(ISrvTexture postProcessedShadows)
        {
            PixelShaderId directionalPixelShader;
            MyShadowsQuality shadowsQuality = MyRender11.RenderSettings.ShadowQuality.GetShadowsQuality();
            if (!MyRender11.Settings.EnableShadows || !MyRender11.DebugOverrides.Shadows || shadowsQuality == MyShadowsQuality.DISABLED)
            {
                if (DirectionalEnvironmentLight_NoShadow == PixelShaderId.NULL)
                    DirectionalEnvironmentLight_NoShadow = MyShaders.CreatePs("Lighting/LightDir.hlsl", new[] { new ShaderMacro("NO_SHADOWS", null) });

                directionalPixelShader = DirectionalEnvironmentLight_NoShadow;
            }
            else
                directionalPixelShader = DirectionalEnvironmentLight_Pixel;

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

            RC.PixelShader.SetSrv(MyCommon.AO_SLOT, MyScreenDependants.m_ambientOcclusion);

            MyScreenPass.RunFullscreenPixelFreq(MyGBuffer.Main.LBuffer);
            if (MyRender11.MultisamplingEnabled)
            {
                RC.PixelShader.Set(DirectionalEnvironmentLight_Sample);
                MyScreenPass.RunFullscreenSampleFreq(MyGBuffer.Main.LBuffer);
            }
            RC.PixelShader.SetSrv(MyCommon.SHADOW_SLOT, null);
        }
    }
}
