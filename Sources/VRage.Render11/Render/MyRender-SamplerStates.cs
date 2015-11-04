using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        internal static SamplerId m_alphamaskarraySamplerState;

        internal static SamplerState[] StandardSamplers 
        { 
            get {
                return new[] { 
                    MyPipelineStates.GetSampler(m_defaultSamplerState), 
                    MyPipelineStates.GetSampler(m_pointSamplerState), 
                    MyPipelineStates.GetSampler(m_linearSamplerState), 
                    MyPipelineStates.GetSampler(m_textureSamplerState), 
                    MyPipelineStates.GetSampler(m_alphamaskSamplerState),
                    MyPipelineStates.GetSampler(m_alphamaskarraySamplerState) }; 
            } 
        }

        internal static void UpdateTextureSampler(SamplerId samplerState, TextureAddressMode addressMode)
        {
            SamplerStateDescription description = new SamplerStateDescription();
            description.AddressU = addressMode;
            description.AddressV = addressMode;
            description.AddressW = addressMode;
            description.MaximumLod = System.Single.MaxValue;

            if(MyRender11.RenderSettings.AnisotropicFiltering == MyTextureAnisoFiltering.NONE)
            {
                description.Filter = Filter.MinMagMipLinear;
            }
            else
            {
                description.Filter = Filter.Anisotropic;

                switch(MyRender11.RenderSettings.AnisotropicFiltering)
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

            MyPipelineStates.ChangeSamplerState(samplerState, description);
        }

        private static void InitilizeSamplerStates()
        {
            SamplerStateDescription description = new SamplerStateDescription();
            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = System.Single.MaxValue;
            m_defaultSamplerState = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Border;
            description.AddressV = TextureAddressMode.Border;
            description.AddressW = TextureAddressMode.Border;
            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = System.Single.MaxValue;
            description.BorderColor = new Color4(0, 0, 0, 0);
            m_alphamaskSamplerState = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.MinMagMipPoint;
            description.MaximumLod = System.Single.MaxValue;
            m_pointSamplerState = MyPipelineStates.CreateSamplerState(description);

            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = System.Single.MaxValue;
            m_linearSamplerState = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.ComparisonMinMagMipLinear;
            description.MaximumLod = System.Single.MaxValue;
            description.ComparisonFunction = Comparison.LessEqual;
            m_shadowmapSamplerState = MyPipelineStates.CreateSamplerState(description);

            m_textureSamplerState = MyPipelineStates.CreateSamplerState(description);
            m_alphamaskarraySamplerState = MyPipelineStates.CreateSamplerState(description);

            UpdateTextureSampler(m_textureSamplerState, TextureAddressMode.Wrap);
            UpdateTextureSampler(m_alphamaskarraySamplerState, TextureAddressMode.Clamp);
        }
    }
}
