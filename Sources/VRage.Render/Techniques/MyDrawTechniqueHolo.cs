using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Effects;
using VRageRender.Graphics;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueHolo : MyDrawTechniqueBaseDNS
    {
        public MyDrawTechniqueHolo()
        {
            SolidRasterizerState = RasterizerState.CullNone;
            WireframeRasterizerState = RasterizerState.CullNone;
        }

        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = (MyEffectModelsDNS)MyRender.GetEffect(MyEffects.ModelDNS);
            SetupBaseEffect(shader, setup, lodType);

            MyEffectModelsDNS dnsShader = shader as MyEffectModelsDNS;

            dnsShader.SetHalfPixel(MyRenderCamera.Viewport.Width, MyRenderCamera.Viewport.Height);
            dnsShader.SetScale(MyRender.GetScaleForViewport(MyRender.GetRenderTarget(MyRenderTargets.Depth)));

            bool useDepth = lodType != MyLodTypeEnum.LOD_NEAR;

            if (useDepth)
            {
                // DepthStencilState.DepthRead;
                MyStateObjects.DepthStencil_TestFarObject_DepthReadOnly.Apply();
            }
            else
            {
                DepthStencilState.DepthRead.Apply();
            }
            MyStateObjects.Holo_BlendState.Apply();

            shader.ApplyHolo(!useDepth);
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }
    }
}
