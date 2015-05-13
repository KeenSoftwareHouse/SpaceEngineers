#region Using

using SharpDX.Direct3D9;
using VRageRender.Graphics;
using VRageRender.Effects;
using VRageMath;

#endregion

namespace VRageRender
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  Volumetric SSAO 2
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class MyPostProcessVolumetricFog : MyPostProcessBase
    {
        /// <summary>
        /// Name of the post process
        /// </summary>
        public override MyPostProcessEnum Name { get { return MyPostProcessEnum.VolumetricFog; } }
        public override string DisplayName { get { return "Volumetric Fog"; } }

        public MyPostProcessVolumetricFog(bool enabled)
            : base(enabled)
        {
        }

        /// <summary>
        /// Enable state of post process
        /// </summary>
        public override bool Enabled
        {
            get
            {
                return base.Enabled;
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
                case PostProcessStage.PostLighting:
                    {
                        //todo fog
                        //if (MySector.FogProperties.FogMultiplier <= 0.0f)
                          //  return source;

                        MyStateObjects.VolumetricFogBlend.Apply();

                        MyEffectVolumetricFog volumetricFog = MyRender.GetEffect(MyEffects.VolumetricFog) as MyEffectVolumetricFog;

                        int width = MyRenderCamera.Viewport.Width;
                        int height = MyRenderCamera.Viewport.Height;

                        var scale = MyRender.GetScaleForViewport(source);

                        volumetricFog.SetSourceRT(source);
                        volumetricFog.SetDepthsRT(MyRender.GetRenderTarget(MyRenderTargets.Depth));
                        volumetricFog.SetNormalsTexture(MyRender.GetRenderTarget(MyRenderTargets.Normals));
                        volumetricFog.SetHalfPixel(width, height);
                        volumetricFog.SetViewProjectionMatrix((Matrix)MyRenderCamera.ViewProjectionMatrix);
                        volumetricFog.SetCameraPosition((Vector3)MyRenderCamera.Position);
                        volumetricFog.SetCameraMatrix((Matrix)MatrixD.Invert(MyRenderCamera.ViewMatrix));
                        volumetricFog.SetFrustumCorners(MyRender.GetShadowRenderer().GetFrustumCorners());
                        volumetricFog.SetScale(scale);
                        MyRenderCamera.SetupBaseEffect(volumetricFog, MyLodTypeEnum.LOD0);

                        //volumetricFog.SetWorldMatrix(Matrix.CreateScale(1000) * Matrix.CreateTranslation(MyCamera.Position));
                        //todo
                        //if (MyFakes.MWBUILDER)
                        //    volumetricFog.SetTechnique(MyEffectVolumetricFog.TechniqueEnum.SkipBackground);
                        //else
                            volumetricFog.SetTechnique(MyEffectVolumetricFog.TechniqueEnum.Default);

                        MyRender.GetFullscreenQuad().Draw(volumetricFog);
                    }
                    break;
            }        
            return source;
        }
    }
}
