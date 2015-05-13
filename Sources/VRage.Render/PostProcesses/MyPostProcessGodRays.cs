#region Using

using SharpDX.Direct3D9;
using VRageMath;
using VRageRender.Effects;
using VRageRender.Graphics;

#endregion

namespace VRageRender
{
    class MyPostProcessGodRays : MyPostProcessBase
    {
        public MyPostProcessGodRays(bool enabled): 
            base(enabled)
        {
            ApplyBlur = false;
        }

        /// <summary>
        /// Name of the post process
        /// </summary>
        public override MyPostProcessEnum Name { get { return MyPostProcessEnum.GodRays; } }
        public override string DisplayName { get { return "GodRays"; } }

        public float Density = 0.34f;  //0.097
        public float Weight = 1.27f;   //0.522
        public float Decay = 0.97f;    //0.992
        public float Exposition = 0.077f; //0.343

        public bool ApplyBlur;

        /// <summary>
        /// Enable state of post process
        /// </summary>
        public override bool Enabled
        {
            get
            {
                //return base.Enabled && (MyRenderConstants.RenderQualityProfile.EnableGodRays && MySector.GodRaysProperties.Enabled);
                return base.Enabled && MyRenderConstants.RenderQualityProfile.EnableGodRays;
            }
            set
            {
                base.Enabled = value;
            }
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
                                  
                        var halfRT = MyRender.GetRenderTarget(MyRenderTargets.AuxiliaryHalf0);

                        MyRender.SetRenderTarget(halfRT, null);
                        BlendState.Opaque.Apply();
                        RasterizerState.CullNone.Apply();
                        DepthStencilState.None.Apply();
                        
                        MyEffectGodRays effectGodRays = MyRender.GetEffect(MyEffects.GodRays) as MyEffectGodRays;

                        effectGodRays.SetDiffuseTexture(source);
                        effectGodRays.SetDepthTexture(MyRender.GetRenderTarget(MyRenderTargets.Depth));
                        effectGodRays.SetFrustumCorners(MyRender.GetShadowRenderer().GetFrustumCorners());
                        effectGodRays.SetView((Matrix)MyRenderCamera.ViewMatrix);
                        effectGodRays.SetWorldViewProjection((Matrix)MyRenderCamera.ViewProjectionMatrix);
                        effectGodRays.SetDensity(Density);
                        effectGodRays.SetDecay(Decay);
                        
                        effectGodRays.SetWeight(Weight * (1 - MyRender.FogProperties.FogMultiplier));
                        effectGodRays.SetExposition(Exposition);
                        effectGodRays.SetLightPosition(15000f * -MyRender.Sun.Direction);
                        effectGodRays.SetLightDirection(MyRender.Sun.Direction);
                        effectGodRays.SetCameraPos((Vector3)MyRenderCamera.Position);

                        MyRender.GetFullscreenQuad().Draw(effectGodRays);
                        
                        if (ApplyBlur)
                        {
                            var auxTarget = MyRender.GetRenderTarget(MyRenderTargets.AuxiliaryHalf1010102);

                            var blurEffect = MyRender.GetEffect(MyEffects.GaussianBlur) as MyEffectGaussianBlur;
                            blurEffect.SetHalfPixel(halfRT.GetLevelDescription(0).Width, halfRT.GetLevelDescription(0).Height);

                            // Apply vertical gaussian blur
                            MyRender.SetRenderTarget(auxTarget, null);
                            blurEffect.BlurAmount = 1;
                            blurEffect.SetSourceTexture(halfRT);
                            blurEffect.SetHeightForVerticalPass(halfRT.GetLevelDescription(0).Height);
                            MyRender.GetFullscreenQuad().Draw(blurEffect);

                            // Apply horizontal gaussian blur
                            MyRender.SetRenderTarget(halfRT, null);
                            blurEffect.BlurAmount = 1;
                            blurEffect.SetSourceTexture(auxTarget);
                            blurEffect.SetWidthForHorisontalPass(auxTarget.GetLevelDescription(0).Width);
                            MyRender.GetFullscreenQuad().Draw(blurEffect);
                        }
                                
                        // Additive
                        MyRender.SetRenderTarget(availableRenderTarget, null);
                        //MySandboxGame.Static.GraphicsDevice.Clear(ClearFlags.All, new SharpDX.ColorBGRA(0), 1, 0);
                        BlendState.Opaque.Apply();
                        MyRender.Blit(source, true);

                        var upscaleEffect = MyRender.GetEffect(MyEffects.Scale) as MyEffectScale;
                        upscaleEffect.SetScale(new Vector2(2));
                        upscaleEffect.SetTechnique(MyEffectScale.Technique.HWScale);
                        MyStateObjects.Additive_NoAlphaWrite_BlendState.Apply();

                        upscaleEffect.SetSourceTextureMod(halfRT);
                        MyRender.GetFullscreenQuad().Draw(upscaleEffect);
                                     
                        return availableRenderTarget;
                    }
            }

            return source;
        }
    }
}
