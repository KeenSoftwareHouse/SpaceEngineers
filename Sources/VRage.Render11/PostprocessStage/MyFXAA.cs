using VRage.Render11.Common;
using VRage.Render11.Resources;

namespace VRageRender
{
    class MyFXAA : MyImmediateRC
    {
        static PixelShaderId m_ps;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("Postprocess/Fxaa.hlsl");
        }

        internal static void Run(IRtvBindable destination, ISrvBindable source)
        {
            RC.SetBlendState(null);

            RC.SetInputLayout(null);
            RC.PixelShader.Set(m_ps);

            RC.SetRtv(destination);
            RC.PixelShader.SetSrv(0, source);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(destination.Size.X, destination.Size.Y));
        }
    }
}