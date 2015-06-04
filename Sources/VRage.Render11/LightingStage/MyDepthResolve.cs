namespace VRageRender
{
    class MyDepthResolve : MyScreenPass
    {
        static PixelShaderId m_ps;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("depth_resolve.hlsl", "resolve_depth");
        }

        internal static void Run(MyBindableResource dst, MyBindableResource src)
        {
            RC.SetPS(m_ps);
            RC.BindDepthRT(dst, DepthStencilAccess.ReadWrite, null);
            RC.BindSRV(0, src);
            DrawFullscreenQuad();
        }
    }
}
