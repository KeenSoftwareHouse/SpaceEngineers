using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectVoxelsDebris : MyEffectVoxelsBase
    {
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_viewWorldScaleMatrix;
        readonly EffectHandle m_textureCoordRandomPositionOffset;
        readonly EffectHandle m_textureCoordScale;
        readonly EffectHandle m_diffuseTextureColorMultiplier;

        public MyEffectVoxelsDebris()
            : base("Effects2\\Voxels\\MyEffectVoxelsDebris")
        {
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_viewWorldScaleMatrix = m_D3DEffect.GetParameter(null, "ViewWorldScaleMatrix");
            m_textureCoordRandomPositionOffset = m_D3DEffect.GetParameter(null, "TextureCoordRandomPositionOffset");
            m_textureCoordScale = m_D3DEffect.GetParameter(null, "TextureCoordScale");
            m_diffuseTextureColorMultiplier = m_D3DEffect.GetParameter(null, "DiffuseTextureColorMultiplier");
        }

        public void SetViewWorldScaleMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_viewWorldScaleMatrix, matrix);
        }

        public void SetWorldMatrix(ref Matrix matrix)
        {
            m_D3DEffect.SetValue(m_worldMatrix, matrix);
        }

        //  This applies only for explosion debris, because we want to add some randomization to 'world position to texture coord' transformation
        public void SetTextureCoordRandomPositionOffset(float textureCoordRandomPositionOffset)
        {
            m_D3DEffect.SetValue(m_textureCoordRandomPositionOffset, textureCoordRandomPositionOffset);
        }

        //  This applies only for explosion debris, because we want to add some randomization to 'world position to texture coord' transformation
        public void SetTextureCoordScale(float textureCoordScale)
        {
            m_D3DEffect.SetValue(m_textureCoordScale, textureCoordScale);
        }

        //	Add random color overlay on explosion debris diffuse texture output
        public void SetDiffuseTextureColorMultiplier(float diffuseTextureColorMultiplier)
        {
            m_D3DEffect.SetValue(m_diffuseTextureColorMultiplier, diffuseTextureColorMultiplier);
        }
    }
}