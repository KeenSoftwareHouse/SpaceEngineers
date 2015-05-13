using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D9;
using VRageRender.Effects;
using VRageRender.Graphics;

namespace VRageRender
{
    class MyPostProcessVignetting : MyPostProcessBase
    {
        public float VignettingPower;

        public MyPostProcessVignetting(bool enabled)
            : base(enabled)
        {
        }


        public override MyPostProcessEnum Name
        {
            get { return MyPostProcessEnum.Vignetting; }
        }

        public override string DisplayName
        {
            get { return "Vignetting"; }
        }

        public override Texture Render(PostProcessStage postProcessStage, Texture source, Texture availableRenderTarget)
        {
            switch (postProcessStage)
            {
                case PostProcessStage.AlphaBlended:
                {
                    //BlendState.Opaque.Apply();
                    //DepthStencilState.None.Apply();
                    //RasterizerState.CullCounterClockwise.Apply();

                    //MyRender.SetRenderTarget(availableRenderTarget, null);

                    //MyEffectVignetting effectVignetting = MyRender.GetEffect(MyEffects.Vignetting) as MyEffectVignetting;
                    //effectVignetting.SetInputTexture(source);
                    //effectVignetting.SetHalfPixel(source.GetLevelDescription(0).Width, source.GetLevelDescription(0).Height);
                    //effectVignetting.SetVignettingPower(VignettingPower);

                    //effectVignetting.EnableVignetting();

                    //MyRender.GetFullscreenQuad().Draw(effectVignetting);
                    return availableRenderTarget;
                }
            }
            return source;
        }
    }
}
