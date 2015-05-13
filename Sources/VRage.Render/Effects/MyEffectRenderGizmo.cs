using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectRenderGizmo : MyEffectBase
    {
        readonly EffectHandle m_worldViewProjectionMatrix;
        readonly EffectHandle m_textureDiffuse;
        readonly EffectHandle m_diffuseColor;
        readonly EffectHandle m_technique_RenderGizmo;

        public MyEffectRenderGizmo()
            : base("Effects2\\Models\\MyEffectRenderGizmo")
        {
            m_worldViewProjectionMatrix = m_D3DEffect.GetParameter(null, "WorldViewProjectionMatrix");
            m_textureDiffuse = m_D3DEffect.GetParameter(null, "TextureDiffuse");
            m_diffuseColor = m_D3DEffect.GetParameter(null, "DiffuseColor");
            m_technique_RenderGizmo = m_D3DEffect.GetTechnique("Technique_RenderGizmo");
        }

        public void SetWorldViewProjectionMatrix(Matrix projectionMatrix)
        {
            m_D3DEffect.SetValue(m_worldViewProjectionMatrix, projectionMatrix);
        }

        public override void SetTextureDiffuse(Texture texture2D)
        {
            //TODO
            /*
            if (texture2D == null && MyRender.CheckDiffuseTextures)
                m_D3DEffect.SetTexture(m_textureDiffuse, (Texture)MyRender.GetDebugTexture());
            else
                m_D3DEffect.SetTexture(m_textureDiffuse, texture2D);
             * */
        }

        public override void SetDiffuseColor(Vector3 diffuseColor)
        {
            m_D3DEffect.SetValue(m_diffuseColor, diffuseColor); 
        }
    }
}