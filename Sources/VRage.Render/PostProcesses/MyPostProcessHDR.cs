#region Using

using System;
using SharpDX.Direct3D9;
using VRageRender.Graphics;
using VRageRender.Effects;

#endregion

namespace VRageRender
{
    class MyPostProcessHDR : MyPostProcessBase
    {
        public override MyPostProcessEnum Name { get { return MyPostProcessEnum.HDR; } }
        public override string DisplayName { get { return "HDR"; } }

        readonly Texture[] m_thresholdTargets;

        public float Exposure { get; set; }
        public float Threshold { get; set; }
        public float BloomIntensity { get; set; }
        public float BloomIntensityBackground { get; set; }
        public float VerticalBlurAmount { get; set; }
        public float HorizontalBlurAmount { get; set; }
        public float NumberOfBlurPasses { get; set; }

        public static bool DebugHDRChecked { get; set; }

        public MyPostProcessHDR()
        {
            DebugHDRChecked = true;

            Exposure = 2.0f;
            Threshold = 1.0f;
            BloomIntensity = 2.0f;
            BloomIntensityBackground = 0.4f;
            VerticalBlurAmount = 2.5f;
            HorizontalBlurAmount = 2.5f;
            NumberOfBlurPasses = 1;

            m_thresholdTargets = new Texture[2];
        }

        /// <summary>
        /// Render method is called directly by renderer. Depending on stage, post process can do various things 
        /// </summary>
        /// <param name="postProcessStage">Stage indicating in which part renderer currently is.</param>public override void RenderAfterBlendLights()
        public override Texture Render(PostProcessStage postProcessStage, Texture source, Texture availableRenderTarget)
        {
            switch (postProcessStage)
            {
                case PostProcessStage.LODBlend:
                    {
                        //if (RenderHDRThisFrame())
                        //{
                        //    (MyRender.GetEffect(MyEffects.BlendLights) as MyEffectBlendLights).CopyEmissivityTechnique = MyEffectBlendLights.Technique.CopyEmissivityHDR;
                        //    (MyRender.GetEffect(MyEffects.BlendLights) as MyEffectBlendLights).DefaultTechnique = MyEffectBlendLights.Technique.HDR;
                        //    (MyRender.GetEffect(MyEffects.DirectionalLight) as MyEffectDirectionalLight).DefaultTechnique = MyEffectDirectionalLight.Technique.DefaultHDR;
                        //    (MyRender.GetEffect(MyEffects.DirectionalLight) as MyEffectDirectionalLight).DefaultWithoutShadowsTechnique = MyEffectDirectionalLight.Technique.WithoutShadowsHDR;
                        //    (MyRender.GetEffect(MyEffects.DirectionalLight) as MyEffectDirectionalLight).DefaultNoLightingTechnique = MyEffectDirectionalLight.Technique.NoLightingHDR;

                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultTechnique = MyEffectPointLight.MyEffectPointLightTechnique.DefaultHDR;
                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultPointTechnique = MyEffectPointLight.MyEffectPointLightTechnique.DefaultHDR;
                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultHemisphereTechnique = MyEffectPointLight.MyEffectPointLightTechnique.DefaultHDR;
                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultReflectorTechnique = MyEffectPointLight.MyEffectPointLightTechnique.ReflectorHDR; // unused, dont have instancing
                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultSpotTechnique = MyEffectPointLight.MyEffectPointLightTechnique.SpotHDR;
                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultSpotShadowTechnique = MyEffectPointLight.MyEffectPointLightTechnique.SpotShadowsHDR;

                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultPointInstancedTechnique = MyEffectPointLight.MyEffectPointLightTechnique.PointHDR_Instanced;
                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultHemisphereInstancedTechnique = MyEffectPointLight.MyEffectPointLightTechnique.HemisphereHDR_Instanced;
                        //    (MyRender.GetEffect(MyEffects.PointLight) as MyEffectPointLight).DefaultSpotInstancedTechnique = MyEffectPointLight.MyEffectPointLightTechnique.SpotHDR_Instanced;
                        //}
                        break;
                    }
                case PostProcessStage.HDR:
                    {
                        // if HDR is disabled or some debug rendering display
                        // is enabled then skip HDR post process
                        if (!RenderHDRThisFrame())
                            return source;

                        BlendState.Opaque.Apply();
                        DepthStencilState.None.Apply();
                       // RasterizerState.CullNone.Apply(MySandboxGameDX.Static.GraphicsDevice);

                        m_thresholdTargets[0] = MyRender.GetRenderTarget(MyRenderTargets.HDRAux);
                        m_thresholdTargets[1] = availableRenderTarget;

                        // 1. threshold
                        GenerateThreshold(
                            source,
                            MyRender.GetRenderTarget(MyRenderTargets.Diffuse),
                            m_thresholdTargets,
                            MyRender.GetEffect(MyEffects.Threshold) as MyEffectThreshold,
                            Threshold, BloomIntensity, BloomIntensityBackground, Exposure);

                                   /*
                        MySandboxGame.SetRenderTarget(null, null,  SetDepthTargetEnum.RestoreDefault);
                        MySandboxGame.SetRenderTarget(availableRenderTarget, null, SetDepthTargetEnum.RestoreDefault);
                        MyEffectScreenshot ssEffect = MyRender.GetEffect(MyEffects.Screenshot) as MyEffectScreenshot;
                        ssEffect.SetSourceTexture(m_thresholdTargets[0]);
                        ssEffect.SetScale(VRageMath.Vector2.One);
                        ssEffect.SetTechnique(MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
                        MyGuiManager.GetFullscreenQuad().Draw(ssEffect);
                        return availableRenderTarget;
                                            */
                                    

                        // 2. downscale HDR1 -> Downscaled8
                        // !! IMPORTANT !! you cannot just switch the function call if you want different downscale
                        // Also changing the RTs is necessary (they have fixed dimensions).
                        GenerateDownscale4(
                            MyRender.GetRenderTarget(MyRenderTargets.HDRAux), // Requires mip mapped RT with mip autogeneration
                            MyRender.GetRenderTarget(MyRenderTargets.Depth),
                            MyRender.GetRenderTarget(MyRenderTargets.HDR4Threshold),
                            MyRender.GetEffect(MyEffects.Scale) as MyEffectScale);

                        /*
                        // 3?. avg luminance
                        float dt = (MySandboxGame.TotalGamePlayTimeInMilliseconds - lastTime) / 1000.0f;
                        lastTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                        CalculateAverageLuminance(
                            MyRender.GetRenderTarget(MyRenderTargets.Downscaled8),
                            MyRender.GetRenderTarget(MyRenderTargets.Downscaled8Threshold),
                            MyRender.GetEffect(MyEffects.Luminance) as MyEffectLuminance,
                            MyRender.GetEffect(MyEffects.Scale) as MyEffectScale,
                            dt, 0.5f);
                        */

                        // 4. blur
                        Blur(MyRender.GetRenderTarget(MyRenderTargets.HDR4Threshold),
                            MyRender.GetRenderTarget(MyRenderTargets.HDR4),
                            MyRender.GetEffect(MyEffects.GaussianBlur) as MyEffectGaussianBlur,
                            VerticalBlurAmount, HorizontalBlurAmount);

                        // 5. scale blurred to halfsize
                        Upscale4To2(
                            MyRender.GetRenderTarget(MyRenderTargets.HDR4Threshold),
                            MyRender.GetRenderTarget(MyRenderTargets.AuxiliaryHalf1010102),
                            MyRender.GetEffect(MyEffects.Scale) as MyEffectScale);

                        MyRender.SetRenderTarget(availableRenderTarget, null, SetDepthTargetEnum.RestoreDefault);

                        // 6. tonemap + apply bloom
                        HDR(
                            source,
                            MyRender.GetRenderTarget(MyRenderTargets.Diffuse),
                            MyRender.GetRenderTarget(MyRenderTargets.AuxiliaryHalf1010102),
                            MyRender.GetEffect(MyEffects.HDR) as MyEffectHDR,
                            0.6f, Exposure);

                        return availableRenderTarget;

                        //RenderTarget2D temp = currentFrameAdaptedLuminance;
                        //currentFrameAdaptedLuminance = lastFrameAdaptedLuminance;
                        //lastFrameAdaptedLuminance = temp;
                    }
            }
            return source;
        }

        public static bool RenderHDR()
        {
            return MyRenderConstants.RenderQualityProfile.EnableHDR && MyRender.SupportsHDR;
        }

        public static bool RenderHDRThisFrame()
        {
            return MyRender.CurrentRenderSetup.EnableLights != null &&
                (MyRender.CurrentRenderSetup.EnableHDR != null &&
                (MyRenderConstants.RenderQualityProfile.EnableHDR &&
                MyRender.SupportsHDR &&
                !MyRender.Settings.ShowBlendedScreens &&
                DebugHDRChecked &&
                MyRender.CurrentRenderSetup.EnableHDR.Value &&
                MyRender.EnableLights &&
                MyRender.Settings.EnableLightsRuntime && 
                MyRender.CurrentRenderSetup.EnableLights.Value &&
                !MyRender.Settings.ShowSpecularIntensity &&
                !MyRender.Settings.ShowSpecularPower &&
                !MyRender.Settings.ShowEmissivity &&
                !MyRender.Settings.ShowReflectivity));
        }

        /*
        private void CalculateAverageLuminance(RenderTarget2D source, RenderTarget2D destination, MyEffectLuminance luminanceEffect, MyEffectScale scalingEffect, float dt, float tau)
        {
            // Calculate the initial luminance
            luminanceEffect.SetTechniqueLuminance();
            PostProcess(source, destination, luminanceEffect);

            //// Repeatedly downscale    
            //scalingEffect.SetTechniqueDownscale();
            //for (int i = 1; i < luminanceChain.Length; i++)
            //{
            //    scalingEffect.SetSourceDimensions(luminanceChain[i - 1].Width, luminanceChain[i - 1].Height);
            //    PostProcess(luminanceChain[i - 1], luminanceChain[i], scalingEffect);
            //}

            //// Final downscale           
            //scalingEffect.SetTechniqueDownscaleLuminance();
            //scalingEffect.SetSourceDimensions(luminanceChain[luminanceChain.Length - 1].Width, luminanceChain[luminanceChain.Length - 1].Height);
            //PostProcess(luminanceChain[luminanceChain.Length - 1], currentFrameLuminance, scalingEffect);

            // Final downscale
            luminanceEffect.SetTechniqueLuminanceMipmap();
            float size = MathHelper.Min(MyCamera.ForwardViewport.Width, MyCamera.ForwardViewport.Height);
            // TODO check if mipmap level is correct
            int mipLevel = (int)Math.Floor(Math.Log(size / 8.0f, 2));
            //int mipLevel = (int)Math.Ceiling(Math.Log(size / 8.0f, 2));
            luminanceEffect.SetMipLevel(mipLevel);
            PostProcess(destination, currentFrameLuminance, luminanceEffect);

            // Adapt the luminance, to simulate slowly adjust exposure
            MySandboxGame.Static.GraphicsDevice.SetRenderTarget(currentFrameAdaptedLuminance);
            luminanceEffect.SetTechniqueAdaptedLuminance();
            luminanceEffect.SetDT(dt);
            luminanceEffect.SetTau(tau);
            luminanceEffect.SetSourceTexture(currentFrameLuminance);
            luminanceEffect.SetSourceTexture2(lastFrameAdaptedLuminance);
            luminanceEffect.SetHalfPixel(source.Width, source.Height);
            MyGuiManager.GetFullscreenQuad().Draw(luminanceEffect);
        }
        */

        private void HDR(Texture sourceMod, Texture sourceDiv, Texture bloomSource, MyEffectHDR effect, float middleGrey, float exposure)
        {
            effect.SetSourceTextureMod(sourceMod);
            effect.SetSourceTextureDiv(sourceDiv);
            effect.SetBloomTexture(bloomSource);
            //effect.SetLumTexture(currentFrameAdaptedLuminance);
            //effect.SetLumTexture(currentFrameLuminance);
            effect.SetHalfPixel(sourceMod.GetLevelDescription(0).Width, sourceMod.GetLevelDescription(0).Height);
            //effect.SetMiddleGrey(middleGrey);
            effect.SetExposure(exposure);

            MyRender.GetFullscreenQuad().Draw(effect);
        }

        private void Upscale8To2(Texture down8, Texture down4, Texture down2, MyEffectScale effect)
        {
            effect.SetTechnique(MyEffectScale.Technique.HWScale);

            PostProcess(down8, down4, effect);

            PostProcess(down4, down2, effect);
        }

        private void Upscale4To2(Texture down4, Texture down2, MyEffectScale effect)
        {
            effect.SetTechnique(MyEffectScale.Technique.HWScale);

            PostProcess(down4, down2, effect);
        }

        private void Blur(Texture sourceAndDestination, Texture aux, MyEffectGaussianBlur effect, float verticalBlurAmount, float horizontalBlurAmount)
        {
            effect.SetHalfPixel(sourceAndDestination.GetLevelDescription(0).Width, sourceAndDestination.GetLevelDescription(0).Height);

            int numberOfBlurPasses = (int)Math.Floor(NumberOfBlurPasses);
            for (int i = 0; i < numberOfBlurPasses; i++)
            {
                // Apply vertical gaussian blur
                MyRender.SetRenderTarget(aux, null);
                effect.BlurAmount = verticalBlurAmount;
                effect.SetSourceTexture(sourceAndDestination);
                //effect.SetWidthForHorisontalPass(sourceAndDestination.Width);
                effect.SetHeightForVerticalPass(sourceAndDestination.GetLevelDescription(0).Height);
                MyRender.GetFullscreenQuad().Draw(effect);

                // Apply horizontal gaussian blur
                MyRender.SetRenderTarget(sourceAndDestination, null);
                effect.BlurAmount = horizontalBlurAmount;
                effect.SetSourceTexture(aux);
                //effect.SetHeightForVerticalPass(sourceAndDestination.Height);
                effect.SetWidthForHorisontalPass(aux.GetLevelDescription(0).Width);
                MyRender.GetFullscreenQuad().Draw(effect);
            }
        }

        /// <summary>
        /// Downscales the source to 1/8th size, using mipmaps
        /// !! IMPORTANT !! you cannot just switch function call. Also changing RTs is necessary.
        /// </summary>
        protected void GenerateDownscale8(Texture sourceMod, Texture sourceDiv, Texture destination, MyEffectScale effect)
        {
            effect.SetTechnique(MyEffectScale.Technique.Downscale8);

            MyRender.SetRenderTarget(destination, null);

            effect.SetSourceTextureMod(sourceMod);
            effect.SetSourceTextureDiv(sourceDiv);
            //effect.SetLumTexture(currentFrameAdaptedLuminance);
            effect.SetHalfPixel(sourceMod.GetLevelDescription(0).Width, sourceMod.GetLevelDescription(0).Height);

            MyRender.GetFullscreenQuad().Draw(effect);
        }

        /// <summary>
        /// Downscales the source to 1/4th size, using mipmaps
        /// !! IMPORTANT !! you cannot just switch function call. Also changing RTs is necessary.
        /// </summary>
        protected void GenerateDownscale4(Texture sourceMod, Texture sourceDiv, Texture destination, MyEffectScale effect)
        {
            effect.SetTechnique(MyEffectScale.Technique.Downscale4);

            MyRender.SetRenderTarget(destination, null);

            effect.SetSourceTextureMod(sourceMod);
            effect.SetSourceTextureDiv(sourceDiv);
            //effect.SetLumTexture(currentFrameAdaptedLuminance);
            effect.SetHalfPixel(sourceMod.GetLevelDescription(0).Width, sourceMod.GetLevelDescription(0).Height);

            MyRender.GetFullscreenQuad().Draw(effect);
        }

        protected void PostProcess(Texture source, Texture destination, MyEffectHDRBase effect)
        {
            MyRender.SetRenderTarget(destination, null);

            effect.SetHalfPixel(source.GetLevelDescription(0).Width, source.GetLevelDescription(0).Height);
            effect.SetSourceTextureMod(source);

            MyRender.GetFullscreenQuad().Draw(effect);
        }

        private void GenerateThreshold(Texture sourceMod, Texture sourceDiv, Texture[] destination, MyEffectThreshold effect, float threshold, float bloomIntensity, float bloomIntensityBackground, float exposure)
        {
            MyRender.SetRenderTargets(destination, null);

            effect.SetSourceTextureMod(sourceMod);
            effect.SetSourceTextureDiv(sourceDiv);
            //effect.SetLumTexture(currentFrameAdaptedLuminance);
            effect.SetHalfPixel(sourceMod.GetLevelDescription(0).Width, sourceMod.GetLevelDescription(0).Height);
            effect.SetThreshold(threshold);
            effect.SetBloomIntensity(bloomIntensity);
            effect.SetBloomIntensityBackground(bloomIntensityBackground);
            effect.SetExposure(exposure);

            MyRender.GetFullscreenQuad().Draw(effect);
        }
    }
}
