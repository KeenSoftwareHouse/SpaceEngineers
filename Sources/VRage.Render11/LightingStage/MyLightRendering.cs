using SharpDX.Direct3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage.Utils;
using VRageMath;
using VRageRender.Resources;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;

namespace VRageRender
{
    [StructLayout(LayoutKind.Explicit, Size = 192)]
    struct SpotlightConstants
    {
        [FieldOffset(0)]
        internal Matrix ProxyWorldViewProj;
        [FieldOffset(64)]
        internal Matrix ShadowMatrix;

        [FieldOffset(128)]
        internal Vector3 Position;
        [FieldOffset(128 + 12)]
        internal float Range;

        [FieldOffset(128 + 16)]
        internal Vector3 Color;
        [FieldOffset(128 + 28)]
        internal float ApertureCos;

        [FieldOffset(128 + 32)]
        internal Vector3 Direction;
        [FieldOffset(128 + 44)]
        internal float ShadowsRange;

        [FieldOffset(128 + 48)]
        internal Vector3 Up;
        [FieldOffset(128 + 60)]
        internal float GlossFactor;
    }

    class MyLightRendering : MyImmediateRC
    {
        internal static ConstantsBufferId m_pointlightsConstants;
        internal static int m_activePointlights;
        internal static ConstantsBufferId m_spotlightsConstants;
        internal static int m_activeSpotlights;
        internal static ConstantsBufferId m_sunlightConstants;

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

			DirectionalEnvironmentLight_Pixel = MyShaders.CreatePs("light_dir.hlsl");
            DirectionalEnvironmentLight_Sample = MyShaders.CreatePs("light_dir.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            PointlightsTiled_Pixel = MyShaders.CreatePs("light_point.hlsl");
            PointlightsTiled_Sample = MyShaders.CreatePs("light_point.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            m_preparePointLights = MyShaders.CreateCs("prepare_lights.hlsl", new[] { new ShaderMacro("NUMTHREADS", TILE_SIZE) });

			SpotlightProxyVs = MyShaders.CreateVs("light_spot.hlsl");
            SpotlightPs_Pixel = MyShaders.CreatePs("light_spot.hlsl");
            SpotlightPs_Sample = MyShaders.CreatePs("light_spot.hlsl", MyRender11.ShaderSampleFrequencyDefine());
            SpotlightProxyIL = MyShaders.CreateIL(SpotlightProxyVs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION_PACKED));

            m_pointlightCullHwBuffer = MyHwBuffers.CreateStructuredBuffer(MyRender11Constants.MAX_POINT_LIGHTS, sizeof(MyPointlightConstants), true, null, "MyLightRendering");
            m_pointlightsConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MyPointlightInfo) * MyRender11Constants.MAX_POINT_LIGHTS, "MyLightRendering pointLights");
            m_spotlightsConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(SpotlightConstants) * MyRender11Constants.MAX_SPOTLIGHTS, "MyLightRendering spotLights");
            m_sunlightConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MySunlightConstantsLayout), "MyLightRendering sunLights");
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
            BoundingFrustumD viewFrustumClippedD = MyRender11.Environment.ViewFrustumClippedD;
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
                RC.CSSetCB(0, MyCommon.FrameConstants);
            else
                MyStereoRender.CSBindRawCB_FrameConstants(RC);
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            //RC.BindUAV(0, MyScreenDependants.m_test);
            RC.BindUAV(0, MyScreenDependants.m_tileIndices);
            RC.BindGBufferForRead(0, MyGBuffer.Main);
            RC.CSBindRawSRV(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer);
            RC.SetCS(m_preparePointLights);
            Vector2I tiles = new Vector2I(MyScreenDependants.TilesX, MyScreenDependants.TilesY);
            if (MyStereoRender.Enable && MyStereoRender.RenderRegion != MyStereoRegion.FULLSCREEN)
                tiles.X /= 2;

            RC.DeviceContext.Dispatch(tiles.X, tiles.Y, 1);
            RC.SetCS(null);
        }

        internal unsafe static void RenderSpotlights()
        {
            RC.BindDepthRT(MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.ReadOnly, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            RC.DeviceContext.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            RC.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            if (MyStereoRender.Enable)
            {
                MyStereoRender.PSBindRawCB_FrameConstants(RC);
                MyStereoRender.SetViewport(RC);
            }

            var coneMesh = MyMeshes.GetMeshId(X.TEXT_("Models/Debug/Cone.mwm"), 1.0f);
            var buffers = MyMeshes.GetLodMesh(coneMesh, 0).Buffers;
            RC.SetVB(0, buffers.VB0.Buffer, buffers.VB0.Stride);
            RC.SetIB(buffers.IB.Buffer, buffers.IB.Format);

            RC.SetVS(SpotlightProxyVs);
            RC.SetIL(SpotlightProxyIL);
            RC.SetPS(SpotlightPs_Pixel);

            RC.SetRS(MyRender11.m_invTriRasterizerState);

            var cb = MyCommon.GetObjectCB(sizeof(SpotlightConstants));
            RC.SetCB(1, cb);
            RC.DeviceContext.PixelShader.SetSampler(13, SamplerStates.m_alphamask);
            RC.DeviceContext.PixelShader.SetSampler(14, SamplerStates.m_shadowmap);
            RC.DeviceContext.PixelShader.SetSampler(15, SamplerStates.m_shadowmap);

            int index = 0;
            int casterIndex = 0;

            foreach(var id in VisibleSpotlights)
            {
                SpotlightConstants spotlight = new SpotlightConstants();
                MyLights.WriteSpotlightConstants(id, ref spotlight);

                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref spotlight);
                mapping.Unmap();

                RC.DeviceContext.PixelShader.SetShaderResource(13, MyTextures.GetView(MyLights.Spotlights[id.Index].ReflectorTexture));

                if(id.CastsShadowsThisFrame)
                {
                    RC.DeviceContext.PixelShader.SetShaderResource(14, MyRender11.DynamicShadows.ShadowmapsPool[casterIndex].SRV);
                    casterIndex++;
                }

                if (MyRender11.MultisamplingEnabled)
                {
                    RC.SetDS(MyDepthStencilState.TestEdgeStencil, 0);
                    RC.SetPS(SpotlightPs_Pixel);
                }
                RC.DeviceContext.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);

                if (MyRender11.MultisamplingEnabled)
                {
                    RC.SetPS(SpotlightPs_Sample);
                    RC.SetDS(MyDepthStencilState.TestEdgeStencil, 0x80);
                    RC.DeviceContext.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);
                }
                
                index++;
                if (index >= SPOTLIGHTS_MAX)
                    break;
            }

            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetDS(MyDepthStencilState.DefaultDepthState);
            }

            RC.SetRS(null);
        }

        internal static void Render()
        {
            MyLights.Update();
            
            MyGpuProfiler.IC_BeginBlock("Map lights to tiles");
            if (MyRender11.DebugOverrides.PointLights)
                PreparePointLights();
            MyGpuProfiler.IC_EndBlock();

            RC.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            if (!MyStereoRender.Enable)
                RC.CSSetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else
                MyStereoRender.CSBindRawCB_FrameConstants(RC);

            RC.BindGBufferForRead(0, MyGBuffer.Main);
            RC.BindRawSRV(MyCommon.MATERIAL_BUFFER_SLOT, MySceneMaterials.m_buffer);
            RC.SetBS(MyRender11.BlendAdditive);
            if (!MyStereoRender.Enable)
                RC.SetDS(MyDepthStencilState.IgnoreDepthStencil);
            else
                RC.SetDS(MyDepthStencilState.StereoIgnoreDepthStencil);
            RC.DeviceContext.PixelShader.SetSamplers(0, SamplerStates.StandardSamplers);

            MyGpuProfiler.IC_BeginBlock("Apply point lights");
            if (MyRender11.DebugOverrides.PointLights)
                RenderPointlightsTiled();
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("Apply spotlights");
            if (MyRender11.DebugOverrides.SpotLights)
                RenderSpotlights();
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("Apply directional light");
            if (MyRender11.DebugOverrides.EnvLight)
                RenderDirectionalEnvironmentLight();
            MyGpuProfiler.IC_EndBlock();

            // Because of BindGBufferForRead:
            RC.BindRawSRV(0, null);
            RC.BindRawSRV(1, null);
            RC.BindRawSRV(2, null);
            RC.BindRawSRV(3, null);
            RC.BindRawSRV(4, null);
            RC.CSBindRawSRV(0, null);
            RC.CSBindRawSRV(1, null);
            RC.CSBindRawSRV(2, null);
            RC.CSBindRawSRV(3, null);
            RC.CSBindRawSRV(4, null);
            RC.SetBS(null);
        }

        static void RenderPointlightsTiled()
        {
            RC.BindSRV(MyCommon.TILE_LIGHT_INDICES_SLOT, MyScreenDependants.m_tileIndices);
            RC.BindRawSRV(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer);
            RC.BindSRV(MyCommon.AO_SLOT, MyScreenDependants.m_ambientOcclusion);

            RC.SetPS(PointlightsTiled_Pixel);
            MyScreenPass.RunFullscreenPixelFreq(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetPS(PointlightsTiled_Sample);
                MyScreenPass.RunFullscreenSampleFreq(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
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

        static void RenderDirectionalEnvironmentLight()
        {
            PixelShaderId directionalPixelShader;
            if (!MyRenderProxy.Settings.EnableShadows || !MyRender11.DebugOverrides.Shadows)
            {
                if (DirectionalEnvironmentLight_NoShadow == PixelShaderId.NULL)
                    DirectionalEnvironmentLight_NoShadow = MyShaders.CreatePs("light_dir.hlsl", new[] { new ShaderMacro("NO_SHADOWS", null) });

                directionalPixelShader = DirectionalEnvironmentLight_NoShadow;
            }
            else
                directionalPixelShader = DirectionalEnvironmentLight_Pixel;
            MySunlightConstantsLayout constants;
            constants.Direction = MyRender11.Environment.DirectionalLightDir;
            constants.Color = MyRender11.Environment.DirectionalLightIntensity;

            var mapping = MyMapping.MapDiscard(m_sunlightConstants);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();

            //context.VertexShader.Set(MyCommon.FullscreenShader.VertexShader);
            RC.SetPS(directionalPixelShader);
            RC.SetCB(1, m_sunlightConstants);
            RC.SetCB(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.DeviceContext.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, SamplerStates.m_shadowmap);

            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyRender11.Environment.DaySkybox, MyTextureEnum.CUBEMAP, true)));
            
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX_IBL_SLOT,
                MyRender11.IsIntelBrokenCubemapsWorkaround ? MyTextures.GetView(MyTextures.IntelFallbackCubeTexId) : MyEnvironmentProbe.Instance.cubemapPrefiltered.SRV);
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX2_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyRender11.Environment.NightSkybox, MyTextureEnum.CUBEMAP, true)));
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX2_IBL_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyRender11.Environment.NightSkyboxPrefiltered, MyTextureEnum.CUBEMAP, true)));

            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.CASCADES_SM_SLOT, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapArray.SRV);
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SHADOW_SLOT, MyRender11.PostProcessedShadows.SRV);

            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.AMBIENT_BRDF_LUT_SLOT,
                MyCommon.GetAmbientBrdfLut());

            RC.BindSRV(MyCommon.AO_SLOT, MyScreenDependants.m_ambientOcclusion);

            MyScreenPass.RunFullscreenPixelFreq(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetPS(DirectionalEnvironmentLight_Sample);
                MyScreenPass.RunFullscreenSampleFreq(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            }
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SHADOW_SLOT, null);
        }
    }
}
