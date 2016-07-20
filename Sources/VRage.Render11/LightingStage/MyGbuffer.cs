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
    }

    static class MyScreenDependants
    {
        internal static MyDepthStencil m_resolvedDepth;
        internal static MyRenderTarget m_ambientOcclusion;
        internal static MyRenderTarget m_ambientOcclusionHelper;

        internal static MyRWStructuredBuffer m_tileIndices;

        internal static int TilesNum;
        internal static int TilesX;
        internal static int TilesY;

        internal static void Resize(int width, int height, int samplesNum, int samplesQuality)
        {
            if (m_resolvedDepth != null) {
                m_resolvedDepth.Release();
                m_ambientOcclusionHelper.Release();
                m_ambientOcclusion.Release();
                m_tileIndices.Release();
            }

            m_resolvedDepth = new MyDepthStencil(width, height, 1, 0);
            m_ambientOcclusionHelper = new MyRenderTarget(width, height, Format.R8G8B8A8_UNorm, 1, 0);
            m_ambientOcclusion = new MyRenderTarget(width, height, Format.R8G8B8A8_UNorm, 1, 0);
            
            TilesX = (width + MyLightRendering.TILE_SIZE - 1) / MyLightRendering.TILE_SIZE;
            TilesY = ((height + MyLightRendering.TILE_SIZE - 1) / MyLightRendering.TILE_SIZE);
            TilesNum = TilesX * TilesY;
            m_tileIndices = new MyRWStructuredBuffer(TilesNum + TilesNum * MyRender11Constants.MAX_POINT_LIGHTS, sizeof(uint), MyRWStructuredBuffer.UAVType.Default, true, "MyScreenDependants::tileIndices");
        }
    }

    class MyGBuffer
    {
        internal const Format LBufferFormat = Format.R11G11B10_Float;
        private readonly List<MyHWResource> m_resources = new List<MyHWResource>();

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
                new MyRenderTarget(width, height, Format.R10G10B10A2_UNorm,
                samplesNum, samplesQuality));
            m_resources.Insert((int)MyGbufferSlot.GBuffer2,
                new MyRenderTarget(width, height, Format.R8G8B8A8_UNorm,
                samplesNum, samplesQuality));
            m_resources.Insert((int)MyGbufferSlot.LBuffer,
                new MyRenderTarget(width, height, LBufferFormat,
                samplesNum, samplesQuality));
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

        internal void Clear(VRageMath.Color clearColor)
        {
            MyRender11.DeviceContext.ClearDepthStencilView(DepthStencil.m_DSV,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, MyRender11.DepthClearValue, 0);

            foreach(var res in m_resources)
            {
                var rt = res as IRenderTargetBindable;
                if(rt != null)
				{
					var v3 = clearColor.ToVector3();
                    MyRender11.DeviceContext.ClearRenderTargetView(rt.RTV, 
						new Color4(v3.X, v3.Y, v3.Z,1
							));
				}

                var uav = res as IUnorderedAccessBindable;
                if (uav != null)
                    MyRender11.DeviceContext.ClearUnorderedAccessView(uav.UAV, Int4.Zero);
            }
        }

        internal static MyGBuffer Main;
    }
}
