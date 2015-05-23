using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;
using SharpDX;
using SharpDX.Direct3D11;
using VRageRender.Resources;
using Format = SharpDX.DXGI.Format;

namespace VRageRender
{
    enum MyGbufferSlot
    {
        DepthStencil,
        GBuffer0,
        GBuffer1,
        GBuffer2,
        LBuffer,

        DepthResolved,
        LBufferResolved
    }

    static class MyScreenDependants
    {
        internal static MyDepthStencil m_resolvedDepth;
        internal static MyRenderTarget m_particlesRT;
        internal static MyRenderTarget m_ambientOcclusion;
        internal static MyUnorderedAccessTexture m_test;

        internal static MyRWStructuredBuffer m_tileIndexes;

        internal static int TilesNum;
        internal static int TilesX;

        internal static void Resize(int width, int height, int samplesNum, int samplesQuality)
        {
            if (m_resolvedDepth != null) {
                m_resolvedDepth.Release();
                m_particlesRT.Release();
                m_ambientOcclusion.Release();
                m_test.Release();
                m_tileIndexes.Release();
            }

            m_resolvedDepth = new MyDepthStencil(width, height, 1, 0);
            m_particlesRT = new MyRenderTarget(width, height, Format.R16G16B16A16_Float, 1, 0);
            m_ambientOcclusion = new MyRenderTarget(width, height, Format.R8G8B8A8_UNorm, 1, 0);
            m_test = new MyUnorderedAccessTexture(width, height, Format.R8G8B8A8_UNorm);
            
            int tilesNum = ((width + MyLightRendering.TILE_SIZE - 1) / MyLightRendering.TILE_SIZE) * ((height + MyLightRendering.TILE_SIZE - 1) / MyLightRendering.TILE_SIZE);
            TilesNum = tilesNum;
            TilesX = (width + MyLightRendering.TILE_SIZE - 1) / MyLightRendering.TILE_SIZE;
            m_tileIndexes = new MyRWStructuredBuffer(tilesNum + tilesNum * MyRender11Constants.MAX_POINT_LIGHTS, sizeof(uint));
        }
    }

    class MyGBuffer
    {
        internal List<MyHWResource> m_resources = new List<MyHWResource>();

        internal void Resize(int width, int height, int samplesNum, int samplesQuality)
        {
            Release();

            m_resources.Insert((int)MyGbufferSlot.DepthStencil,
                new MyDepthStencil(width, height,
                samplesNum, samplesQuality));
            m_resources.Insert((int)MyGbufferSlot.GBuffer0,
                new MyRenderTarget(width, height, Format.R8G8B8A8_UNorm_SRgb,
                samplesNum, samplesQuality));
            m_resources.Insert((int)MyGbufferSlot.GBuffer1,
                new MyRenderTarget(width, height, Format.R8G8B8A8_UNorm,
                samplesNum, samplesQuality));
            m_resources.Insert((int)MyGbufferSlot.GBuffer2,
                new MyRenderTarget(width, height, Format.R8G8B8A8_UNorm,
                samplesNum, samplesQuality));
            m_resources.Insert((int)MyGbufferSlot.LBuffer,
                new MyRenderTarget(width, height, Format.R11G11B10_Float,
                samplesNum, samplesQuality));

            if (MyRender11.MultisamplingEnabled)
            {
                m_resources.Insert((int)MyGbufferSlot.DepthResolved,
                    new MyDepthStencil(width, height, 1, 0));
                m_resources.Insert((int)MyGbufferSlot.LBufferResolved,
                    new MyUnorderedAccessTexture(width, height, Format.R11G11B10_Float)); // 
            }
        }

        internal void Release()
        {
            foreach(var r in m_resources)
            {
                r.Release();
            }
            m_resources.Clear();
        }

        internal MyBindableResource Get(MyGbufferSlot slot)
        {
            return (MyBindableResource)m_resources[(int)slot];
        }

        internal MyDepthStencil DepthStencil { get { return m_resources[(int)MyGbufferSlot.DepthStencil] as MyDepthStencil; } }

        internal void Clear()
        {
            MyRender11.ImmediateContext.ClearDepthStencilView(DepthStencil.m_DSV,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, MyRender11.DepthClearValue, 0);

            foreach(var res in m_resources)
            {
                var rt = res as IRenderTargetBindable;
                if(rt != null)
                    MyRender11.ImmediateContext.ClearRenderTargetView(rt.RTV, 
                        new Color4(0, 0, 0, 0));

                var uav = res as IUnorderedAccessBindable;
                if (uav != null)
                    MyRender11.ImmediateContext.ClearUnorderedAccessView(uav.UAV, Int4.Zero);
            }
        }

        internal static MyGBuffer Main;
    }

    class MyCubemapRenderer
    {
        internal MyGBuffer m_faceGbuffer;
        
    }
}
