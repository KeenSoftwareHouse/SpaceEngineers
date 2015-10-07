using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    class MyEnvProbeProcessing : MyScreenPass
    {
        static PixelShaderId m_ps;
        static ComputeShaderId m_mipmap;
        static ComputeShaderId m_prefilter;
        static ComputeShaderId m_blend;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("ForwardPostprocess.hlsl", "apply_skybox");
            m_mipmap = MyShaders.CreateCs("EnvPrefiltering.hlsl", "buildMipmap");
            m_prefilter = MyShaders.CreateCs("EnvPrefiltering.hlsl", "prefilter");
            m_blend = MyShaders.CreateCs("EnvPrefiltering.hlsl", "blend");
        }

        internal unsafe static void RunForwardPostprocess(RenderTargetView rt, ShaderResourceView depth, ref Matrix viewMatrix, uint? atmosphereId)
        {
            var mapping = MyMapping.MapDiscard(RC.Context, MyCommon.ProjectionConstants);
            mapping.stream.Write(Matrix.Transpose(viewMatrix));
            mapping.Unmap();

            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

            RC.Context.OutputMerger.SetTargets(rt);
            RC.Context.PixelShader.SetShaderResource(0, depth);
            RC.Context.PixelShader.SetShaderResource(MyCommon.SKYBOX_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.DaySkybox, MyTextureEnum.CUBEMAP, true)));
            RC.Context.PixelShader.SetShaderResource(MyCommon.SKYBOX2_SLOT, MyTextures.GetView(MyTextures.GetTexture(MyEnvironment.NightSkybox, MyTextureEnum.CUBEMAP, true)));
            RC.Context.PixelShader.SetSamplers(0, MyRender11.StandardSamplers);
            RC.Context.PixelShader.Set(m_ps);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(256, 256));

            RC.Context.OutputMerger.SetTargets(null as RenderTargetView);

        }

        internal static void BuildMipmaps(RwTexId texture)
        {
            RC.Context.ComputeShader.Set(m_mipmap);

            var mipLevels = texture.Description2d.MipLevels;
            var side = texture.Description2d.Width;
            for (int j = 0; j < 6; ++j)
            {
                var mipSide = side;
                for (int i = 1; i < mipLevels; ++i)
                {
                    RC.Context.ComputeShader.SetUnorderedAccessView(0, texture.SubresourceUav(j, i));
                    RC.Context.ComputeShader.SetShaderResource(0, texture.SubresourceSrv(j, i - 1));
                    RC.Context.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);
                    RC.Context.ComputeShader.SetShaderResource(0, null);

                    mipSide >>= 1;
                }
            }

            RC.Context.ComputeShader.SetUnorderedAccessView(0, null);
            RC.Context.ComputeShader.Set(null);
        }

        internal static void Prefilter(RwTexId probe, RwTexId prefiltered)
        {
            RC.Context.ComputeShader.Set(m_prefilter);

            var mipLevels = prefiltered.Description2d.MipLevels;
            var side = prefiltered.Description2d.Width;
            var probeSide = probe.Description2d.Width;
            
            RC.CSSetCB(1, MyCommon.GetObjectCB(32));

            RC.Context.ComputeShader.SetShaderResource(0, probe.ShaderView);
            RC.Context.ComputeShader.SetSamplers(0, MyRender11.StandardSamplers);

            for (int j = 0; j < 6; ++j)
            {
                var mipSide = side;
                for (int i = 0; i < mipLevels; ++i)
                {
                    uint samplesNum = i == 0 ? 1u : 64u;

                    var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(32));
                    mapping.stream.Write((uint)samplesNum);
                    mapping.stream.Write((uint)probeSide);
                    mapping.stream.Write((uint)mipSide);
                    mapping.stream.Write((uint) j);
                    mapping.stream.Write(1 - (i / (float)(mipLevels - 1)) );
                    mapping.Unmap();

                    RC.Context.ComputeShader.SetUnorderedAccessView(0, prefiltered.SubresourceUav(j, i));
                    RC.Context.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);

                    mipSide >>= 1;
                }
            }

            RC.Context.ComputeShader.SetUnorderedAccessView(0, null);
            RC.Context.ComputeShader.SetShaderResource(0, null);

            RC.Context.ComputeShader.Set(null);
        }

        internal static void Blend(RwTexId dst, RwTexId src0, RwTexId src1, float w)
        {
            //MyImmediateRC.RC.Context.CopyResource(src1.Resource, dst.Resource);

            RC.Context.ComputeShader.Set(m_blend);

            var mipLevels = dst.Description2d.MipLevels;
            var side = dst.Description2d.Width;

            RC.CSSetCB(1, MyCommon.GetObjectCB(32));

            RC.Context.ComputeShader.SetSamplers(0, MyRender11.StandardSamplers);

            for (int j = 0; j < 6; ++j)
            {
                var mipSide = side;
                for (int i = 0; i < mipLevels; ++i)
                {
                    uint samplesNum = i == 0 ? 1u : 64u;

                    var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(32));
                    mapping.stream.Write((uint)0);
                    mapping.stream.Write((uint)0);
                    mapping.stream.Write((uint)mipSide);
                    mapping.stream.Write((uint)0);
                    mapping.stream.Write(w);
                    mapping.Unmap();

                    RC.Context.ComputeShader.SetShaderResources(0, src0.SubresourceSrv(j, i), src1.SubresourceSrv(j, i));
                    RC.Context.ComputeShader.SetUnorderedAccessView(0, dst.SubresourceUav(j, i));
                    RC.Context.Dispatch((mipSide + 7) / 8, (mipSide + 7) / 8, 1);

                    mipSide >>= 1;
                }
            }

            RC.Context.ComputeShader.SetUnorderedAccessView(0, null);
            RC.Context.ComputeShader.SetShaderResources(0, null, null);

            RC.Context.ComputeShader.Set(null);
        }
    }
}
