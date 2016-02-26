using SharpDX.Direct3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage.Utils;
using VRageMath;
using VRageRender.Lights;
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
        internal float __padding;
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

            m_pointlightCullHwBuffer = MyHwBuffers.CreateStructuredBuffer(MyRender11Constants.MAX_POINT_LIGHTS, sizeof(MyPointlightConstants), true);
            m_pointlightsConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MyPointlightInfo) * MyRender11Constants.MAX_POINT_LIGHTS);
            m_spotlightsConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(SpotlightConstants) * MyRender11Constants.MAX_SPOTLIGHTS);
            m_sunlightConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MySunlightConstantsLayout));
        }

        static MyPointlightConstants[] m_pointlightsCullBuffer = new MyPointlightConstants[MyRender11Constants.MAX_POINT_LIGHTS];
        static StructuredBufferId m_pointlightCullHwBuffer;

        internal readonly static List<LightId> VisiblePointlights = new List<LightId>();
        internal readonly static List<LightId> VisibleSpotlights = new List<LightId>();
        internal static SpotlightConstants[] Spotlights = new SpotlightConstants[32];

        internal static void DrawGlares()
        {
            foreach(var id in VisiblePointlights)
            {
                DrawGlare(id);
            }
            foreach (var id in VisibleSpotlights)
            {
                DrawGlare(id);
            }
        }

        internal static void DrawGlare(LightId light)
        {
            var L = MyEnvironment.CameraPosition - light.Position;
            var distance = (float) L.Length();

            if(!MyLights.Glares.ContainsKey(light))
            {
                return;
            }
            var desc = MyLights.Glares[light];

            switch(desc.Type)
            {
                case MyGlareTypeEnum.Distant:
                    DrawDistantGlare(light, ref desc, distance);
                    break;
                case MyGlareTypeEnum.Normal:
                case MyGlareTypeEnum.Directional:
                    DrawNormalGlare(light, ref desc, L, distance);
                    break;
                default:
                    break;
            }

        }

        internal static void DrawNormalGlare(LightId light, ref MyGlareDesc glare, Vector3 L, float distance)
        {
            //if (m_occlusionRatio <= MyMathConstants.EPSILON)
            //    return;

            var intensity = glare.Intensity;
            var maxDistance = glare.MaxDistance;

            //float alpha = m_occlusionRatio * intensity;
            float alpha = intensity;


            const float minGlareRadius = 0.2f;
            const float maxGlareRadius = 10;
            float radius = MathHelper.Clamp(glare.Range * 20, minGlareRadius, maxGlareRadius);

            float drawingRadius = radius * glare.Size;

            if (glare.Type == MyGlareTypeEnum.Directional)
            {
                float dot = Vector3.Dot(L, glare.Direction);
                alpha *= dot;
            }

            if (alpha <= MyMathConstants.EPSILON)
                return;

            if (distance > maxDistance * .5f)
            {
                // distance falloff
                float falloff = (distance - .5f * maxDistance) / (.5f * maxDistance);
                falloff = (float)Math.Max(0, 1 - falloff);
                drawingRadius *= falloff;
                alpha *= falloff;
            }

            if (drawingRadius <= float.Epsilon)
                return;

            var color = glare.Color;
            color.A = 0;

            MyBillboardsHelper.AddBillboardOriented(glare.Material.ToString(), 
                color * alpha, light.Position, MyEnvironment.InvView.Left, MyEnvironment.InvView.Up, drawingRadius);
        }

        internal static void DrawDistantGlare(LightId light, ref MyGlareDesc glare, float distance)
        {
            //float alpha = m_occlusionRatio * intensity;

            float alpha = glare.Intensity * (glare.QuerySize / 7.5f);

            if (alpha < MyMathConstants.EPSILON)
                return;

            const int minGlareRadius = 5;
            const int maxGlareRadius = 150;

            //glare.QuerySize

            // parent range
            float drawingRadius = MathHelper.Clamp(glare.Range * distance / 1000.0f, minGlareRadius, maxGlareRadius);

            var startFadeout = 800;
            var endFadeout = 1000;

            if (distance > startFadeout)
            {
                var fade = (distance - startFadeout) / (endFadeout - startFadeout);
                alpha *= (1 - fade);
            }

            if (alpha < MyMathConstants.EPSILON)
                return;

            var color = glare.Color;
            color.A = 0;

            var material = (glare.Type == MyGlareTypeEnum.Distant && distance > MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE) ? "LightGlareDistant" : "LightGlare";

            MyBillboardsHelper.AddBillboardOriented(material,
                color * alpha, light.Position, MyEnvironment.InvView.Left, MyEnvironment.InvView.Up, drawingRadius);
        }

        internal static void PreparePointLights()
        {
            var activePointlights = 0;

            MyLights.Update();
            MyLights.PointlightsBvh.OverlapAllFrustum(ref MyEnvironment.ViewFrustumClippedD, VisiblePointlights);

            bool visibleSpotlights = VisiblePointlights.Count != 0;
            if (!visibleSpotlights && !m_lastFrameVisiblePointlights)
                return;

            m_lastFrameVisiblePointlights = visibleSpotlights;

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

            RC.CSSetCB(0, MyCommon.FrameConstants);
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            //RC.BindUAV(0, MyScreenDependants.m_test);
            RC.BindUAV(0, MyScreenDependants.m_tileIndexes);
            RC.BindGBufferForRead(0, MyGBuffer.Main);
            RC.CSBindRawSRV(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer.Srv);
            RC.SetCS(m_preparePointLights);

            var size = MyRender11.ViewportResolution;
            RC.DeviceContext.Dispatch((size.X + TILE_SIZE - 1) / TILE_SIZE, (size.Y + TILE_SIZE - 1) / TILE_SIZE, 1);
            RC.SetCS(null);
        }

        internal unsafe static void RenderSpotlights()
        {
            var coneMesh = MyMeshes.GetMeshId(X.TEXT("Models/Debug/Cone.mwm"));
            var buffers = MyMeshes.GetLodMesh(coneMesh, 0).Buffers;
            RC.SetVB(0, buffers.VB0.Buffer, buffers.VB0.Stride);
            RC.SetIB(buffers.IB.Buffer, buffers.IB.Format);

            RC.SetVS(SpotlightProxyVs);
            RC.SetIL(SpotlightProxyIL);

            RC.SetRS(MyRender11.m_invTriRasterizerState);

            var cb = MyCommon.GetObjectCB(sizeof(SpotlightConstants));
            RC.SetCB(1, cb);
            RC.DeviceContext.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);

            int index = 0;
            int casterIndex = 0;

            foreach(var id in VisibleSpotlights)
            {
                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref MyLightRendering.Spotlights[index]);
                mapping.Unmap();

                RC.DeviceContext.PixelShader.SetShaderResource(13, MyTextures.GetView(MyLights.Spotlights[id.Index].ReflectorTexture));

                if(id.CastsShadowsThisFrame)
                {
                    RC.DeviceContext.PixelShader.SetShaderResource(14, MyRender11.DynamicShadows.ShadowmapsPool[casterIndex].ShaderView);
                    casterIndex++;
                }

                RC.SetPS(SpotlightPs_Pixel);
                if (MyRender11.MultisamplingEnabled)
                {
                    RC.SetDS(MyDepthStencilState.TestEdgeStencil, 0);
                }
                RC.DeviceContext.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);

                if (MyRender11.MultisamplingEnabled)
                {
                    RC.SetPS(SpotlightPs_Sample);
                    RC.SetDS(MyDepthStencilState.TestEdgeStencil, 0x80);
                    RC.DeviceContext.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);
                }
                
                index++;
            }

            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetDS(MyDepthStencilState.DefaultDepthState);
            }

            RC.SetRS(null);
        }

        internal static void Render()
        {
            MyGpuProfiler.IC_BeginBlock("Map lights to tiles");
            MyLightRendering.PreparePointLights();
            MyGpuProfiler.IC_EndBlock();

            RC.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            
            RC.BindGBufferForRead(0, MyGBuffer.Main);
            RC.BindRawSRV(MyCommon.MATERIAL_BUFFER_SLOT, MySceneMaterials.m_buffer.Srv);
            RC.SetBS(MyRender11.BlendAdditive);

            MyGpuProfiler.IC_BeginBlock("Apply point lights");
            RenderPointlightsTiled();
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("Apply spotlights");
            RenderSpotlights();
            MyGpuProfiler.IC_EndBlock();

            DrawGlares();

            MyGpuProfiler.IC_BeginBlock("Apply directional light");
            RenderDirectionalEnvironmentLight();
            MyGpuProfiler.IC_EndBlock();
        }

        static void RenderPointlightsTiled()
        {
            RC.BindSRV(MyCommon.TILE_LIGHT_INDICES_SLOT, MyScreenDependants.m_tileIndexes);
            RC.BindRawSRV(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer.Srv);
            RC.BindSRV(MyCommon.AO_SLOT, MyScreenDependants.m_ambientOcclusion);

            RC.SetPS(PointlightsTiled_Pixel);
            RC.SetDS(MyDepthStencilState.IgnoreDepthStencil);
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
            if (!MyRenderProxy.Settings.EnableShadows)
            {
                if (DirectionalEnvironmentLight_NoShadow == PixelShaderId.NULL)
                    DirectionalEnvironmentLight_NoShadow = MyShaders.CreatePs("light_dir.hlsl", new[] { new ShaderMacro("NO_SHADOWS", null) });

                directionalPixelShader = DirectionalEnvironmentLight_NoShadow;
            }
            else
                directionalPixelShader = DirectionalEnvironmentLight_Pixel;
            MySunlightConstantsLayout constants;
            constants.Direction = MyEnvironment.DirectionalLightDir;
            constants.Color = MyEnvironment.DirectionalLightIntensity;

            var mapping = MyMapping.MapDiscard(m_sunlightConstants);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();

            //context.VertexShader.Set(MyCommon.FullscreenShader.VertexShader);
            RC.SetPS(directionalPixelShader);
            RC.SetCB(1, m_sunlightConstants);
            RC.SetCB(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.DeviceContext.PixelShader.SetSamplers(0, MyRender11.StandardSamplers);
            RC.DeviceContext.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);

            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.DaySkybox, MyTextureEnum.CUBEMAP, true)));
            
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX_IBL_SLOT,
                MyRender11.IsIntelBrokenCubemapsWorkaround ? MyTextures.GetView(MyTextures.IntelFallbackCubeTexId) : MyEnvironmentProbe.Instance.cubemapPrefiltered.ShaderView);
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX2_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.NightSkybox, MyTextureEnum.CUBEMAP, true)));
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX2_IBL_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.NightSkyboxPrefiltered, MyTextureEnum.CUBEMAP, true)));

            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.CASCADES_SM_SLOT, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapArray.ShaderView);
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SHADOW_SLOT, MyRender11.PostProcessedShadows.ShaderView);

            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.AMBIENT_BRDF_LUT_SLOT,
                MyCommon.GetAmbientBrdfLut());

            RC.BindSRV(MyCommon.AO_SLOT, MyScreenDependants.m_ambientOcclusion);

            MyScreenPass.RunFullscreenPixelFreq(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetPS(DirectionalEnvironmentLight_Sample);
                MyScreenPass.RunFullscreenSampleFreq(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            }
        }
    }
}
