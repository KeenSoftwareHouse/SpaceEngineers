using SharpDX;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRageRender;
using Format = SharpDX.DXGI.Format;

namespace VRage.Render11.Resources
{
    internal class MyScreenDependants
    {
        internal static IDepthStencil m_resolvedDepth;
        internal static IRtvTexture m_ambientOcclusion;
        internal static IRtvTexture m_ambientOcclusionHelper;

        internal static MyRWStructuredBuffer m_tileIndices;

        internal static int TilesNum;
        internal static int TilesX;
        internal static int TilesY;

        internal static void Resize(int width, int height, int samplesNum, int samplesQuality)
        {
            MyDepthStencilManager dsManager = MyManagers.DepthStencils;
            dsManager.DisposeTex(ref m_resolvedDepth);
            m_resolvedDepth = dsManager.CreateDepthStencil("MyScreenDependants.ResolvedDepth", width, height);

            MyRwTextureManager texManager = MyManagers.RwTextures; 
            texManager.DisposeTex(ref m_ambientOcclusionHelper);
            m_ambientOcclusionHelper = texManager.CreateRtv("MyScreenDependants.AmbientOcclusionHelper", width, height, Format.R8_UNorm, 1, 0);
            texManager.DisposeTex(ref m_ambientOcclusion);
            m_ambientOcclusion = texManager.CreateRtv("MyScreenDependants.AmbientOcclusion", width, height, Format.R8_UNorm, 1, 0);
            
            TilesX = (width + MyLightRendering.TILE_SIZE - 1) / MyLightRendering.TILE_SIZE;
            TilesY = ((height + MyLightRendering.TILE_SIZE - 1) / MyLightRendering.TILE_SIZE);
            TilesNum = TilesX * TilesY;
            if (m_tileIndices != null)
                m_tileIndices.Release();
            m_tileIndices = new MyRWStructuredBuffer(TilesNum + TilesNum * MyRender11Constants.MAX_POINT_LIGHTS, sizeof(uint), MyRWStructuredBuffer.UavType.Default, true, "MyScreenDependants::tileIndices");
        }
    }

    internal class MyGBuffer: MyImmediateRC
    {
        internal const Format LBufferFormat = Format.R11G11B10_Float;

        IDepthStencil m_depthStencil;
        IRtvTexture m_gbuffer0;
        IRtvTexture m_gbuffer1;
        IRtvTexture m_gbuffer2;
        IRtvTexture m_lbuffer;

        int m_samplesCount;
        int m_samplesQuality;

        public int SamplesCount { get { return m_samplesCount; } }
        public int SamplesQuality { get { return m_samplesQuality; } }

        internal void Resize(int width, int height, int samplesNum, int samplesQuality)
        {
            Release();

            m_samplesCount = samplesNum;
            m_samplesQuality = samplesQuality;

            MyDepthStencilManager dsManager = MyManagers.DepthStencils;
            m_depthStencil = dsManager.CreateDepthStencil("MyGBuffer.DepthStencil", width, height, samplesCount: samplesNum, samplesQuality: samplesQuality);

            MyRwTextureManager rwManager = MyManagers.RwTextures;
            m_gbuffer0 = rwManager.CreateRtv("MyGBuffer.GBuffer0", width, height, Format.R8G8B8A8_UNorm_SRgb, 
                samplesNum, samplesQuality);
            m_gbuffer1 = rwManager.CreateRtv("MyGBuffer.GBuffer1", width, height, Format.R10G10B10A2_UNorm,
                samplesNum, samplesQuality);
            m_gbuffer2 = rwManager.CreateRtv("MyGBuffer.GBuffer2", width, height, Format.R8G8B8A8_UNorm,
                samplesNum, samplesQuality);
            m_lbuffer = rwManager.CreateRtv("MyGBuffer.LBuffer", width, height, LBufferFormat,
                samplesNum, samplesQuality);
        }

        internal void Release()
        {
            MyDepthStencilManager dsManager = MyManagers.DepthStencils;
            MyRwTextureManager rwManager = MyManagers.RwTextures;

            dsManager.DisposeTex(ref m_depthStencil);
            rwManager.DisposeTex(ref m_gbuffer0);
            rwManager.DisposeTex(ref m_gbuffer1);
            rwManager.DisposeTex(ref m_gbuffer2);
            rwManager.DisposeTex(ref m_lbuffer);
        }

        internal IDepthStencil DepthStencil { get { return m_depthStencil; } }
        internal IRtvTexture GBuffer0 { get { return m_gbuffer0; } }
        internal IRtvTexture GBuffer1 { get { return m_gbuffer1; } }
        internal IRtvTexture GBuffer2 { get { return m_gbuffer2; } }
        internal IRtvTexture LBuffer { get { return m_lbuffer; } }

        internal void Clear(VRageMath.Color clearColor)
        {
            RC.ClearDsv(DepthStencil,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, MyRender11.DepthClearValue, 0);

			var v3 = clearColor.ToVector3();
            RC.ClearRtv(m_gbuffer0, new Color4(v3.X, v3.Y, v3.Z, 1));
            RC.ClearRtv(m_gbuffer1, Color4.Black);
            RC.ClearRtv(m_gbuffer2, Color4.Black);
            RC.ClearRtv(m_lbuffer, Color4.Black);
    
        }

        internal static MyGBuffer Main;
    }
}
