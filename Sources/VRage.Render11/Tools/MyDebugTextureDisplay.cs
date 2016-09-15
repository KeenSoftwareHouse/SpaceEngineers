using SharpDX.Direct3D;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageRender;

namespace VRage.Render11.Tools
{
    internal static class MyDebugTextureDisplay
    {
        static PixelShaderId m_ps = PixelShaderId.NULL;

        static IRtvTexture m_selRtvTexture;
        static IUavTexture m_selUavTexture;
        static IBorrowedRtvTexture m_selBorrowedRtvTexture;
        static IBorrowedUavTexture m_selBorrowedUavTexture;

        public static void Deselect()
        {
            m_selRtvTexture = null;
            m_selUavTexture = null;
            if (m_selBorrowedRtvTexture != null)
            {
                m_selBorrowedRtvTexture.Release();
                m_selBorrowedRtvTexture = null;
            }
            if (m_selBorrowedUavTexture != null)
            {
                m_selBorrowedUavTexture.Release();
                m_selBorrowedUavTexture = null;
            }
        }

        public static void Select(IRtvTexture tex)
        {
            Deselect();
            m_selRtvTexture = tex;
        }

        public static void Select(IUavTexture tex)
        {
            Deselect();
            m_selUavTexture = tex;
        }

        public static void Select(IBorrowedRtvTexture tex)
        {
            Deselect();
            tex.AddRef();
            m_selBorrowedRtvTexture = tex;
        }

        public static void Select(IBorrowedUavTexture tex)
        {
            Deselect();
            tex.AddRef();
            m_selBorrowedUavTexture = tex;
        }

        public static void Draw(IRtvBindable renderTarget)
        {
            ISrvBindable srvBind = null;
            if (m_selRtvTexture != null)
                srvBind = m_selRtvTexture;
            if (m_selUavTexture != null)
                srvBind = m_selUavTexture;
            if (m_selBorrowedRtvTexture != null)
                srvBind = m_selBorrowedRtvTexture;
            if (m_selBorrowedUavTexture != null)
                srvBind = m_selBorrowedUavTexture;

            if (srvBind == null) // no texture is selected
                return;

            if (m_ps == PixelShaderId.NULL)
                m_ps = MyShaders.CreatePs("Debug/DebugRt.hlsl");

            MyRenderContext RC = MyImmediateRC.RC;
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            RC.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            RC.SetRtv(renderTarget);
            RC.SetBlendState(null);
            RC.PixelShader.Set(m_ps);
            RC.PixelShader.SetSrv(0, srvBind);
            RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            MyScreenPass.DrawFullscreenQuad();

            Deselect();
        }
    }
}
