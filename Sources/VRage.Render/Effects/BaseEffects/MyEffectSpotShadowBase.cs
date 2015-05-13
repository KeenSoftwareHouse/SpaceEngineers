using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    internal class MyEffectSpotShadowBase : MyEffectBase
    {
        readonly EffectHandle m_shadowBias;
        readonly EffectHandle m_slopeBias;
        readonly EffectHandle m_shadowMap;
        readonly EffectHandle m_shadowMapSize;

        readonly EffectHandle m_invViewMatrix;
        readonly EffectHandle m_lightViewProjectionShadow;

        public MyEffectSpotShadowBase(string asset)
            : base(asset)
        {
            m_shadowBias = m_D3DEffect.GetParameter(null, "ShadowBias");
            m_slopeBias = m_D3DEffect.GetParameter(null, "SlopeBias");
            m_shadowMap = m_D3DEffect.GetParameter(null, "ShadowMap");
            m_shadowMapSize = m_D3DEffect.GetParameter(null, "ShadowMapSize");

            m_invViewMatrix = m_D3DEffect.GetParameter(null, "InvViewMatrix");
            m_lightViewProjectionShadow = m_D3DEffect.GetParameter(null, "LightViewProjectionShadow");
        }

        // Sets light view projection matrix for shadow mapping (different from texture mapping, shadow mapping camera is expanded to prevent shadow border flickering)
        public void SetLightViewProjectionShadow(Matrix lightViewProjection)
        {
            m_D3DEffect.SetValue(m_lightViewProjectionShadow, lightViewProjection);
        }

        public void SetInvViewMatrix(Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_invViewMatrix, viewMatrix);
        }

        public void SetShadowBias(float bias)
        {
            m_D3DEffect.SetValue(m_shadowBias, bias);
        }

        public void SetSlopeBias(float bias)
        {
            m_D3DEffect.SetValue(m_slopeBias, bias);
        }

        public void SetShadowMap(Texture shadowMap)
        {
            m_D3DEffect.SetTexture(m_shadowMap, shadowMap);
        }

        public void SetShadowMapSize(Vector2 size)
        {
            m_D3DEffect.SetValue(m_shadowMapSize, size);
        }
    }
}
