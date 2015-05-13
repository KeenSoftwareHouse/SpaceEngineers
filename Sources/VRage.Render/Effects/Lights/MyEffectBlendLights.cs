using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectBlendLights : MyEffectBase
    {
        public enum Technique
        {
            LightsEnabled,
            LightsDisabled,
            OnlyLights,
            OnlySpecularIntensity,
            OnlySpecularPower,
            OnlyEmissivity,
            OnlyReflectivity,

            CopyEmissivity,
        }

        public Technique DefaultTechnique { get; set; }
        public Technique CopyEmissivityTechnique { get; set; }

        readonly EffectHandle m_Diffuse;
        readonly EffectHandle m_Normal;
        readonly EffectHandle m_Lights;
        readonly EffectHandle m_LightsMod;
        readonly EffectHandle m_LightsDiv;
        readonly EffectHandle m_Depth;
        readonly EffectHandle m_backgroundTexture;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_scale;
        readonly EffectHandle m_AmbientColor;

        readonly EffectHandle m_basicTechnique;
        readonly EffectHandle m_disableLightsTechnique;
        readonly EffectHandle m_onlyLightsTechnique;
        readonly EffectHandle m_onlySpecularIntensityTechnique;
        readonly EffectHandle m_onlySpecularPowerTechnique;
        readonly EffectHandle m_onlyEmissivityTechnique;
        readonly EffectHandle m_onlyReflectivityTechnique;
        readonly EffectHandle m_copyEmissivity;

        public MyEffectBlendLights()
            : base("Effects2\\Lights\\MyEffectBlendLights")
        {
            m_Diffuse = m_D3DEffect.GetParameter(null, "DiffuseTexture");
            m_Lights = m_D3DEffect.GetParameter(null, "LightTexture");
            m_LightsMod = m_D3DEffect.GetParameter(null, "LightTextureMod");
            m_LightsDiv = m_D3DEffect.GetParameter(null, "LightTextureDiv");
            m_Depth = m_D3DEffect.GetParameter(null, "DepthTexture");
            m_Normal = m_D3DEffect.GetParameter(null, "NormalsTexture");
            m_backgroundTexture = m_D3DEffect.GetParameter(null, "BackgroundTexture");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_scale = m_D3DEffect.GetParameter(null, "Scale");
            m_AmbientColor = m_D3DEffect.GetParameter(null, "AmbientColor");

            m_basicTechnique = m_D3DEffect.GetTechnique("BasicTechnique");
            m_disableLightsTechnique = m_D3DEffect.GetTechnique("DisableLightsTechnique");
            m_onlyLightsTechnique = m_D3DEffect.GetTechnique("OnlyLightsTechnique");
            m_onlySpecularIntensityTechnique = m_D3DEffect.GetTechnique("OnlySpecularIntensity");
            m_onlySpecularPowerTechnique = m_D3DEffect.GetTechnique("OnlySpecularPower");
            m_onlyEmissivityTechnique = m_D3DEffect.GetTechnique("OnlyEmissivity");
            m_onlyReflectivityTechnique = m_D3DEffect.GetTechnique("OnlyReflectivity");
            m_copyEmissivity = m_D3DEffect.GetTechnique("CopyEmissivity");

            DefaultTechnique = Technique.LightsEnabled;
            CopyEmissivityTechnique = Technique.CopyEmissivity;
            SetTechnique(DefaultTechnique);
        }

        public void SetDiffuseTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_Diffuse, renderTarget2D);
        }

        public void SetNormalTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_Normal, renderTarget2D);
        }

        public void SetLightsTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_Lights, renderTarget2D);
        }

        public void SetLightsModTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_LightsMod, renderTarget2D);
        }

        public void SetLightsDivTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_LightsDiv, renderTarget2D);
        }

        public void SetDepthTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_Depth, renderTarget2D);
        }

        public void SetBackgroundTexture(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_backgroundTexture, renderTarget2D);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetScale(Vector2 scale)
        {
            m_D3DEffect.SetValue(m_scale, scale);
        }

        public Vector3 AmbientColor
        {
            //get { return m_AmbientColor.GetValueVector3(); }
            set { m_D3DEffect.SetValue(m_AmbientColor, value); }
        }

        public void SetTechnique(Technique technique)
        {
            switch (technique)
            {
                case Technique.LightsEnabled:
                    m_D3DEffect.Technique = m_basicTechnique;
                    break;

                case Technique.LightsDisabled:
                    m_D3DEffect.Technique = m_disableLightsTechnique;
                    break;

                case Technique.OnlyLights:
                    m_D3DEffect.Technique = m_onlyLightsTechnique;
                    break;

                case Technique.OnlySpecularIntensity:
                    m_D3DEffect.Technique = m_onlySpecularIntensityTechnique;
                    break;

                case Technique.OnlySpecularPower:
                    m_D3DEffect.Technique = m_onlySpecularPowerTechnique;
                    break;

                case Technique.OnlyEmissivity:
                    m_D3DEffect.Technique = m_onlyEmissivityTechnique;
                    break;

                case Technique.OnlyReflectivity:
                    m_D3DEffect.Technique = m_onlyReflectivityTechnique;
                    break;

                case Technique.CopyEmissivity:
                    m_D3DEffect.Technique = m_copyEmissivity;
                    break;
            }
        }
    }
}
