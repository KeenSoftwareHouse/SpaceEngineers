using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectSSAO3 : MyEffectBase
    {
        readonly EffectHandle m_depthsRT;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_randomTexture;
        readonly EffectHandle m_viewMatrix;

        readonly EffectHandle m_params1;
        readonly EffectHandle m_params2;

        readonly EffectHandle m_scaleMin;
        readonly EffectHandle m_scaleMax;

        readonly EffectHandle m_depthSoftness;
        readonly EffectHandle m_zScale;

        bool m_useBlur = true;
        bool m_showOnlySSAO = false;

        public MyEffectSSAO3()
            : base("Effects2\\SSAO\\MyEffectSSAO3")
        {
            m_depthsRT = m_D3DEffect.GetParameter(null, "DepthsRT");
            m_randomTexture = m_D3DEffect.GetParameter(null, "RandomTexture");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_viewMatrix = m_D3DEffect.GetParameter(null, "ViewMatrix");

            m_scaleMin = m_D3DEffect.GetParameter(null, "scaleMin");
            m_scaleMax = m_D3DEffect.GetParameter(null, "scaleMax");

            m_depthSoftness = m_D3DEffect.GetParameter(null, "depthSoft");
            m_zScale = m_D3DEffect.GetParameter(null, "zScale");

            m_params1 = m_D3DEffect.GetParameter(null, "g_SSAOParams");
            m_params2 = m_D3DEffect.GetParameter(null, "g_SSAOParams2");
        }

        public void SetDepthsRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_depthsRT, renderTarget2D);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetRandomTexture(Texture randomTexture)
        {
            m_D3DEffect.SetTexture(m_randomTexture, randomTexture);
        }

        public void SetViewMatrix(Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_viewMatrix, viewMatrix);
        }

        public bool ShowOnlySSAO
        {
            get { return m_showOnlySSAO; }
            set { m_showOnlySSAO = value; }
        }

        public bool UseBlur
        {
            get { return m_useBlur; }
            set { m_useBlur = value; }
        }
      
        public float Fallof
        {
            get { return m_D3DEffect.GetValue<Vector4>(m_params1).Y; }
            set 
            { 
                Vector4 v = m_D3DEffect.GetValue<Vector4>(m_params1);
                m_D3DEffect.SetValue(m_params1, new Vector4(v.X, value, v.Z, v.W)); 
            }
        }

        public float Far
        {
            get { return m_D3DEffect.GetValue<Vector4>(m_params1).Z; }
            set 
            { 
                Vector4 v = m_D3DEffect.GetValue<Vector4>(m_params1);
                m_D3DEffect.SetValue(m_params1, new Vector4(v.X, v.Y, value, v.W)); 
            }
        }

        public float OccNorm
        {
            get { return m_D3DEffect.GetValue<Vector4>(m_params1).W; }
            set 
            { 
                Vector4 v = m_D3DEffect.GetValue<Vector4>(m_params1);
                m_D3DEffect.SetValue(m_params1, new Vector4(v.X, v.Y, v.Z, value)); 
            }
        }

        public float Bias
        {
            get { return m_D3DEffect.GetValue<Vector4>(m_params2).X; }
            set
            {
                Vector4 v = m_D3DEffect.GetValue<Vector4>(m_params2);
                m_D3DEffect.SetValue(m_params2, new Vector4(value, v.Y, v.Z, v.W));
            }
        }

        public float Falloff
        {
            get { return m_D3DEffect.GetValue<Vector4>(m_params2).Y; }
            set
            {
                Vector4 v = m_D3DEffect.GetValue<Vector4>(m_params2);
                m_D3DEffect.SetValue(m_params2, new Vector4(v.X, value, v.Z, v.W));
            }
        }

        public float ColorBias
        {
            get { return m_D3DEffect.GetValue<Vector4>(m_params2).Z; }
            set
            {
                Vector4 v = m_D3DEffect.GetValue<Vector4>(m_params2);
                m_D3DEffect.SetValue(m_params2, new Vector4(v.X, v.Y, value, v.W));
            }
        }

        public float ScaleMin
        {
            get { return m_D3DEffect.GetValue<float>(m_scaleMin); }
            set
            {
                m_D3DEffect.SetValue(m_scaleMin, value);
            }
        }

        public float ScaleMax
        {
            get { return m_D3DEffect.GetValue<float>(m_scaleMax); }
            set
            {
                m_D3DEffect.SetValue(m_scaleMax, value);
            }
        }

        public float DepthSoftness
        {
            get { return m_D3DEffect.GetValue<float>(m_depthSoftness); }
            set
            {
                m_D3DEffect.SetValue(m_depthSoftness, value);
            }
        }

        public float ZScale
        {
            get { return m_D3DEffect.GetValue<float>(m_zScale); }
            set
            {
                m_D3DEffect.SetValue(m_zScale, value);
            }
        }
    }
}
