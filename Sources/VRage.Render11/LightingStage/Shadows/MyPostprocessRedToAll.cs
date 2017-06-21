using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;

namespace VRage.Render11.LightingStage.Shadows
{
    class MyPostprocessRedToAll : IManagerDevice
    {
        PixelShaderId m_ps = PixelShaderId.NULL;

        unsafe void IManagerDevice.OnDeviceInit()
        {
            if (m_ps == PixelShaderId.NULL)
                m_ps = MyShaders.CreatePs("Shadows\\RedToAll.hlsl");
        }

        void IManagerDevice.OnDeviceReset()
        {
        }

        void IManagerDevice.OnDeviceEnd()
        {
        }

        public void CopyRedToAll(IRtvBindable output, ISrvTexture source)
        {
            MyRenderContext RC = MyRender11.RC;
            RC.SetBlendState(null);
            RC.SetRtv(output);

            RC.PixelShader.Set(m_ps);
            RC.PixelShader.SetSrv(0, source);
            MyScreenPass.DrawFullscreenQuad();

            RC.ResetTargets();
        }
    }
}
