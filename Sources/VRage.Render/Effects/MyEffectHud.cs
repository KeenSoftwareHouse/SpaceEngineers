using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectHud : MyEffectBase
    {
        readonly EffectHandle m_billboardTexture;
        readonly EffectHandle m_projectionMatrix;

        public MyEffectHud()
            : base("Effects2\\HUD\\MyHudEffect")
        {
            m_billboardTexture = m_D3DEffect.GetParameter(null, "HudTexture");
            m_projectionMatrix = m_D3DEffect.GetParameter(null, "ProjectionMatrix");
        }

        public void SetBillboardTexture(Texture texture)
        {
            m_D3DEffect.SetTexture(m_billboardTexture, texture);
        }

        public void SetProjectionMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_projectionMatrix, matrix);
        }
    }
}
