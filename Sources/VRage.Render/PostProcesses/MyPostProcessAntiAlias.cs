#region Using

using SharpDX.Direct3D9;
using VRageRender.Graphics;
using VRageRender.Effects;

#endregion

namespace VRageRender
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  Antialiasing
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class MyPostProcessAntiAlias : MyPostProcessBase
    {

        /// <summary>
        /// Name of the post process
        /// </summary>
        public override MyPostProcessEnum Name { get { return MyPostProcessEnum.FXAA; } }
        public override string DisplayName { get { return "FXAA"; } }

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
                        BlendState.Opaque.Apply();
                        DepthStencilState.None.Apply();
                        RasterizerState.CullCounterClockwise.Apply();

                        MyRender.SetRenderTarget(availableRenderTarget, null);

                        MyEffectAntiAlias effectAntiAlias = MyRender.GetEffect(MyEffects.AntiAlias) as MyEffectAntiAlias;
                        effectAntiAlias.SetDiffuseTexture(source);
                        effectAntiAlias.SetHalfPixel(source.GetLevelDescription(0).Width, source.GetLevelDescription(0).Height);

                        if (MyRenderConstants.RenderQualityProfile.EnableFXAA)
                            effectAntiAlias.ApplyFxaa();
                        else
                            return source; // Nothing to do, return source

                        MyRender.GetFullscreenQuad().Draw(effectAntiAlias);
                        return availableRenderTarget;
                    }
            }
            return source;
        }
    }
}
