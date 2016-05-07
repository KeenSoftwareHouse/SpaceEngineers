#region Using

using SharpDX.Direct3D9;
using VRageRender.Effects;
using VRageRender.Utils;

#endregion

namespace VRageRender
{
    class MyPostProcessContrast : MyPostProcessBase
    {

        /// <summary>
        /// Name of the post process
        /// </summary>
        public override MyPostProcessEnum Name { get { return MyPostProcessEnum.Contrast; } }
        public override string DisplayName { get { return "Contrast"; } }

        public float Contrast = 1.0f;
        public float Hue = 0.0f;
        public float Saturation = 0.0f;

        public MyPostProcessContrast(bool enabled)
            : base(enabled)
        {
        }

        /// <summary>
        /// Render method is called directly by renderer. Depending on stage, post process can do various things 
        /// </summary>
        /// <param name="postProcessStage">Stage indicating in which part renderer currently is.</param>public override void RenderAfterBlendLights()
        public override Texture Render(PostProcessStage postProcessStage, Texture source, Texture availableRenderTarget)
        {
            switch (postProcessStage)
            {
                case PostProcessStage.AlphaBlended:
                    {
                        MyRender.SetRenderTarget(availableRenderTarget, null);
                        MyEffectContrast effectContrast = MyRender.GetEffect(MyEffects.Contrast) as MyEffectContrast;


                        effectContrast.SetDiffuseTexture(source);
                        effectContrast.SetHalfPixel(MyUtilsRender9.GetHalfPixel(source.GetLevelDescription(0).Width, source.GetLevelDescription(0).Height));
                        effectContrast.SetContrast(Contrast);
                        effectContrast.SetHue(Hue);
                        effectContrast.SetSaturation(Saturation);

                        MyRender.GetFullscreenQuad().Draw(effectContrast);

                        return availableRenderTarget;
                    }
            }

            return source;
        }
    }
}
