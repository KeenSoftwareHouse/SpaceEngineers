using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    class MyEnvProbeProcessing : MyScreenPass
    {
        struct AtmosphereConstants
        {
            internal Vector3 PlanetCentre;
            internal float AtmosphereRadius;
            internal Vector3 BetaRayleighScattering;
            internal float GroundRadius;
            internal Vector3 BetaMieScattering;
            internal float MieG;
            internal Vector2 HeightScaleRayleighMie;
            internal float PlanetScaleFactor;
            internal float AtmosphereScaleFactor;
            internal float Intensity;
            internal float FogIntensity;
            internal Vector2 __padding;
        }

        static PixelShaderId m_ps;
        static PixelShaderId m_atmosphere;
        static ComputeShaderId m_mipmap;
        static ComputeShaderId m_prefilter;
        static ComputeShaderId m_blend;

        static int m_viewportSize = MyRenderSettings.EnvMapResolution;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("EnvProbe/ForwardPostprocess.hlsl");
            m_atmosphere = MyShaders.CreatePs("EnvProbe/AtmospherePostprocess.hlsl");
            m_mipmap = MyShaders.CreateCs("EnvProbe/EnvPrefilteringMipmap.hlsl");
            m_prefilter = MyShaders.CreateCs("EnvProbe/EnvPrefiltering.hlsl");
            m_blend = MyShaders.CreateCs("EnvProbe/EnvPrefilteringBlend.hlsl");
        }

        internal unsafe static void RunForwardPostprocess(IRtvBindable rt, ISrvBindable depth, ref Matrix viewMatrix, uint? atmosphereId)
        {
            MyGpuProfiler.IC_BeginBlock("Postprocess");
            var transpose = Matrix.Transpose(viewMatrix);
            var mapping = MyMapping.MapDiscard(RC, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref transpose);
            mapping.Unmap();

            RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.AllShaderStages.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

            RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            RC.SetRtv(rt);
            RC.PixelShader.SetSrv(0, depth);
            MyFileTextureManager texManager = MyManagers.FileTextures;
            RC.PixelShader.SetSrv(MyCommon.SKYBOX_SLOT, texManager.GetTexture(MyRender11.Environment.Data.Skybox, MyFileTextureEnum.CUBEMAP, true));
            RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);
            RC.PixelShader.Set(m_ps);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(m_viewportSize, m_viewportSize));
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("Atmosphere");
            if (atmosphereId != null)
            {
                var atmosphere = MyAtmosphereRenderer.GetAtmosphere(atmosphereId.Value);
                var constants = new AtmosphereConstants();
                //TODO(AF) These values are computed in MyAtmosphere as well. Find a way to remove the duplication
                var worldMatrix = atmosphere.WorldMatrix;
                worldMatrix.Translation -= MyRender11.Environment.Matrices.CameraPosition;

                double distance = worldMatrix.Translation.Length();
                double atmosphereTop = atmosphere.AtmosphereRadius * atmosphere.Settings.AtmosphereTopModifier * atmosphere.PlanetScaleFactor * atmosphere.Settings.RayleighTransitionModifier;
                float rayleighHeight = atmosphere.Settings.RayleighHeight;
                float t = 0.0f;
                if (distance > atmosphereTop)
                {
                    if (distance > atmosphereTop * 2.0f)
                    {
                        t = 1.0f;
                    }
                    else
                    {
                        t = (float)((distance - atmosphereTop) / atmosphereTop);
                    }
                }
                rayleighHeight = MathHelper.Lerp(atmosphere.Settings.RayleighHeight, atmosphere.Settings.RayleighHeightSpace, t);


                constants.PlanetCentre = (Vector3)worldMatrix.Translation;
                constants.AtmosphereRadius = atmosphere.AtmosphereRadius * atmosphere.Settings.AtmosphereTopModifier;
                constants.GroundRadius = atmosphere.PlanetRadius * 1.01f * atmosphere.Settings.SeaLevelModifier;
                constants.BetaRayleighScattering = atmosphere.BetaRayleighScattering / atmosphere.Settings.RayleighScattering;
                constants.BetaMieScattering = atmosphere.BetaMieScattering / atmosphere.Settings.MieColorScattering;
                constants.HeightScaleRayleighMie = atmosphere.HeightScaleRayleighMie * new Vector2(rayleighHeight, atmosphere.Settings.MieHeight);
                constants.MieG = atmosphere.Settings.MieG;
                constants.PlanetScaleFactor = atmosphere.PlanetScaleFactor;
                constants.AtmosphereScaleFactor = atmosphere.AtmosphereScaleFactor;
                constants.Intensity = atmosphere.Settings.Intensity;
                constants.FogIntensity = atmosphere.Settings.FogIntensity;
            
                var cb = MyCommon.GetObjectCB(sizeof(AtmosphereConstants));
            
                mapping = MyMapping.MapDiscard(RC, cb);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                RC.SetBlendState(MyBlendStateManager.BlendAdditive);
                RC.PixelShader.SetConstantBuffer(2, cb);
                RC.PixelShader.SetSrv(2, MyAtmosphereRenderer.GetAtmosphereLuts(atmosphereId.Value).TransmittanceLut);
                RC.PixelShader.Set(m_atmosphere);

                MyScreenPass.DrawFullscreenQuad(new MyViewport(MyEnvironmentProbe.CubeMapResolution, MyEnvironmentProbe.CubeMapResolution));
            }
            MyGpuProfiler.IC_EndBlock();

            RC.SetRtv(null);

        }

        internal static void BuildMipmaps(IUavArrayTexture texture)
        {
            RC.ComputeShader.Set(m_mipmap);

            var mipLevels = texture.MipmapLevels;
            var side = texture.Size.X;
            for (int j = 0; j < 6; ++j)
            {
                var mipSide = side;
                for (int i = 1; i < mipLevels; ++i)
                {
                    RC.ComputeShader.SetUav(0, texture.SubresourceUav(j, i), -1);
                    RC.ComputeShader.SetSrv(0, texture.SubresourceSrv(j, i - 1));
                    RC.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);
                    RC.ComputeShader.SetSrv(0, null);

                    mipSide >>= 1;
                }
            }

            RC.ComputeShader.SetUav(0, null, -1);
            RC.ComputeShader.Set(null);
        }

        internal static void Prefilter(IUavArrayTexture probe, IUavArrayTexture prefiltered)
        {
            RC.ComputeShader.Set(m_prefilter);

            var mipLevels = prefiltered.MipmapLevels;
            var side = prefiltered.Size.X;
            uint probeSide = (uint)probe.Size.X;
            
            ConstantsBufferId constantBuffer = MyCommon.GetObjectCB(32);
            RC.ComputeShader.SetConstantBuffer(1, constantBuffer);

            RC.ComputeShader.SetSrv(0, probe);
            RC.ComputeShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

            for (int j = 0; j < 6; ++j)
            {
                int mipSide = side;
                for (int i = 0; i < mipLevels; ++i)
                {
                    uint samplesNum = i == 0 ? 1u : 64u;
                    uint mipSideUint = (uint)mipSide;
                    uint ju = (uint)j;
                    float mipLevelFactor = 1 - (i / (float)(mipLevels - 1));

                    var mapping = MyMapping.MapDiscard(constantBuffer);
                    mapping.WriteAndPosition(ref samplesNum);
                    mapping.WriteAndPosition(ref probeSide);
                    mapping.WriteAndPosition(ref mipSideUint);
                    mapping.WriteAndPosition(ref ju);
                    mapping.WriteAndPosition(ref mipLevelFactor);
                    mapping.Unmap();

                    RC.ComputeShader.SetUav(0, prefiltered.SubresourceUav(j, i));
                    RC.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);

                    mipSide >>= 1;
                }
            }

            RC.ComputeShader.SetUav(0, null);
            RC.ComputeShader.SetSrv(0, null);

            RC.ComputeShader.Set(null);
        }

        private struct MyBlendData
        {
            public uint field0;
            public uint field1;
            public uint MipSide;
            public uint field2;
            public float W;
        }

        internal static void Blend(IUavArrayTexture dst, IUavArrayTexture src0, IUavArrayTexture src1, float blendWeight)
        {
            //MyImmediateRC.RC.Context.CopyResource(src1.Resource, dst.Resource);

            RC.ComputeShader.Set(m_blend);

            var mipLevels = dst.MipmapLevels;
            var side = dst.Size.X;

            RC.ComputeShader.SetConstantBuffer(1, MyCommon.GetObjectCB(32));

            RC.ComputeShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

            for (int j = 0; j < 6; ++j)
            {
                var mipSide = side;
                for (int i = 0; i < mipLevels; ++i)
                {
                    uint samplesNum = i == 0 ? 1u : 64u;

                    var blendConstantData = new MyBlendData { field0 = 0, field1 = 0, MipSide = (uint)mipSide, field2 = 0, W = blendWeight };
                    var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(32));
                    mapping.WriteAndPosition(ref blendConstantData);
                    mapping.Unmap();

                    RC.ComputeShader.SetSrv(0, src0.SubresourceSrv(j, i));
                    RC.ComputeShader.SetSrv(1, src1.SubresourceSrv(j, i));

                    // The single parameter version of SetUnorderedAccessView allocates
                    RC.ComputeShader.SetUav(0, dst.SubresourceUav(j, i), -1);
                    RC.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);

                    mipSide >>= 1;
                }
            }

            RC.ComputeShader.SetUav(0, null);
            RC.ComputeShader.SetSrv(0, null);
            RC.ComputeShader.SetSrv(1, null);

            RC.ComputeShader.Set(null);
        }
    }
}
