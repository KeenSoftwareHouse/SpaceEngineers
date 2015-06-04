using System;
using SharpDX;
using SharpDX.Direct3D11;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static SamplerId m_defaultSamplerState;
        internal static SamplerId m_pointSamplerState;
        internal static SamplerId m_linearSamplerState;
        internal static SamplerId m_textureSamplerState;
        internal static SamplerId m_shadowmapSamplerState;
        internal static SamplerId m_alphamaskSamplerState;

        internal static SamplerState[] StandardSamplers => new[] { 
            MyPipelineStates.GetSampler(m_defaultSamplerState), 
            MyPipelineStates.GetSampler(m_pointSamplerState), 
            MyPipelineStates.GetSampler(m_linearSamplerState), 
            MyPipelineStates.GetSampler(m_textureSamplerState), 
            MyPipelineStates.GetSampler(m_alphamaskSamplerState) };

        internal static void UpdateTextureSampler()
        {
            SamplerStateDescription description = new SamplerStateDescription();
            description.AddressU = TextureAddressMode.Wrap;
            description.AddressV = TextureAddressMode.Wrap;
            description.AddressW = TextureAddressMode.Wrap;
            description.MaximumLod = Single.MaxValue;

            if(RenderSettings.AnisotropicFiltering == MyTextureAnisoFiltering.NONE)
            {
                description.Filter = Filter.MinMagMipLinear;
            }
            else
            {
                description.Filter = Filter.Anisotropic;

                switch(RenderSettings.AnisotropicFiltering)
                {
                    case MyTextureAnisoFiltering.ANISO_1:
                        description.MaximumAnisotropy = 1;
                        break;
                    case MyTextureAnisoFiltering.ANISO_4:
                        description.MaximumAnisotropy = 4;
                        break;
                    case MyTextureAnisoFiltering.ANISO_8:
                        description.MaximumAnisotropy = 8;
                        break;
                    case MyTextureAnisoFiltering.ANISO_16:
                        description.MaximumAnisotropy = 16;
                        break;
                    default:
                        description.MaximumAnisotropy = 1;
                        break;
                }
            }

            MyPipelineStates.ChangeSamplerState(m_textureSamplerState, description);
        }

        private static void InitilizeSamplerStates()
        {
            SamplerStateDescription description = new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.MinMagMipLinear,
                MaximumLod = Single.MaxValue
            };
            m_defaultSamplerState = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Border;
            description.AddressV = TextureAddressMode.Border;
            description.AddressW = TextureAddressMode.Border;
            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = Single.MaxValue;
            description.BorderColor = new Color4(0, 0, 0, 0);
            m_alphamaskSamplerState = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.MinMagMipPoint;
            description.MaximumLod = Single.MaxValue;
            m_pointSamplerState = MyPipelineStates.CreateSamplerState(description);

            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = Single.MaxValue;
            m_linearSamplerState = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.ComparisonMinMagMipLinear;
            description.MaximumLod = Single.MaxValue;
            description.ComparisonFunction = Comparison.LessEqual;
            m_shadowmapSamplerState = MyPipelineStates.CreateSamplerState(description);

            m_textureSamplerState = MyPipelineStates.CreateSamplerState(description);
            UpdateTextureSampler();
        }
    }
}
