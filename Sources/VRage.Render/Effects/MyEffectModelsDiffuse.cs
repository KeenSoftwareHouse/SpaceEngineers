using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Matrix = VRageMath.Matrix;

    class MyEffectModelsDiffuse : MyEffectBase
    {
        public enum Technique
        {
            PositionColor,
            Position,
        }

        readonly EffectHandle m_viewMatrix;
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_projectionMatrix;
        readonly EffectHandle m_diffuseColor;
        
        readonly EffectHandle m_positionColorTechnique;
        readonly EffectHandle m_positionTechnique;


        public MyEffectModelsDiffuse()
            : base("Effects2\\Models\\MyEffectModelsDiffuse")
        {
            m_viewMatrix = m_D3DEffect.GetParameter(null, "ViewMatrix");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_projectionMatrix = m_D3DEffect.GetParameter(null, "ProjectionMatrix");

            m_diffuseColor = m_D3DEffect.GetParameter(null, "DiffuseColor");

            m_positionColorTechnique = m_D3DEffect.GetTechnique("Technique_PositionColor");
            m_positionTechnique = m_D3DEffect.GetTechnique("Technique_Position");
        }

        public void SetWorldMatrix(Matrix worldMatrix)
        {
            m_D3DEffect.SetValue(m_worldMatrix, worldMatrix);
        }

        public override void SetViewMatrix(ref Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_viewMatrix, viewMatrix);
        }

        public void SetViewMatrix(Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_viewMatrix, viewMatrix);
        }

        public override void SetProjectionMatrix(ref Matrix projectionMatrix)
        {
            m_D3DEffect.SetValue(m_projectionMatrix, projectionMatrix);
        }

        public void SetProjectionMatrix(Matrix projectionMatrix)
        {
            m_D3DEffect.SetValue(m_projectionMatrix, projectionMatrix);
        }

        public override void SetDiffuseColor(Vector3 diffuseColor)
        {
            m_D3DEffect.SetValue(m_diffuseColor, new VRageMath.Vector4(diffuseColor.X, diffuseColor.Y, diffuseColor.Z, 1));
        }

        public void SetDiffuseColor4(VRageMath.Vector4 diffuseColor)
        {
            m_D3DEffect.SetValue(m_diffuseColor, diffuseColor);
        }

        public void SetTechnique(Technique technique)
        {
            switch (technique)
            {
                case Technique.PositionColor:
                    m_D3DEffect.Technique = m_positionColorTechnique;
                    break;
                case Technique.Position:
                    m_D3DEffect.Technique = m_positionTechnique;
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }
    }
}