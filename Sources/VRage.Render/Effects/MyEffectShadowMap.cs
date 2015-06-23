using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectShadowMap : MyEffectBase
    {
        public enum ShadowTechnique
        {
            PCF2x2 = 0,
            PCF3x3 = 1,
            PCF5x5 = 2,
            PCF7x7 = 3,
            GenerateShadow,
            GenerateShadowForVoxels,
            GenerateShadowInstanced,
            GenerateShadowInstancedGeneric,
            GenerateShadowForSkinned,
            Clear,
        }

        readonly EffectHandle m_bias;
        readonly EffectHandle m_invViewMatrix;
        readonly EffectHandle m_lightViewProjMatrices;
        readonly EffectHandle m_frustumCornersVS;
        readonly EffectHandle m_clipPlanes;
        readonly EffectHandle m_shadowMap;
        readonly EffectHandle m_depthTexture;
        readonly EffectHandle m_shadowTermHalfPixel;
        readonly EffectHandle m_shadowMapSize;
        readonly EffectHandle m_showSplitColors;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_viewProjMatrix;
        readonly EffectHandle m_bones;
        readonly EffectHandle m_dithering;
        readonly EffectHandle m_ditheringTexture;
        readonly EffectHandle m_ditheringTextureSize;


        readonly EffectHandle m_createShadowTerm2x2;
        readonly EffectHandle m_createShadowTerm3x3;
        readonly EffectHandle m_createShadowTerm5x5;
        readonly EffectHandle m_createShadowTerm7x7;
        readonly EffectHandle m_generateShadowMap;
        readonly EffectHandle m_generateVoxelShadowMap;
        readonly EffectHandle m_generateShadowMapInstanced;
        readonly EffectHandle m_generateShadowMapInstancedGeneric;
        readonly EffectHandle m_generateShadowSkinned;

        readonly EffectHandle m_clearTechnique;

        public readonly MyEffectComponentVoxelVertex VoxelVertex;

        public MyEffectShadowMap()
            : base("Effects2\\Shadows\\MyEffectShadowMap")
        {
            m_bias = m_D3DEffect.GetParameter(null, "ShadowBias");
            m_invViewMatrix = m_D3DEffect.GetParameter(null, "InvViewMatrix");
            m_lightViewProjMatrices = m_D3DEffect.GetParameter(null, "LightViewProjMatrices");
            m_frustumCornersVS = m_D3DEffect.GetParameter(null, "FrustumCornersVS");
            m_clipPlanes = m_D3DEffect.GetParameter(null, "ClipPlanes");
            m_shadowMap = m_D3DEffect.GetParameter(null, "ShadowMap");
            m_depthTexture = m_D3DEffect.GetParameter(null, "DepthTexture");
            m_shadowTermHalfPixel = m_D3DEffect.GetParameter(null, "ShadowTermHalfPixel");
            m_shadowMapSize = m_D3DEffect.GetParameter(null, "ShadowMapSize");
            m_showSplitColors = m_D3DEffect.GetParameter(null, "ShowSplitColors");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_viewProjMatrix = m_D3DEffect.GetParameter(null, "ViewProjMatrix");
            m_bones = m_D3DEffect.GetParameter(null, "Bones");
            m_dithering = m_D3DEffect.GetParameter(null, "Dithering");
            m_ditheringTexture = m_D3DEffect.GetParameter(null, "TextureDithering");
            m_ditheringTextureSize = m_D3DEffect.GetParameter(null, "TextureDitheringSize");

            VoxelVertex = new MyEffectComponentVoxelVertex(m_D3DEffect);

            m_createShadowTerm2x2 = m_D3DEffect.GetTechnique("CreateShadowTerm2x2PCF");
            m_createShadowTerm3x3 = m_D3DEffect.GetTechnique("CreateShadowTerm3x3PCF");
            m_createShadowTerm5x5 = m_D3DEffect.GetTechnique("CreateShadowTerm5x5PCF");
            m_createShadowTerm7x7 = m_D3DEffect.GetTechnique("CreateShadowTerm7x7PCF");
            m_generateShadowMap = m_D3DEffect.GetTechnique("GenerateShadowMap");
            m_generateVoxelShadowMap = m_D3DEffect.GetTechnique("GenerateVoxelShadowMap");
            m_generateShadowSkinned = m_D3DEffect.GetTechnique("GenerateShadowMapSkinned");
            m_clearTechnique = m_D3DEffect.GetTechnique("Clear");

            m_generateShadowMapInstanced = m_D3DEffect.GetTechnique("GenerateShadowMapInstanced");
            m_generateShadowMapInstancedGeneric = m_D3DEffect.GetTechnique("GenerateShadowMapInstancedGeneric");
        }

        public void SetInvViewMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_invViewMatrix, matrix);
        }

        public void SetLightViewProjMatrices(ref Matrix[] matrices)
        {
            m_D3DEffect.SetValue(m_lightViewProjMatrices, matrices);
        }

        public void SetFrustumCornersVS(ref Vector3[] frustumCornersVS)
        {
            m_D3DEffect.SetValue(m_frustumCornersVS, frustumCornersVS);
        }

        public void SetClipPlanes(ref Vector2[] clipPlanes)
        {
            m_D3DEffect.SetValue(m_clipPlanes, clipPlanes);
        }

        public void SetShadowMap(Texture texture)
        {
            m_D3DEffect.SetTexture(m_shadowMap, texture);
        }

        public void SetDepthTexture(Texture texture)
        {
            m_D3DEffect.SetTexture(m_depthTexture, texture);
        }

        public void SetShadowTermHalfPixel(int width, int height)
        {
            m_D3DEffect.SetValue(m_shadowTermHalfPixel, MyUtilsRender9.GetHalfPixel(width, height));
        }

        public void SetShadowMapSize(int width, int height)
        {
            m_D3DEffect.SetValue(m_shadowMapSize, new Vector2(width, height));
        }

        public void SetShowSplitColors(bool show)
        {
            m_D3DEffect.SetValue(m_showSplitColors, show);
        }

        public void SetHalfPixel(int width, int height)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(width, height));
        }

        public void SetWorldMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_worldMatrix, matrix);
        }

        public void SetViewProjMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_viewProjMatrix, matrix);
        }

        public void SetBones(Matrix[] boneTransforms)
        {
            m_D3DEffect.SetValue(m_bones, boneTransforms);
        }

        public void SetDitheringTexture(Texture ditheringTexture)
        {
            m_D3DEffect.SetTexture(m_ditheringTexture, ditheringTexture);
            var desc = ditheringTexture.GetLevelDescription(0);
            m_D3DEffect.SetValue(m_ditheringTextureSize, new Vector2(desc.Width, desc.Height));
        }

        public void SetDithering(float dithering)
        {
            m_D3DEffect.SetValue(m_dithering, dithering);
        }

        public void SetTechnique(ShadowTechnique type)
        {
            switch (type)
            {
                case ShadowTechnique.PCF2x2:
                    m_D3DEffect.Technique = m_createShadowTerm2x2;
                    break;
                case ShadowTechnique.PCF3x3:
                    m_D3DEffect.Technique = m_createShadowTerm3x3;
                    break;
                case ShadowTechnique.PCF5x5:
                    m_D3DEffect.Technique = m_createShadowTerm5x5;
                    break;
                case ShadowTechnique.PCF7x7:
                    m_D3DEffect.Technique = m_createShadowTerm7x7;
                    break;
                case ShadowTechnique.GenerateShadow:
                    m_D3DEffect.Technique = m_generateShadowMap;
                    break;
                case ShadowTechnique.GenerateShadowForVoxels:
                    m_D3DEffect.Technique = m_generateVoxelShadowMap;
                    break;
                case ShadowTechnique.GenerateShadowInstanced:
                    m_D3DEffect.Technique = m_generateShadowMapInstanced;
                    break;
                case ShadowTechnique.GenerateShadowInstancedGeneric:
                    m_D3DEffect.Technique = m_generateShadowMapInstancedGeneric;
                    break;
                case ShadowTechnique.GenerateShadowForSkinned:
                    m_D3DEffect.Technique = m_generateShadowSkinned;
                    break;
                case ShadowTechnique.Clear:
                    m_D3DEffect.Technique = m_clearTechnique;
                    break;
            }
        }
    }
}
