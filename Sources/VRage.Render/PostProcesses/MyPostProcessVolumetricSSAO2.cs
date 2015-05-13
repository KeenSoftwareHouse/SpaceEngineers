#region Using

using SharpDX.Direct3D9;

#endregion

namespace VRageRender
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using VRageRender.Effects;
    using VRageRender.Graphics;

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  Volumetric SSAO 2
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class MyPostProcessVolumetricSSAO2 : MyPostProcessBase
    {
        public float MinRadius = 1.554f; 
        public float MaxRadius = 400; 
        public float RadiusGrowZScale = 10.0f;
        public float CameraZFar = 70294;   //71500  //70294

        public float Bias = 0.35f; 
        public float Falloff = 0.12f; 
        public float NormValue = 1.0f;   //1
        public float Contrast = 4f;  //4

        public bool ShowOnlySSAO;
        public bool UseBlur;

        // SSAOParams.x = minRadius
// SSAOParams.y = maxRadius
// SSAOParams.z = radiusGrowZscale
// SSAOParams.w = camera zfar

// SSAOParams2.x = bias
// SSAOParams2.y = fallof
// SSAOParams2.z = occlusion samples normalization value * color scale
//uniform float4	SSAOParams2;


        /// <summary>
        /// Name of the post process
        /// </summary>
        public override MyPostProcessEnum Name { get { return MyPostProcessEnum.VolumetricSSAO2; } }
        public override string DisplayName { get { return "Volumetric SSAO 2"; } } 

        /// <summary>
        /// Enable state of post process
        /// </summary>
        public override bool Enabled
        {
            get
            {
                return base.Enabled && MyRenderConstants.RenderQualityProfile.EnableSSAO;
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
                case PostProcessStage.LODBlend:
                    {
                       

                        MyEffectVolumetricSSAO2 volumetricSsao = MyRender.GetEffect(MyEffects.VolumetricSSAO) as MyEffectVolumetricSSAO2;

                        int width = MyRender.GetRenderTarget(MyRenderTargets.Normals).GetLevelDescription(0).Width;
                        int height = MyRender.GetRenderTarget(MyRenderTargets.Normals).GetLevelDescription(0).Height;
                        int screenSizeX = width;
                        int screenSizeY = height;

                        //Render SSAO
                        MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.SSAO), null);

                        MyRender.GraphicsDevice.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(0), 1, 0);
                        DepthStencilState.None.Apply();
                        BlendState.Opaque.Apply();

                        Vector4 ssaoParams = new Vector4(MinRadius, MaxRadius, RadiusGrowZScale, CameraZFar);
                        Vector4 ssaoParams2 = new Vector4(Bias, Falloff, NormValue, 0);

                        volumetricSsao.SetDepthsRT(MyRender.GetRenderTarget(MyRenderTargets.Depth));
                        volumetricSsao.SetNormalsTexture(MyRender.GetRenderTarget(MyRenderTargets.Normals));
                        volumetricSsao.SetHalfPixel(screenSizeX, screenSizeY);

                        volumetricSsao.SetFrustumCorners(MyRender.GetShadowRenderer().GetFrustumCorners());

                        volumetricSsao.SetViewMatrix(MyRenderCamera.ViewMatrixAtZero);

                        volumetricSsao.SetParams1(ssaoParams);
                        volumetricSsao.SetParams2(ssaoParams2);

                        volumetricSsao.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);

                        volumetricSsao.SetContrast(Contrast);


                        MyRender.GetFullscreenQuad().Draw(volumetricSsao);
                                  
                        if (UseBlur)
                        {
                            //SSAO Blur
                            MyRender.SetRenderTarget(availableRenderTarget, null);
                            MyEffectSSAOBlur2 effectSsaoBlur = MyRender.GetEffect(MyEffects.SSAOBlur) as MyEffectSSAOBlur2;
                            effectSsaoBlur.SetDepthsRT(MyRender.GetRenderTarget(MyRenderTargets.Depth));
                            //effectSsaoBlur.SetNormalsRT(MyRender.GetRenderTarget(MyRenderTargets.Normals));
                            effectSsaoBlur.SetHalfPixel(width, height);
                            effectSsaoBlur.SetSSAOHalfPixel(screenSizeX, screenSizeY);
                            effectSsaoBlur.SetSsaoRT(MyRender.GetRenderTarget(MyRenderTargets.SSAO));
                            effectSsaoBlur.SetBlurDirection(new Vector2(0, 1f / (float)screenSizeY));
                            //effectSsaoBlur.SetBlurDirection(new Vector2(1 / (float)halfWidth, 1f / (float)halfHeight));

                            MyRender.GetFullscreenQuad().Draw(effectSsaoBlur);

                            MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.SSAOBlur), null);
                            effectSsaoBlur.SetSsaoRT(availableRenderTarget);
                            effectSsaoBlur.SetBlurDirection(new Vector2(1f / (float)screenSizeX, 0));
                            MyRender.GetFullscreenQuad().Draw(effectSsaoBlur);
                        }

                        //Bake it into diffuse
                        /*
                        MyEffectScreenshot ssEffect = MyRender.GetEffect(MyEffects.Screenshot) as MyEffectScreenshot;     
                        MySandboxGame.SetRenderTarget(availableRenderTarget, null);
                        ssEffect.SetSourceTexture(MyRender.GetRenderTarget(MyRenderTargets.Diffuse));
                        ssEffect.SetScale(Vector2.One);
                        ssEffect.SetTechnique(MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
                        
                        MyGuiManager.GetFullscreenQuad().Draw(ssEffect);
                                          
                                         */
                        MyRender.SetRenderTarget(MyRender.GetRenderTarget(MyRenderTargets.Diffuse), null);
                        /*
                        ssEffect.SetSourceTexture(availableRenderTarget);
                        ssEffect.SetTechnique(MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
                        ssEffect.SetScale(Vector2.One);
                        MyGuiManager.GetFullscreenQuad().Draw(ssEffect);
                                          */
                        MyEffectVolumetricSSAO2 effectVolumetricSsao = MyRender.GetEffect(MyEffects.VolumetricSSAO) as MyEffectVolumetricSSAO2;

                        //Blend with SSAO together
                        DepthStencilState.None.Apply();
                        
                        if (!ShowOnlySSAO)
                        {
                            MyRender.BeginSpriteBatch(MyStateObjects.SSAO_BlendState);
                        }
                        else
                        {
                            MyRender.CurrentRenderSetup.EnableLights = false;
                            MyRender.GraphicsDevice.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(1.0f), 1, 0);

                            MyRender.BeginSpriteBatch(MyStateObjects.SSAO_BlendState);
                        }

                        if (UseBlur)
                            MyRender.DrawSprite(MyRender.GetRenderTarget(MyRenderTargets.SSAOBlur), new Rectangle(0, 0, MyRenderCamera.Viewport.Width, MyRenderCamera.Viewport.Height), Color.White);
                        else
                            MyRender.DrawSprite(MyRender.GetRenderTarget(MyRenderTargets.SSAO), new Rectangle(0, 0, MyRenderCamera.Viewport.Width, MyRenderCamera.Viewport.Height), Color.White);
                        MyRender.EndSpriteBatch();
 
                    }
                    break;
            }       
            return source;
        }
    }
}
