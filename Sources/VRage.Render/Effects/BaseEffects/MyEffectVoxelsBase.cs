using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using System;
    using VRage;
    using Matrix = VRageMath.Matrix;

    abstract class MyEffectVoxelsBase : MyEffectAtmosphereBase
    {
        readonly EffectHandle m_projectionMatrix;

        readonly EffectHandle m_textureDiffuseForAxisXZ;
        readonly EffectHandle m_textureDiffuseForAxisY;
        readonly EffectHandle m_textureNormalMapForAxisXZ;
        readonly EffectHandle m_textureNormalMapForAxisY;

        readonly EffectHandle m_textureDiffuseForAxisXZ2;
        readonly EffectHandle m_textureDiffuseForAxisY2;
        readonly EffectHandle m_textureNormalMapForAxisXZ2;
        readonly EffectHandle m_textureNormalMapForAxisY2;

        readonly EffectHandle m_textureDiffuseForAxisXZ3;
        readonly EffectHandle m_textureDiffuseForAxisY3;
        readonly EffectHandle m_textureNormalMapForAxisXZ3;
        readonly EffectHandle m_textureNormalMapForAxisY3;

        readonly EffectHandle m_specularIntensity;
        readonly EffectHandle m_specularPower;

        readonly EffectHandle m_specularIntensity2;
        readonly EffectHandle m_specularPower2;

        readonly EffectHandle m_specularIntensity3;
        readonly EffectHandle m_specularPower3;

        readonly EffectHandle m_lowTechnique;
        readonly EffectHandle m_normalTechnique;
        readonly EffectHandle m_highTechnique;
        readonly EffectHandle m_extremeTechnique;

        readonly EffectHandle m_normalMultimaterialTechnique;
        readonly EffectHandle m_highMultimaterialTechnique;
        readonly EffectHandle m_extremeMultimaterialTechnique;

        readonly EffectHandle m_normalFarTechnique;
        readonly EffectHandle m_normalMultimaterialFarTechnique;

        readonly EffectHandle m_highFarTechnique;
        readonly EffectHandle m_highMultimaterialFarTechnique;

        readonly EffectHandle m_extremeFarTechnique;
        readonly EffectHandle m_extremeMultimaterialFarTechnique;

        readonly EffectHandle m_lowInstancedTechnique;
        readonly EffectHandle m_normalInstancedTechnique;
        readonly EffectHandle m_highInstancedTechnique;
        readonly EffectHandle m_extremeInstancedTechnique;

        /// <summary>
        /// Set to true when multiple materials has been set
        /// </summary>
        protected bool m_multimaterial;

        public MyEffectDynamicLightingBase DynamicLights { get; private set; }
        public MyEffectReflectorBase Reflector { get; private set; }

        public MyEffectVoxelsBase(string asset)
            : base(asset)
        {
            m_projectionMatrix = m_D3DEffect.GetParameter(null, "ProjectionMatrix");

            m_textureDiffuseForAxisXZ = m_D3DEffect.GetParameter(null, "TextureDiffuseForAxisXZ");
            m_textureDiffuseForAxisY = m_D3DEffect.GetParameter(null, "TextureDiffuseForAxisY");
            m_textureNormalMapForAxisXZ = m_D3DEffect.GetParameter(null, "TextureNormalMapForAxisXZ");
            m_textureNormalMapForAxisY = m_D3DEffect.GetParameter(null, "TextureNormalMapForAxisY");

            m_textureDiffuseForAxisXZ2 = m_D3DEffect.GetParameter(null, "TextureDiffuseForAxisXZ2");
            m_textureDiffuseForAxisY2 = m_D3DEffect.GetParameter(null, "TextureDiffuseForAxisY2");
            m_textureNormalMapForAxisXZ2 = m_D3DEffect.GetParameter(null, "TextureNormalMapForAxisXZ2");
            m_textureNormalMapForAxisY2 = m_D3DEffect.GetParameter(null, "TextureNormalMapForAxisY2");

            m_textureDiffuseForAxisXZ3 = m_D3DEffect.GetParameter(null, "TextureDiffuseForAxisXZ3");
            m_textureDiffuseForAxisY3 = m_D3DEffect.GetParameter(null, "TextureDiffuseForAxisY3");
            m_textureNormalMapForAxisXZ3 = m_D3DEffect.GetParameter(null, "TextureNormalMapForAxisXZ3");
            m_textureNormalMapForAxisY3 = m_D3DEffect.GetParameter(null, "TextureNormalMapForAxisY3");

            m_specularIntensity = m_D3DEffect.GetParameter(null, "SpecularIntensity");
            m_specularPower = m_D3DEffect.GetParameter(null, "SpecularPower");

            m_specularIntensity2 = m_D3DEffect.GetParameter(null, "SpecularIntensity2");
            m_specularPower2 = m_D3DEffect.GetParameter(null, "SpecularPower2");

            m_specularIntensity3 = m_D3DEffect.GetParameter(null, "SpecularIntensity3");
            m_specularPower3 = m_D3DEffect.GetParameter(null, "SpecularPower3");

            m_lowTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityLow");
            m_normalTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormal");
            m_highTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHigh");
            m_extremeTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityExtreme");

            m_normalMultimaterialTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormal_Multimaterial");
            m_highMultimaterialTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHigh_Multimaterial");
            m_extremeMultimaterialTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityExtreme_Multimaterial");

            m_normalFarTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormal_Far");
            m_normalMultimaterialFarTechnique = D3DEffect.GetTechnique("Technique_RenderQualityNormal_Mulitmaterial_Far");

            m_highFarTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHigh_Far");
            m_highMultimaterialFarTechnique = D3DEffect.GetTechnique("Technique_RenderQualityHigh_Mulitmaterial_Far");

            m_extremeFarTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityExtreme_Far");
            m_extremeMultimaterialFarTechnique = D3DEffect.GetTechnique("Technique_RenderQualityExtreme_Mulitmaterial_Far"); 


            m_lowInstancedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityLow_Instanced");
            m_normalInstancedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormal_Instanced");
            m_highInstancedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHigh_Instanced");
            m_extremeInstancedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityExtreme_Instanced");

            DynamicLights = new MyEffectDynamicLightingBase(m_D3DEffect);
            Reflector = new MyEffectReflectorBase(m_D3DEffect);

        }

        public override void SetSpecularIntensity(float specularIntensity)
        {
            // DO NOTHING, these values are set by UpdateVoxelTextures()
        }

        public override void SetSpecularPower(float specularPower)
        {
            // DO NOTHING, these values are set by UpdateVoxelTextures()
        }

        public void UpdateVoxelTextures(byte material)
        {   
            m_multimaterial = false;

            //  Get with lazy-load
            MyRenderVoxelMaterial voxelMaterial = MyRenderVoxelMaterials.Get(material);
            MyVoxelMaterialTextures voxelTexture = voxelMaterial.GetTextures();

            m_D3DEffect.SetTexture(m_textureDiffuseForAxisXZ, (Texture)voxelTexture.TextureDiffuseForAxisXZ);
            m_D3DEffect.SetTexture(m_textureDiffuseForAxisY, (Texture)voxelTexture.TextureDiffuseForAxisY);

            m_D3DEffect.SetTexture(m_textureNormalMapForAxisXZ, (Texture)voxelTexture.TextureNormalMapForAxisXZ);
            m_D3DEffect.SetTexture(m_textureNormalMapForAxisY, (Texture)voxelTexture.TextureNormalMapForAxisY);


            m_D3DEffect.SetValue(m_specularIntensity, voxelMaterial.SpecularIntensity);
            m_D3DEffect.SetValue(m_specularPower, voxelMaterial.SpecularPower); 
        }

        public void UpdateVoxelMultiTextures(byte mat0, byte? mat1, byte? mat2)
        {
            m_multimaterial = false;
            //  Get with lazy-load
            // Material 0
            MyRenderVoxelMaterial voxelMaterial = MyRenderVoxelMaterials.Get(mat0);
            MyVoxelMaterialTextures voxelTexture = voxelMaterial.GetTextures();

            m_D3DEffect.SetTexture(m_textureDiffuseForAxisXZ, (Texture)voxelTexture.TextureDiffuseForAxisXZ);
            m_D3DEffect.SetTexture(m_textureNormalMapForAxisXZ, (Texture)voxelTexture.TextureNormalMapForAxisXZ);
            m_D3DEffect.SetTexture(m_textureDiffuseForAxisY, (Texture)voxelTexture.TextureDiffuseForAxisY);
            m_D3DEffect.SetTexture(m_textureNormalMapForAxisY, (Texture)voxelTexture.TextureNormalMapForAxisY);

            m_D3DEffect.SetValue(m_specularIntensity, voxelMaterial.SpecularIntensity);
            m_D3DEffect.SetValue(m_specularPower, voxelMaterial.SpecularPower);

            // Material 1
            if (mat1.HasValue)
            {
                m_multimaterial = true;
                MyRenderVoxelMaterial voxelMaterial2 = MyRenderVoxelMaterials.Get(mat1.Value);
                MyVoxelMaterialTextures voxelTexture2 = voxelMaterial2.GetTextures();

                m_D3DEffect.SetTexture(m_textureDiffuseForAxisXZ2, (Texture)voxelTexture2.TextureDiffuseForAxisXZ);
                m_D3DEffect.SetTexture(m_textureNormalMapForAxisXZ2, (Texture)voxelTexture2.TextureNormalMapForAxisXZ);
                m_D3DEffect.SetTexture(m_textureDiffuseForAxisY2, (Texture)voxelTexture2.TextureDiffuseForAxisY);
                m_D3DEffect.SetTexture(m_textureNormalMapForAxisY2, (Texture)voxelTexture2.TextureNormalMapForAxisY);


                m_D3DEffect.SetValue(m_specularIntensity2, voxelMaterial2.SpecularIntensity);
                m_D3DEffect.SetValue(m_specularPower2, voxelMaterial2.SpecularPower);
            }

            // Material 2
            if (mat2.HasValue)
            {
                m_multimaterial = true;

                MyRenderVoxelMaterial voxelMaterial3 = MyRenderVoxelMaterials.Get(mat2.Value);
                MyVoxelMaterialTextures voxelTexture3 = voxelMaterial3.GetTextures();

                m_D3DEffect.SetTexture(m_textureDiffuseForAxisXZ3, (Texture)voxelTexture3.TextureDiffuseForAxisXZ);
                m_D3DEffect.SetTexture(m_textureNormalMapForAxisXZ3, (Texture)voxelTexture3.TextureNormalMapForAxisXZ);
                m_D3DEffect.SetTexture(m_textureDiffuseForAxisY3, (Texture)voxelTexture3.TextureDiffuseForAxisY);
                m_D3DEffect.SetTexture(m_textureNormalMapForAxisY3, (Texture)voxelTexture3.TextureNormalMapForAxisY);

                m_D3DEffect.SetValue(m_specularIntensity3, voxelMaterial3.SpecularIntensity);
                m_D3DEffect.SetValue(m_specularPower3, voxelMaterial3.SpecularPower);
            }
        }

        public override void SetProjectionMatrix(ref Matrix projectionMatrix)
        {
            m_D3DEffect.SetValue(m_projectionMatrix, projectionMatrix);
        }

        public void SetTechnique(MyEffectVoxelsTechniqueEnum technique)
        {
            switch (technique)
            {
                case MyEffectVoxelsTechniqueEnum.Low:
                    m_D3DEffect.Technique = m_lowTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.Normal:
                    m_D3DEffect.Technique = m_normalTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.High:
                    m_D3DEffect.Technique = m_highTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.Extreme:
                    m_D3DEffect.Technique = m_extremeTechnique;
                    break;


                default:
                    throw new InvalidBranchException();

            }
        }

        // Valid only for static asteroids
        public void ApplyInstanced()
        {
            switch (MyRenderConstants.RenderQualityProfile.VoxelsRenderTechnique)
            {
                case MyEffectVoxelsTechniqueEnum.Low:
                    m_D3DEffect.Technique = m_lowInstancedTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.Normal:
                    m_D3DEffect.Technique = m_normalInstancedTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.High:
                    m_D3DEffect.Technique = m_highInstancedTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.Extreme:
                    m_D3DEffect.Technique = m_extremeInstancedTechnique;
                    break;
            }

        }

        public void ApplyMultimaterial()
        {
            switch (MyRenderConstants.RenderQualityProfile.VoxelsRenderTechnique)
            {
                case MyEffectVoxelsTechniqueEnum.Low:
                    m_D3DEffect.Technique = m_normalMultimaterialTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.Normal:
                    m_D3DEffect.Technique = m_normalMultimaterialTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.High:
                    m_D3DEffect.Technique = m_highMultimaterialTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.Extreme:
                    m_D3DEffect.Technique = m_extremeMultimaterialTechnique;
                    break;
            }
        }

        public void ApplyFar(MyEffectVoxelsTechniqueEnum technique)
        {
            switch (technique)
            {
                case MyEffectVoxelsTechniqueEnum.Low:
                case MyEffectVoxelsTechniqueEnum.Normal:
                    m_D3DEffect.Technique = m_normalFarTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.High:
                    m_D3DEffect.Technique = m_highFarTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.Extreme:
                    m_D3DEffect.Technique = m_extremeFarTechnique;
                    break;
                default:
                    throw new InvalidBranchException();

            }
        }

        public void ApplyMultimaterialFar(MyEffectVoxelsTechniqueEnum technique)
        {
            switch (MyRenderConstants.RenderQualityProfile.VoxelsRenderTechnique)
            {
                case MyEffectVoxelsTechniqueEnum.Low:
                case MyEffectVoxelsTechniqueEnum.Normal:
                    m_D3DEffect.Technique = m_normalMultimaterialFarTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.High:
                    m_D3DEffect.Technique = m_highMultimaterialFarTechnique;
                    break;
                case MyEffectVoxelsTechniqueEnum.Extreme:
                    m_D3DEffect.Technique = m_extremeMultimaterialFarTechnique;
                    break;
            }
        }

        public void Apply()
        {
            SetTechnique(MyRenderConstants.RenderQualityProfile.VoxelsRenderTechnique);
        }
        
        public void ApplyWithoutTechniqueChange()
        {
        }

        public override void Begin(int pass, FX fx)
        {
            base.Begin(pass, FX.DoNotSaveState | FX.DoNotSaveShaderState | FX.DoNotSaveSamplerState);
        }

        public override void Dispose()
        {
            DynamicLights.Dispose();
            Reflector.Dispose();
            base.Dispose();
        }
    }
}