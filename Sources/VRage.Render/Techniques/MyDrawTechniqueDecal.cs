using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Effects;
using VRageRender.Graphics;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueDecal : MyDrawTechniqueBaseDNS
    {
        public MyDrawTechniqueDecal()
        {
            SolidRasterizerState = MyStateObjects.BiasedRasterizer_StaticDecals;
            WireframeRasterizerState = MyStateObjects.BiasedRasterizer_StaticDecals;
        }

        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = (MyEffectModelsDNS)MyRender.GetEffect(MyEffects.ModelDNS);
            SetupBaseEffect(shader, setup, lodType);

            MyStateObjects.Static_Decals_BlendState.Apply();
            MyStateObjects.DepthStencil_TestFarObject_DepthReadOnly.Apply();

            shader.BeginBlended();
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }
    }
}
