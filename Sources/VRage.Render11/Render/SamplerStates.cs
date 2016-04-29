using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;
using VRage.Utils;

namespace VRageRender
{
    internal static class SamplerStates
    {
        internal static SamplerId m_default { get; private set; }
        internal static SamplerId m_point { get; private set; }
        internal static SamplerId m_linear { get; private set; }
        internal static SamplerId m_texture { get; private set; }
        internal static SamplerId m_shadowmap { get; private set; }
        internal static SamplerId m_alphamask { get; private set; }
        internal static SamplerId m_alphamaskArray { get; private set; }

        internal static SamplerState[] StandardSamplers { get; private set; }

        static SamplerStates()
        {
            StandardSamplers = new SamplerState[6];
        }
        internal static void InitOnce()
        {
            SamplerStateDescription description = new SamplerStateDescription();
            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = System.Single.MaxValue;
            m_default = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Border;
            description.AddressV = TextureAddressMode.Border;
            description.AddressW = TextureAddressMode.Border;
            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = System.Single.MaxValue;
            description.BorderColor = new Color4(0, 0, 0, 0);
            m_alphamask = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.MinMagMipPoint;
            description.MaximumLod = System.Single.MaxValue;
            m_point = MyPipelineStates.CreateSamplerState(description);

            description.Filter = Filter.MinMagMipLinear;
            description.MaximumLod = System.Single.MaxValue;
            m_linear = MyPipelineStates.CreateSamplerState(description);

            description.AddressU = TextureAddressMode.Clamp;
            description.AddressV = TextureAddressMode.Clamp;
            description.AddressW = TextureAddressMode.Clamp;
            description.Filter = Filter.ComparisonMinMagMipLinear;
            description.MaximumLod = System.Single.MaxValue;
            description.ComparisonFunction = Comparison.LessEqual;
            m_shadowmap = MyPipelineStates.CreateSamplerState(description);

            m_texture = MyPipelineStates.CreateSamplerState(description);
            m_alphamaskArray = MyPipelineStates.CreateSamplerState(description);

            UpdateFiltering();

            Init();
        }

        internal static void Init()
        {
            StandardSamplers[0] = MyPipelineStates.GetSampler(m_default);
            StandardSamplers[1] = MyPipelineStates.GetSampler(m_point);
            StandardSamplers[2] = MyPipelineStates.GetSampler(m_linear);
            StandardSamplers[3] = MyPipelineStates.GetSampler(m_texture);
            StandardSamplers[4] = MyPipelineStates.GetSampler(m_alphamask);
            StandardSamplers[5] = MyPipelineStates.GetSampler(m_alphamaskArray);
        }

        internal static void UpdateFiltering()
        {
            UpdateTextureSampler(m_texture, TextureAddressMode.Wrap);
            UpdateTextureSampler(m_alphamaskArray, TextureAddressMode.Clamp);
            Init();
        }

        internal static void OnDeviceReset() 
        {
            Init();
        }

        private static void UpdateTextureSampler(SamplerId samplerState, TextureAddressMode addressMode)
        {
            SamplerStateDescription description = new SamplerStateDescription();
            description.AddressU = addressMode;
            description.AddressV = addressMode;
            description.AddressW = addressMode;
            description.MaximumLod = System.Single.MaxValue;

            if (MyRender11.RenderSettings.AnisotropicFiltering == MyTextureAnisoFiltering.NONE)
            {
                description.Filter = Filter.MinMagMipLinear;
            }
            else
            {
                description.Filter = Filter.Anisotropic;

                switch (MyRender11.RenderSettings.AnisotropicFiltering)
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
    }
}
