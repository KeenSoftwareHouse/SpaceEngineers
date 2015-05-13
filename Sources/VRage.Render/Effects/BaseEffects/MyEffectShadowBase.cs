using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;

    internal class MyEffectShadowBase : MyEffectBase
    {
        EffectHandle m_invViewMatrix;
        EffectHandle m_lightViewProjMatrices;
        EffectHandle m_clipPlanes;
        EffectHandle m_shadowMap;
        EffectHandle m_shadowMapSize;
        EffectHandle m_showSplitColors;
        EffectHandle m_shadowBias;
        EffectHandle m_slopeBias;
        EffectHandle m_slopeCascadeMultiplier;


        public MyEffectShadowBase(Effect xnaEffect)
            : base(xnaEffect)
        {
            Init();
        }

        public MyEffectShadowBase(string asset)
            : base(asset)
        {
            Init();
        }

        private void Init()
        {
            m_invViewMatrix = m_D3DEffect.GetParameter(null, "InvViewMatrix");
            m_lightViewProjMatrices = m_D3DEffect.GetParameter(null, "LightViewProjMatrices");
            m_clipPlanes = m_D3DEffect.GetParameter(null, "ClipPlanes");
            m_shadowMap = m_D3DEffect.GetParameter(null, "ShadowMap");
            m_shadowMapSize = m_D3DEffect.GetParameter(null, "ShadowMapSize");
            m_showSplitColors = m_D3DEffect.GetParameter(null, "ShowSplitColors");
            m_shadowBias = m_D3DEffect.GetParameter(null, "ShadowBias");
            m_slopeBias = m_D3DEffect.GetParameter(null, "SlopeBias");
            m_slopeCascadeMultiplier = m_D3DEffect.GetParameter(null, "SlopeCascadeMultiplier");
        }

        public void SetInvViewMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_invViewMatrix, matrix);
        }

        public void SetLightViewProjMatrices(Matrix[] matrices)
        {
            m_D3DEffect.SetValue(m_lightViewProjMatrices, matrices);
        }

        public void SetClipPlanes(Vector2[] planes)
        {
            m_D3DEffect.SetValue(m_clipPlanes, planes);
        }

        public void SetShadowMap(Texture shadowMap)
        {
            m_D3DEffect.SetTexture(m_shadowMap, shadowMap);
        }

        public void SetShadowMapSize(Vector4 size)
        {
            m_D3DEffect.SetValue(m_shadowMapSize, size);
        }

        public void ShowSplitColors(bool show)
        {
            m_D3DEffect.SetValue(m_showSplitColors, show);
        }

        public void SetShadowBias(float bias)
        {
            m_D3DEffect.SetValue(m_shadowBias, bias);
        }

        public void SetSlopeBias(float bias)
        {
            m_D3DEffect.SetValue(m_slopeBias, bias);
        }

        public void SetSlopeCascadeMultiplier(float multiplier)
        {
            m_D3DEffect.SetValue(m_slopeCascadeMultiplier, multiplier);
        }
    }
}
