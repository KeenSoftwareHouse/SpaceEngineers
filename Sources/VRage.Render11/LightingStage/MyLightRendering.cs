using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Resource = SharpDX.Direct3D11.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using Color = VRageMath.Color;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Runtime.InteropServices;
using VRageRender.Lights;
using VRage.Utils;

namespace VRageRender
{
    struct MyMapping
    {
        internal DataStream stream;
        internal DeviceContext context;
        internal Resource buffer;
        internal DataBox dataBox;

        internal static MyMapping MapDiscard(DeviceContext context, Resource buffer)
        {
            MyMapping mapping;
            mapping.context = context;
            mapping.buffer = buffer;
            mapping.dataBox = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None, out mapping.stream);
            return mapping;
        }

        internal static MyMapping MapDiscard(Resource buffer)
        {
            return MapDiscard(MyRender11.ImmediateContext, buffer);
        }

        internal void Unmap()
        {
            context.UnmapSubresource(buffer, 0);
            stream.Dispose();
        }
    }

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
    }

    class MyLightRendering : MyImmediateRC
    {
        internal static ConstantsBufferId m_pointlightsConstants;
        internal static int m_activePointlights;
        internal static ConstantsBufferId m_spotlightsConstants;
        internal static int m_activeSpotlights;
        internal static ConstantsBufferId m_sunlightConstants;

        static PixelShaderId DirectionalEnvironmentLight_Pixel;
        static PixelShaderId DirectionalEnvironmentLight_Sample;

        static PixelShaderId PointlightsTiled_Pixel;
        static PixelShaderId PointlightsTiled_Sample;

        internal const int TILE_SIZE = 16;
        internal static ComputeShaderId m_preparePointLights;

        static VertexShaderId SpotlightProxyVs;
        static PixelShaderId SpotlightPs_Pixel;
        static PixelShaderId SpotlightPs_Sample;
        static InputLayoutId SpotlightProxyIL;

        internal static unsafe void Init()
        {
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));

            DirectionalEnvironmentLight_Pixel = MyShaders.CreatePs("light.hlsl", "directional_environment");
            DirectionalEnvironmentLight_Sample = MyShaders.CreatePs("light.hlsl", "directional_environment", MyShaderHelpers.FormatMacros(MyRender11.ShaderSampleFrequencyDefine()));

            PointlightsTiled_Pixel = MyShaders.CreatePs("light.hlsl", "pointlights_tiled");
            PointlightsTiled_Sample = MyShaders.CreatePs("light.hlsl", "pointlights_tiled", MyShaderHelpers.FormatMacros(MyRender11.ShaderSampleFrequencyDefine()));

            m_preparePointLights = MyShaders.CreateCs("prepare_lights.hlsl", "prepare_lights", MyShaderHelpers.FormatMacros("NUMTHREADS " + TILE_SIZE));

            SpotlightProxyVs = MyShaders.CreateVs("light.hlsl", "spotlightVs");
            SpotlightPs_Pixel = MyShaders.CreatePs("light.hlsl", "spotlightFromProxy");
            SpotlightPs_Sample = MyShaders.CreatePs("light.hlsl", "spotlightFromProxy", MyShaderHelpers.FormatMacros(MyRender11.ShaderSampleFrequencyDefine()));
            SpotlightProxyIL = MyShaders.CreateIL(SpotlightProxyVs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION_PACKED));

            var stride = sizeof(MyPointlightConstants);
            m_pointlightCullHwBuffer = MyHwBuffers.CreateStructuredBuffer(MyRender11Constants.MAX_POINT_LIGHTS, stride, true);
            m_pointlightsConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MyPointlightInfo) * MyRender11Constants.MAX_POINT_LIGHTS);
            m_spotlightsConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MySpotlightConstants) * MyRender11Constants.MAX_SPOTLIGHTS);
            m_sunlightConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MySunlightConstantsLayout));
        }

        static MyPointlightConstants[] m_pointlightsCullBuffer = new MyPointlightConstants[MyRender11Constants.MAX_POINT_LIGHTS];
        static StructuredBufferId m_pointlightCullHwBuffer;

        internal static List<LightId> VisiblePointlights = new List<LightId>();
        internal static List<LightId> VisibleSpotlights = new List<LightId>();
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

            DrawSunGlare();
        }

        internal const float RENDER_SUN_DISTANCE = 1800;

        internal static void DrawSunGlare()
        {
            if (MyEnvironment.DirectionalLightDir == Vector3.Zero)
                return;
            if (MyEnvironment.SunMaterial == null)
                return;


            //// this should be computed every time the sector is changed. If it is not initialized, calculate now:
            //m_distanceToSun = MyRender.Sun.DistanceToSun;

            //m_directionToSunNormalized = -MyRender.Sun.Direction;

            //float radius = MyRender.Sun.SunSizeMultiplier * MySunConstants.SUN_SIZE_MULTIPLIER * MySunConstants.RENDER_SUN_DISTANCE / m_distanceToSun;
            ////radius = Math.Max(MySunConstants.MIN_SUN_SIZE * MyRender.Sun.SunSizeMultiplier, radius);
            ////radius = Math.Min(MySunConstants.MAX_SUN_SIZE * MyRender.Sun.SunSizeMultiplier, radius);

            //float sunColorMultiplier = 3;

            //sunColorMultiplier *= (1 - MyRender.FogProperties.FogMultiplier * 0.7f);

            //m_querySize = .5f * radius;

            //var sunPosition = GetSunPosition();
            //radius *= .5f;
            //Color color = new Color(.95f * sunColorMultiplier, .65f * sunColorMultiplier, .35f * sunColorMultiplier, 1);

            //color = color * 5;

            //MyTransparentGeometry.AddPointBillboard(MyRender.Sun.SunMaterial, color, sunPosition, radius, 0);

            var distanceToSun = MyEnvironment.SunDistance;

            var directionToSunNormalized = -MyEnvironment.DirectionalLightDir;

            float radius = MyEnvironment.SunSizeMultiplier * RENDER_SUN_DISTANCE / distanceToSun;

            float sunColorMultiplier = 3;
            sunColorMultiplier *= (1 - MyEnvironment.FogSettings.FogMultiplier * 0.7f);

            var sunPosition = MyEnvironment.CameraPosition + directionToSunNormalized * RENDER_SUN_DISTANCE;
            radius *= .5f;
            Color color = new Color(.95f * sunColorMultiplier, .65f * sunColorMultiplier, .35f * sunColorMultiplier, 1);

            color = color * 5;

            if (MyEnvironment.SunBillboardEnabled)
                MyBillboardsHelper.AddPointBillboard(MyEnvironment.SunMaterial, color, sunPosition, radius, 0);
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
            float radius = MathHelper.Clamp(glare.Range * distance / 100.0f, minGlareRadius, maxGlareRadius);

            float drawingRadius = radius;

            var startFadeout = 1000;
            var endFadeout = 1100;

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
            MyLights.PointlightsBvh.OverlapAllFrustum(ref MyEnvironment.ViewFrustum, VisiblePointlights);

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

            var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(16));
            mapping.stream.Write(activePointlights);
            mapping.stream.Write(0f);
            mapping.stream.Write(0f);
            mapping.stream.Write(0f);
            mapping.Unmap();

            mapping = MyMapping.MapDiscard(m_pointlightCullHwBuffer.Buffer);
            for (int i = 0; i < activePointlights; i++)
            {
                mapping.stream.Write(m_pointlightsCullBuffer[i]);
            }
            for (int i = activePointlights; i < MyRender11Constants.MAX_POINT_LIGHTS; i++)
            {
                mapping.stream.Write(new MyPointlightConstants());
            }
            mapping.Unmap();

            RC.CSSetCB(0, MyCommon.FrameConstants);
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            //RC.BindUAV(0, MyScreenDependants.m_test);
            RC.BindUAV(0, MyScreenDependants.m_tileIndexes);
            RC.BindGBufferForRead(0, MyGBuffer.Main);
            RC.CSBindRawSRV(MyCommon.POINTLIGHT_SLOT, m_pointlightCullHwBuffer.Srv);
            RC.SetCS(m_preparePointLights);

            var size = MyRender11.ViewportResolution;
            RC.Context.Dispatch((size.X + TILE_SIZE - 1) / TILE_SIZE, (size.Y + TILE_SIZE - 1) / TILE_SIZE, 1);
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

            RC.SetRS(MyRender11.m_nocullRasterizerState);

            var cb = MyCommon.GetObjectCB(sizeof(SpotlightConstants));
            RC.SetCB(1, cb);
            RC.Context.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);

            int index = 0;
            int casterIndex = 0;

            foreach(var id in VisibleSpotlights)
            {
                var mapping = MyMapping.MapDiscard(cb);
                mapping.stream.Write(MyLightRendering.Spotlights[index]);
                mapping.Unmap();

                RC.Context.PixelShader.SetShaderResource(13, MyTextures.GetView(MyLights.Spotlights[id.Index].ReflectorTexture));

                if(id.CastsShadows)
                {
                    RC.Context.PixelShader.SetShaderResource(14, MyShadows.ShadowmapsPool[casterIndex].ShaderView);
                    casterIndex++;
                }

                RC.SetPS(SpotlightPs_Pixel);
                if (MyRender11.MultisamplingEnabled)
                {
                    RC.SetDS(MyDepthStencilState.TestAAEdge, 0);
                }
                RC.Context.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);

                if (MyRender11.MultisamplingEnabled)
                {
                    RC.SetPS(SpotlightPs_Sample);
                    RC.SetDS(MyDepthStencilState.TestAAEdge, 0x80);
                    RC.Context.DrawIndexed(MyMeshes.GetLodMesh(coneMesh, 0).Info.IndicesNum, 0, 0);
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

            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
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
            MySunlightConstantsLayout constants;
            constants.Direction = MyEnvironment.DirectionalLightDir;
            constants.Color = MyEnvironment.DirectionalLightIntensity;

            var mapping = MyMapping.MapDiscard(m_sunlightConstants);
            mapping.stream.Write(constants);
            mapping.Unmap();

            //context.VertexShader.Set(MyCommon.FullscreenShader.VertexShader);
            RC.SetPS(DirectionalEnvironmentLight_Pixel);
            RC.SetCB(1, m_sunlightConstants);
            RC.SetCB(4, MyShadows.m_csmConstants);
            RC.Context.PixelShader.SetSamplers(0, MyRender11.StandardSamplers);
            RC.Context.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);
            RC.Context.PixelShader.SetShaderResource(MyCommon.CASCADES_SM_SLOT, MyShadows.m_cascadeShadowmapArray.ShaderView);

            var z = Vector4.Transform(new Vector4(0.5f, 0.5f, -MyEnvironment.FarClipping, 1), MyEnvironment.Projection);


            RC.Context.PixelShader.SetShaderResource(MyCommon.SKYBOX_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.DaySkybox, MyTextureEnum.CUBEMAP, true)));
            //RC.Context.PixelShader.SetShaderResource(MyCommon.SKYBOX_IBL_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.DaySkyboxPrefiltered, MyTextureEnum.CUBEMAP, true)));
            
            RC.Context.PixelShader.SetShaderResource(MyCommon.SKYBOX_IBL_SLOT,
                MyRender11.IsIntelBrokenCubemapsWorkaround ? MyTextures.GetView(MyTextures.IntelFallbackCubeTexId) : MyGeometryRenderer.m_envProbe.cubemapPrefiltered.ShaderView);
            RC.Context.PixelShader.SetShaderResource(MyCommon.SKYBOX2_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.NightSkybox, MyTextureEnum.CUBEMAP, true)));
            RC.Context.PixelShader.SetShaderResource(MyCommon.SKYBOX2_IBL_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.NightSkyboxPrefiltered, MyTextureEnum.CUBEMAP, true)));

            RC.Context.PixelShader.SetShaderResource(MyCommon.SHADOW_SLOT, MyRender11.m_shadowsHelper.ShaderView);

            RC.Context.PixelShader.SetShaderResource(MyCommon.AMBIENT_BRDF_LUT_SLOT,
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
