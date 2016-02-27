
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Import;
using VRageMath;
using VRageRender.Effects;
using VRageRender.Textures;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueAtmosphere : MyDrawTechniqueBase
    {
        public sealed override void SetupMaterial(MyEffectBase shader, MyRenderMeshMaterial material)
        {
            System.Diagnostics.Debug.Assert(material != null);

            shader.SetEmissivity(0);

            MyRender.CheckTextures(shader, material.NormalTexture, material.DrawTechnique == MyMeshDrawTechnique.HOLO);
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            MyEffectAtmosphere effectAtmosphere = shader as MyEffectAtmosphere;

            effectAtmosphere.SetWorldMatrix((Matrix)renderElement.WorldMatrixForDraw);
            effectAtmosphere.SetDiffuseColor(renderElement.Color);

            var atmosphere = renderElement.RenderObject as MyRenderAtmosphere;

            float depthScale = 0.2f;

            effectAtmosphere.SetInnerRadius(atmosphere.PlanetRadius);
            effectAtmosphere.SetOutherRadius(atmosphere.AtmosphereRadius);

            float scaleAtmosphere = 1.0f / (atmosphere.AtmosphereRadius - atmosphere.PlanetRadius);

            effectAtmosphere.SetScaleAtmosphere(scaleAtmosphere);
            effectAtmosphere.SetScaleAtmosphereOverScaleDepth(scaleAtmosphere / depthScale);

            Vector3 cameraToCenter = atmosphere.GetRelativeCameraPos(MyRenderCamera.Position);

            effectAtmosphere.SetRelativeCameraPos(cameraToCenter);

            effectAtmosphere.SetLightPos(-MySunGlare.GetSunDirection());
            effectAtmosphere.SetIsInside(atmosphere.IsInside(MyRenderCamera.Position));

            effectAtmosphere.SetScaleDepth(depthScale);

            effectAtmosphere.SetWavelength(atmosphere.AtmosphereWavelengths);
        }

        public sealed override void SetupVoxelMaterial(MyEffectVoxels shader, MyRenderVoxelBatch batch)
        {
            throw new InvalidOperationException();
        }

        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = (MyEffectAtmosphere)MyRender.GetEffect(MyEffects.Atmosphere);
            SetupBaseEffect(shader, setup, lodType);

            shader.SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsRenderTechnique);
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }
    }
}
