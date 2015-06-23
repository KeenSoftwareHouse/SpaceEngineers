using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Resources;

namespace VRageRender
{
    class MyCopyToRT : MyImmediateRC
    {
        static PixelShaderId m_copyPs;
        static PixelShaderId m_clearAlphaPs;

        internal static void Init()
        {
            m_copyPs = MyShaders.CreatePs("postprocess.hlsl", "copy");
            m_clearAlphaPs = MyShaders.CreatePs("postprocess.hlsl", "clear_alpha");
        }

        internal static void Run(MyBindableResource destination, MyBindableResource source)
        {
            var context = MyRender11.Context;
        
            context.OutputMerger.BlendState = null;
            //context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

            context.InputAssembler.InputLayout = null;
            context.PixelShader.Set(m_copyPs);
        
            //context.OutputMerger.SetTargets(null as DepthStencilView, target);
            //context.PixelShader.SetShaderResource(0, resource);
        
            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);
            RC.BindSRV(0, source);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(destination.GetSize().X, destination.GetSize().Y));
        }

        internal static void ClearAlpha(MyBindableResource destination)
        {
            var context = MyRender11.Context;

            context.OutputMerger.BlendState = MyRender11.BlendAdditive;

            context.InputAssembler.InputLayout = null;
            context.PixelShader.Set(m_clearAlphaPs);

            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(destination.GetSize().X, destination.GetSize().Y));

            context.OutputMerger.BlendState = null;
        }
    }

    //class MyPostprocess : MyImmediateRC
    //{
    //    internal static MyShader FullscreenShader = MyShaderCache.Create("postprocess.hlsl", "fullscreen", MyShaderProfile.VS_5_0);
    //    static MyShader CopyShader = MyShaderCache.Create("postprocess.hlsl", "copy", MyShaderProfile.PS_5_0);
    //    static MyShader SkyboxShader = MyShaderCache.Create("skybox.hlsl", "skybox", MyShaderProfile.PS_5_0);
    //    static MyShader SSGrassShader = MyShaderCache.Create("ss_grass.hlsl", "ps", MyShaderProfile.PS_5_0);

    //    internal static void DrawFullscreenQuad()
    //    {
    //        var context = MyRender.Context;
    //        context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);
    //        context.VertexShader.Set(FullscreenShader.VertexShader);
    //        context.Draw(3, 0);
    //    }

    //    internal static void Init()
    //    {

    //    }

    //    internal static void Copy(MyBindableResource destination, MyBindableResource source)
    //    {
    //        var context = MyRender.Context;

    //        context.OutputMerger.BlendState = null;
    //        context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

    //        context.InputAssembler.InputLayout = null;
    //        context.VertexShader.Set(FullscreenShader.VertexShader);
    //        context.PixelShader.Set(CopyShader.PixelShader);

    //        //context.OutputMerger.SetTargets(null as DepthStencilView, target);
    //        //context.PixelShader.SetShaderResource(0, resource);

    //        RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);
    //        RC.BindSRV(0, source);

    //        context.Draw(3, 0);
    //    }

    //    internal static void SSGrass(MyBindableResource destination,
    //        MyBindableResource depth, MyBindableResource source, MyBindableResource gbuffer2)
    //    {
    //        var context = MyRender.Context;

    //        var cbuffer = MyCommon.GetObjectBuffer(16);
    //        var mapping = MyMapping.MapDiscard(cbuffer.Buffer);
    //        mapping.stream.Write(MyRender.Settings.GrassPostprocessCloseDistance);
    //        mapping.Unmap();

    //        context.PixelShader.SetConstantBuffer(1, cbuffer.Buffer);

    //        context.OutputMerger.BlendState = null;
    //        context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

    //        context.VertexShader.Set(FullscreenShader.VertexShader);
    //        context.PixelShader.Set(SSGrassShader.PixelShader);

    //        //var array = new ShaderResourceView[] { depth, resource, MyRender.MainGbuffer.Gbuffers[2].ShaderView };
    //        //context.OutputMerger.SetTargets(null as DepthStencilView, target);
    //        //context.PixelShader.SetShaderResources(0, array);

    //        RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);
    //        RC.BindSRV(0, depth, source, gbuffer2);

    //        context.Draw(3, 0);
    //    }

    //    internal static void DrawSkybox(MyBindableResource destination)
    //    {
    //        var context = MyRender.Context;

    //        context.OutputMerger.BlendState = null;
    //        context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

    //        context.PixelShader.SetSamplers(0, MyRender.StandardSamplers);

    //        context.VertexShader.Set(FullscreenShader.VertexShader);
    //        context.PixelShader.Set(SkyboxShader.PixelShader);

    //        //context.OutputMerger.SetTargets(null as DepthStencilView, target);
    //        RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);
    //        RC.BindGBufferForRead(0, MyGBuffer.Main);

    //        //context.PixelShader.SetShaderResources(0, MyRender.MainGbuffer.DepthGbufferViews);
    //        context.PixelShader.SetShaderResource(MyCommon.SKYBOX_SLOT,
    //            MyTextureManager.GetTexture(MyEnvironment.SkyboxTexture).ShaderView);

    //        context.Draw(3, 0);

    //        context.PixelShader.SetShaderResource(0, null);
    //    }
    //}
}
