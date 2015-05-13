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
    class MyPostProcessColorMapping : MyPostProcessBase
    {
        public MyPostProcessColorMapping(bool enabled)
            : base(enabled)
        {
        }


        public override MyPostProcessEnum Name
        {
            get { return MyPostProcessEnum.ColorMapping; }
        }

        public override string DisplayName
        {
            get { return "ColorMapping"; }
        }

        public override Texture Render(PostProcessStage postProcessStage, Texture source, Texture availableRenderTarget)
        {
            switch (postProcessStage)
            {
                case PostProcessStage.AlphaBlended:
                {
                    BlendState.Opaque.Apply();
                    DepthStencilState.None.Apply();
                    RasterizerState.CullCounterClockwise.Apply();

                    MyRender.SetRenderTarget(availableRenderTarget, null);

                    MyEffectColorMapping effect = MyRender.GetEffect(MyEffects.ColorMapping) as MyEffectColorMapping;
                    effect.SetInputTexture(source);
                    effect.SetHalfPixel(source.GetLevelDescription(0).Width, source.GetLevelDescription(0).Height);
                    effect.Enable();

                    MyRender.GetFullscreenQuad().Draw(effect);
                    return availableRenderTarget;
                }
            }
            return source;
        }
    }
}
