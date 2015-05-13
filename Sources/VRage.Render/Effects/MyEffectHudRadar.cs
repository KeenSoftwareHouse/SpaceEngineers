using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectHudRadar : MyEffectBase
    {
        readonly EffectHandle m_billboardTexture;
        readonly EffectHandle m_viewProjectionMatrix;

        public MyEffectHudRadar()
            : base("Effects2\\HUD\\MyHudRadarEffect")
        {
            m_billboardTexture = m_D3DEffect.GetParameter(null, "Texture");
           m_viewProjectionMatrix = m_D3DEffect.GetParameter(null, "ViewProjectionMatrix");
        }

        public void SetBillboardTexture(Texture texture)
        {
            m_D3DEffect.SetTexture(m_billboardTexture, texture);
        }

        public void SetViewProjectionMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_viewProjectionMatrix, matrix);
        }
    }
}
