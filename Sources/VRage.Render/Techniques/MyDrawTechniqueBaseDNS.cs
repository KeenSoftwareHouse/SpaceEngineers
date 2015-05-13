using System;
using VRage;
using VRage.Import;
using VRageMath;
using VRageRender.Effects;
using VRageRender.Textures;

namespace VRageRender.Techniques
{
    abstract class MyDrawTechniqueBaseDNS : MyDrawTechniqueBase
    {
        public sealed override void SetupMaterial(MyEffectBase shader, MyRenderMeshMaterial material)
        {
            System.Diagnostics.Debug.Assert(material != null);

            shader.SetTextureDiffuse(material.DiffuseTexture);
            shader.SetTextureNormal(material.NormalTexture);

            shader.SetSpecularIntensity(material.SpecularIntensity);
            shader.SetSpecularPower(material.SpecularPower);

            shader.SetDiffuseUVAnim(material.DiffuseUVAnim);
            shader.SetEmissivityUVAnim(material.EmissiveUVAnim);

            shader.SetEmissivityOffset(material.EmissivityOffset);

            ((MyEffectModelsDNS)shader).EnableColorMaskHsv(material.DiffuseTexture != null ? material.EnableColorMask : false);
            ((MyEffectModelsDNS)shader).SetDitheringTexture((SharpDX.Direct3D9.Texture)MyTextureManager.GetTexture<MyTexture2D>(@"Textures\Models\Dither.png"));
            ((MyEffectModelsDNS)shader).SetHalfPixel(MyRenderCamera.Viewport.Width, MyRenderCamera.Viewport.Height);

            // TODO: Petrzilka - Get rid of this branching
            if (material.DrawTechnique == MyMeshDrawTechnique.HOLO)
            {
                shader.SetEmissivity(material.HoloEmissivity);
            }

            if (material.Emissivity.HasValue)
            {
                shader.SetEmissivity(material.Emissivity.Value);
            }
            else
                shader.SetEmissivity(0);

            MyRender.CheckTextures(shader, material.NormalTexture, material.DrawTechnique == MyMeshDrawTechnique.HOLO);
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            MyEffectModelsDNS effectDNS = shader as MyEffectModelsDNS;
            effectDNS.SetWorldMatrix((Matrix)renderElement.WorldMatrixForDraw);
            effectDNS.SetDiffuseColor(renderElement.Color);
            effectDNS.Dithering = renderElement.Dithering;
            effectDNS.Time = (float)MyRender.InterpolationTime.Miliseconds;
            effectDNS.SetColorMaskHSV(renderElement.ColorMaskHSV);
        }

        public sealed override void SetupVoxelMaterial(MyEffectVoxels shader, MyRenderVoxelBatch batch)
        {
            throw new InvalidOperationException();
        }
    }
}
