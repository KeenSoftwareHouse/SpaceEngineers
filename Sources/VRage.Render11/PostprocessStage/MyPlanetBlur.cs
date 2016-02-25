using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageRender
{
    class MyPlanetBlur : MyScreenPass
    {
        internal static MyPlanetBlurSettings Settings;

        static PixelShaderId m_psH;
        static PixelShaderId m_psV;

        static ConstantsBufferId m_cb;

        internal static void RecreateShadersForSettings()
        {
            m_psH = MyShaders.CreatePs("blur_planet_h.hlsl");
            m_psV = MyShaders.CreatePs("blur_planet_v.hlsl");
            m_cb = MyCommon.GetObjectCB(16);
        }

        internal static void Run(MyBindableResource dst1, MyBindableResource dst2, MyBindableResource lightBuffer, MyGBuffer gbuffer)
        {
            if (!Settings.BlurEnabled || Settings.BlurAmount < 0.01f)
            {
                return;
            }

            RC.DeviceContext.ClearRenderTargetView((dst1 as IRenderTargetBindable).RTV, new SharpDX.Color4(0, 0, 0, 0));
            RC.DeviceContext.ClearRenderTargetView((dst2 as IRenderTargetBindable).RTV, new SharpDX.Color4(0, 0, 0, 0));

            float zero = 0f;
            var mapping = MyMapping.MapDiscard(m_cb);
            mapping.WriteAndPosition(ref Settings.BlurAmount);
            mapping.WriteAndPosition(ref Settings.BlurDistance);
            mapping.WriteAndPosition(ref Settings.BlurTransitionRatio);
            mapping.WriteAndPosition(ref zero);
            mapping.Unmap();

            RC.SetCB(0, MyCommon.FrameConstants);
            RC.SetCB(1, m_cb);
            

            RC.SetPS(m_psH);
            RC.BindDepthRT(null, DepthStencilAccess.DepthReadOnly, dst1);
            RC.BindGBufferForRead(0, gbuffer);
            RC.BindSRV(5, lightBuffer);
            DrawFullscreenQuad();


            RC.SetPS(m_psV);
            RC.BindDepthRT(null, DepthStencilAccess.DepthReadOnly, dst2);
            RC.BindSRV(5, dst1);

            DrawFullscreenQuad();

//          MyBlendTargets.Run(lightBuffer, MyScreenDependants.m_planetBlur2, MyRender11.BlendPlanetBlur);
//          MyBlendTargets.Run(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), MyScreenDependants.m_planetBlur2, MyRender11.BlendPlanetBlur);
        }

        internal static void Init()
        {
            Settings = MyPlanetBlurSettings.Defaults();
            MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
        }
    }
}
