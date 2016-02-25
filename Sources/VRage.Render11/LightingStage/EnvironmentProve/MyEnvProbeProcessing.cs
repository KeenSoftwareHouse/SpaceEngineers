using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;
using VRageRender.Resources;

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

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("ForwardPostprocess.hlsl");
            m_atmosphere = MyShaders.CreatePs("AtmospherePostprocess.hlsl");
            m_mipmap = MyShaders.CreateCs("EnvPrefiltering_mipmap.hlsl");
            m_prefilter = MyShaders.CreateCs("EnvPrefiltering.hlsl");
            m_blend = MyShaders.CreateCs("EnvPrefiltering_blend.hlsl");
        }

        internal unsafe static void RunForwardPostprocess(RenderTargetView rt, ShaderResourceView depth, ref Matrix viewMatrix, uint? atmosphereId)
        {
            var transpose = Matrix.Transpose(viewMatrix);
            var mapping = MyMapping.MapDiscard(RC.DeviceContext, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref transpose);
            mapping.Unmap();

            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

            RC.DeviceContext.OutputMerger.SetTargets(rt);
            RC.DeviceContext.PixelShader.SetShaderResource(0, depth);
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.DaySkybox, MyTextureEnum.CUBEMAP, true)));
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX2_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.NightSkybox, MyTextureEnum.CUBEMAP, true)));
            RC.DeviceContext.PixelShader.SetSamplers(0, MyRender11.StandardSamplers);
            RC.DeviceContext.PixelShader.Set(m_ps);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(256, 256));

            if (atmosphereId != null)
            {
                var atmosphere = MyAtmosphereRenderer.GetAtmosphere(atmosphereId.Value);
                var constants = new AtmosphereConstants();
                //TODO(AF) These values are computed in MyAtmosphere as well. Find a way to remove the duplication
                var worldMatrix = atmosphere.WorldMatrix;
                worldMatrix.Translation -= MyEnvironment.CameraPosition;

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
            
                mapping = MyMapping.MapDiscard(RC.DeviceContext, cb);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();
            
                RC.SetBS(MyRender11.BlendAdditive);
                RC.SetCB(2, cb);
                RC.DeviceContext.PixelShader.SetShaderResource(2, MyAtmosphereRenderer.GetAtmosphereLuts(atmosphereId.Value).TransmittanceLut.ShaderView);
                RC.DeviceContext.PixelShader.Set(m_atmosphere);

                MyScreenPass.DrawFullscreenQuad(new MyViewport(MyEnvironmentProbe.CubeMapResolution, MyEnvironmentProbe.CubeMapResolution));
            }

            RC.DeviceContext.OutputMerger.SetTargets(null as RenderTargetView);

        }

        internal static void BuildMipmaps(RwTexId texture)
        {
            RC.DeviceContext.ComputeShader.Set(m_mipmap);

            var mipLevels = texture.Description2d.MipLevels;
            var side = texture.Description2d.Width;
            for (int j = 0; j < 6; ++j)
            {
                var mipSide = side;
                for (int i = 1; i < mipLevels; ++i)
                {
                    ComputeShaderId.TmpUav[0] = texture.SubresourceUav(j, i);
                    RC.DeviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
                    RC.DeviceContext.ComputeShader.SetShaderResource(0, texture.SubresourceSrv(j, i - 1));
                    RC.DeviceContext.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);
                    RC.DeviceContext.ComputeShader.SetShaderResource(0, null);

                    mipSide >>= 1;
                }
            }

            ComputeShaderId.TmpUav[0] = null;
            RC.DeviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
            RC.DeviceContext.ComputeShader.Set(null);
        }

        internal static void Prefilter(RwTexId probe, RwTexId prefiltered)
        {
            RC.DeviceContext.ComputeShader.Set(m_prefilter);

            var mipLevels = prefiltered.Description2d.MipLevels;
            var side = prefiltered.Description2d.Width;
            uint probeSide = (uint)probe.Description2d.Width;
            
            ConstantsBufferId constantBuffer = MyCommon.GetObjectCB(32);
            RC.CSSetCB(1, constantBuffer);

            RC.DeviceContext.ComputeShader.SetShaderResource(0, probe.ShaderView);
            RC.DeviceContext.ComputeShader.SetSamplers(0, MyRender11.StandardSamplers);

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

                    ComputeShaderId.TmpUav[0] = prefiltered.SubresourceUav(j, i);
                    RC.DeviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
                    RC.DeviceContext.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);

                    mipSide >>= 1;
                }
            }

            ComputeShaderId.TmpUav[0] = null;
            RC.DeviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
            RC.DeviceContext.ComputeShader.SetShaderResource(0, null);

            RC.DeviceContext.ComputeShader.Set(null);
        }

        private struct MyBlendData
        {
            public uint field0;
            public uint field1;
            public uint MipSide;
            public uint field2;
            public float W;
        }

        internal static void Blend(RwTexId dst, RwTexId src0, RwTexId src1, float blendWeight)
        {
            //MyImmediateRC.RC.Context.CopyResource(src1.Resource, dst.Resource);

            RC.DeviceContext.ComputeShader.Set(m_blend);

            var mipLevels = dst.Description2d.MipLevels;
            var side = dst.Description2d.Width;

            RC.CSSetCB(1, MyCommon.GetObjectCB(32));

            RC.DeviceContext.ComputeShader.SetSamplers(0, MyRender11.StandardSamplers);

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

                    RC.DeviceContext.ComputeShader.SetShaderResource(0, src0.SubresourceSrv(j, i));
                    RC.DeviceContext.ComputeShader.SetShaderResource(1, src1.SubresourceSrv(j, i));

                    // The single parameter version of SetUnorderedAccessView allocates
                    ComputeShaderId.TmpUav[0] = dst.SubresourceUav(j, i);
                    RC.DeviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
                    RC.DeviceContext.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);

                    mipSide >>= 1;
                }
            }

            ComputeShaderId.TmpUav[0] = null;
            RC.DeviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
            RC.DeviceContext.ComputeShader.SetShaderResource(0, null);
            RC.DeviceContext.ComputeShader.SetShaderResource(1, null);

            RC.DeviceContext.ComputeShader.Set(null);
        }
    }
}
