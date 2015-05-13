using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectDistantImpostors : MyEffectBase
    {
        readonly EffectHandle m_impostorTexture;
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_viewProjectionMatrix;
        readonly EffectHandle m_scale;
        readonly EffectHandle m_cameraPos;
        readonly EffectHandle m_animation;
        readonly EffectHandle m_contrastAndIntensity;
        readonly EffectHandle m_color;
        readonly EffectHandle m_sunDirection;

        readonly EffectHandle m_defaultTechnique;
        readonly EffectHandle m_coloredTechnique;
        readonly EffectHandle m_coloredLitTechnique;
        readonly EffectHandle m_textured3DTechnique;

        public MyEffectPerlinNoiseBase PerlinBase  { get; private set; }

        public enum Technique
        {
            Default,
            Colored,
            ColoredLit,
            Textured3D
        }

        public MyEffectDistantImpostors()
            : base("Effects2\\BackgroundCube\\MyDistantImpostorEffect")
        {
            m_impostorTexture = m_D3DEffect.GetParameter(null, "ImpostorTexture");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_viewProjectionMatrix = m_D3DEffect.GetParameter(null, "ViewProjectionMatrix");
            m_scale = m_D3DEffect.GetParameter(null, "Scale");
            m_cameraPos = m_D3DEffect.GetParameter(null, "CameraPos");
            m_animation = m_D3DEffect.GetParameter(null, "Animation");
            m_contrastAndIntensity = m_D3DEffect.GetParameter(null, "ContrastAndIntensity");
            m_color = m_D3DEffect.GetParameter(null, "Color");
            m_sunDirection = m_D3DEffect.GetParameter(null, "SunDir");

            m_defaultTechnique = m_D3DEffect.GetTechnique("Default");
            m_coloredTechnique = m_D3DEffect.GetTechnique("Colored");
            m_coloredLitTechnique = m_D3DEffect.GetTechnique("ColoredLit");
            m_textured3DTechnique = m_D3DEffect.GetTechnique("Textured3D");

            PerlinBase = new MyEffectPerlinNoiseBase(m_D3DEffect);
        }

        public void SetImpostorTexture(Texture texture)
        {
            m_D3DEffect.SetTexture(m_impostorTexture, texture);
        }

        public void SetWorldMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_worldMatrix, matrix);
        }

        public void SetViewProjectionMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_viewProjectionMatrix, matrix);
        }

        public void SetScale(float scale)
        {
            m_D3DEffect.SetValue(m_scale, scale);
        }

        public void SetCameraPos(Vector3 cameraPos)
        {
            m_D3DEffect.SetValue(m_cameraPos, cameraPos);
        }

        public void SetAnimation(Vector4 animation)
        {
            m_D3DEffect.SetValue(m_animation, animation);
        }

        public void SetContrastAndIntensity(Vector2 value)
        {
            m_D3DEffect.SetValue(m_contrastAndIntensity, value);
        }

        public void SetColor(Vector3 value)
        {
            m_D3DEffect.SetValue(m_color, value);
        }

        public void SetSunDirection(Vector3 sunDirection)
        {
            m_D3DEffect.SetValue(m_sunDirection, sunDirection);
        }

        public void SetTechnique(Technique technique)
        {
            switch(technique)
            {
                case Technique.Colored:
                    m_D3DEffect.Technique = m_coloredTechnique;
                    break;

                case Technique.ColoredLit:
                    m_D3DEffect.Technique = m_coloredLitTechnique;
                    break;

                case Technique.Textured3D:
                    m_D3DEffect.Technique = m_textured3DTechnique;
                    break;

                default:
                    m_D3DEffect.Technique = m_defaultTechnique;
                    break;
            }
        }

        public override void Dispose()
        {
            PerlinBase.Dispose();

            base.Dispose();
        }
    }
}
