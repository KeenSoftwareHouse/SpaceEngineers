
using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;
    using System;

    class MyEffectDirectionalLight : MyEffectShadowBase
    {
        public enum Technique
        {
            Default,
            WithoutShadows,
            NoLighting,
        }

        public Technique DefaultTechnique { get; set; }
        public Technique DefaultWithoutShadowsTechnique { get; set; }
        public Technique DefaultNoLightingTechnique { get; set; }

        readonly EffectHandle m_depthsRT;
        readonly EffectHandle m_normalsRT;
        readonly EffectHandle m_diffuseRT;
        readonly EffectHandle m_halfPixelAndScale;
        readonly EffectHandle m_lightColorAndIntensity;
        readonly EffectHandle m_backlightColorAndIntensity;


        readonly EffectHandle m_shadowHalfPixel;
        readonly EffectHandle m_lightDirection;
        readonly EffectHandle m_lightSpecularColor;

        readonly EffectHandle m_worldViewProjMatrix;
        readonly EffectHandle m_cameraMatrix;

        readonly EffectHandle m_frustumCorners;
        readonly EffectHandle m_enableCascadeBlending;

        readonly EffectHandle m_textureEnvironmentMain;
        readonly EffectHandle m_textureEnvironmentAux;
        readonly EffectHandle m_textureAmbientMain;
        readonly EffectHandle m_textureAmbientAux;
        readonly EffectHandle m_textureEnvironmentBlendFactor;
        readonly EffectHandle m_cameraPosition;

        readonly EffectHandle m_enableAmbientEnvironment;
        readonly EffectHandle m_enableReflectionEnvironment;
        readonly EffectHandle m_ambientMinimumAndIntensity;

        readonly EffectHandle m_nearSlopeBiasDistance;

        readonly EffectHandle m_lightingTechnique;
        readonly EffectHandle m_lightingWOShadowsTechnique;
        readonly EffectHandle m_noLightingTechnique;

        public MyEffectDirectionalLight()
            : base("Effects2\\Lights\\MyEffectDirectionalLight")
        {
            m_normalsRT = m_D3DEffect.GetParameter(null, "NormalsRT");
            m_diffuseRT = m_D3DEffect.GetParameter(null, "DiffuseRT");
            m_depthsRT = m_D3DEffect.GetParameter(null, "DepthsRT");
            m_halfPixelAndScale = m_D3DEffect.GetParameter(null, "HalfPixelAndScale");
            m_shadowHalfPixel = m_D3DEffect.GetParameter(null, "ShadowHalfPixel");

            m_lightDirection = m_D3DEffect.GetParameter(null, "LightDirection");
            m_lightColorAndIntensity = m_D3DEffect.GetParameter(null, "LightColorAndIntensity");
            m_lightSpecularColor = m_D3DEffect.GetParameter(null, "LightSpecularColor");
            m_backlightColorAndIntensity = m_D3DEffect.GetParameter(null, "BacklightColorAndIntensity");

            m_worldViewProjMatrix = m_D3DEffect.GetParameter(null, "WorldViewProjMatrix");
            m_cameraMatrix = m_D3DEffect.GetParameter(null, "CameraMatrix");

            m_frustumCorners = m_D3DEffect.GetParameter(null, "FrustumCorners");
            m_enableCascadeBlending = m_D3DEffect.GetParameter(null, "EnableCascadeBlending");

            m_textureEnvironmentMain = m_D3DEffect.GetParameter(null, "TextureEnvironmentMain");
            m_textureEnvironmentAux = m_D3DEffect.GetParameter(null, "TextureEnvironmentAux");
            m_textureAmbientMain = m_D3DEffect.GetParameter(null, "TextureAmbientMain");
            m_textureAmbientAux = m_D3DEffect.GetParameter(null, "TextureAmbientAux");
            m_textureEnvironmentBlendFactor = m_D3DEffect.GetParameter(null, "TextureEnvironmentBlendFactor");
            m_cameraPosition = m_D3DEffect.GetParameter(null, "CameraPosition");

            m_enableAmbientEnvironment = m_D3DEffect.GetParameter(null, "EnableAmbientEnv");
            m_enableReflectionEnvironment = m_D3DEffect.GetParameter(null, "EnableReflectionEnv");
            m_ambientMinimumAndIntensity = m_D3DEffect.GetParameter(null, "AmbientMinimumAndIntensity");

            m_nearSlopeBiasDistance = m_D3DEffect.GetParameter(null, "NearSlopeBiasDistance");

            DefaultTechnique = Technique.Default;
            DefaultWithoutShadowsTechnique= Technique.WithoutShadows;
            DefaultNoLightingTechnique = Technique.NoLighting;

            m_lightingTechnique = m_D3DEffect.GetTechnique("Technique_Lighting");
            m_lightingWOShadowsTechnique = m_D3DEffect.GetTechnique("Technique_LightingWithoutShadows");
            m_noLightingTechnique = m_D3DEffect.GetTechnique("Technique_NoLighting");

            SetTechnique(DefaultTechnique);

        }

        public void SetNormalsRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_normalsRT, renderTarget2D);
        }

        public void SetDiffuseRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_diffuseRT, renderTarget2D);
        }

        public void SetDepthsRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_depthsRT, renderTarget2D);
        }

        public void SetHalfPixelAndScale(int screenSizeX, int screenSizeY, Vector2 scale)
        {
            Vector2 halfPixel = MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY);
            m_D3DEffect.SetValue(m_halfPixelAndScale, new Vector4(halfPixel.X, halfPixel.Y, scale.X, scale.Y));
        }


        public void SetShadowHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_shadowHalfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetLightDirection(Vector3 lightDirection)
        {
            m_D3DEffect.SetValue(m_lightDirection, lightDirection);
        }

        public void SetLightColorAndIntensity(Vector3 lightColor, float intensity)
        {
            m_D3DEffect.SetValue(m_lightColorAndIntensity, new Vector4(lightColor.X, lightColor.Y, lightColor.Z, intensity));
        }

        public void SetSpecularLightColor(Vector3 lightColor)
        {
            m_D3DEffect.SetValue(m_lightSpecularColor, lightColor);
        }

        public void SetBacklightColorAndIntensity(Vector3 lightColor, float intensity)
        {
            m_D3DEffect.SetValue(m_backlightColorAndIntensity, new Vector4(lightColor.X, lightColor.Y, lightColor.Z, intensity));
        }

        public void SetWorldViewProjMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_worldViewProjMatrix, matrix);
        }

        public void SetCameraMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_cameraMatrix, matrix);
        }

        public void SetFrustumCorners(Vector3[] frustumCornersVS)
        {
            m_D3DEffect.SetValue(m_frustumCorners, frustumCornersVS);
        }

        public void EnableCascadeBlending(bool enable)
        {
            m_D3DEffect.SetValue(m_enableCascadeBlending, enable ? 1 : 0);
        }

        public void SetTextureEnvironmentMain(CubeTexture environmentMapMain)
        {
            m_D3DEffect.SetTexture(m_textureEnvironmentMain, environmentMapMain);
        }

        public void SetTextureEnvironmentAux(CubeTexture environmentMapAux)
        {
            m_D3DEffect.SetTexture(m_textureEnvironmentAux, environmentMapAux);
        }

        public void SetTextureAmbientMain(CubeTexture ambientMapMain)
        {
            m_D3DEffect.SetTexture(m_textureAmbientMain, ambientMapMain);
        }

        public void SetTextureAmbientAux(CubeTexture ambientMapAux)
        {
            m_D3DEffect.SetTexture(m_textureAmbientAux, ambientMapAux);
        }

        public void SetTextureEnvironmentBlendFactor(float blendFactor)
        {
            m_D3DEffect.SetValue(m_textureEnvironmentBlendFactor, blendFactor);
        }

        public void SetCameraPosition(Vector3 cameraPosition)
        {
            m_D3DEffect.SetValue(m_cameraPosition, cameraPosition);
        }

        /// <summary>
        /// Enables ambient environmental mapping.
        /// </summary>
        /// <param name="enable">True to enable ambient envrionmental mapping.</param>
        public void SetEnableAmbientEnvironment(bool enable)
        {
            m_D3DEffect.SetValue(m_enableAmbientEnvironment, enable);
        }

        /// <summary>
        /// Enables reflective environment mapping.
        /// </summary>
        /// <param name="enable">True to enable reflective environmental mapping.</param>
        public void SetEnableReflectionEnvironment(bool enable)
        {
            m_D3DEffect.SetValue(m_enableReflectionEnvironment, enable);
        }

        /// <summary>
        /// Sets ambient minimum, ambient is taken from environment map, if environment map is too dark, this value is used insted.
        /// </summary>
        /// <param name="minimumAmbient">Ambient minimum.</param>
        public void SetAmbientMinimumAndIntensity(Vector4 minimumAmbientAndIntensity)
        {
            m_D3DEffect.SetValue(m_ambientMinimumAndIntensity, minimumAmbientAndIntensity);
        }

        public void SetNearSlopeBiasDistance(float nearSlopeBiasDistance)
        {
            m_D3DEffect.SetValue(m_nearSlopeBiasDistance, nearSlopeBiasDistance);
        }


        public void SetTechnique(Technique technique)
        {
            switch (technique)
            {
                case Technique.Default:
                    m_D3DEffect.Technique = m_lightingTechnique;
                    break;

                case Technique.WithoutShadows:
                    m_D3DEffect.Technique = m_lightingWOShadowsTechnique;
                    break;

                case Technique.NoLighting:
                    m_D3DEffect.Technique = m_noLightingTechnique;
                    break;
                    
                default:
                    throw new InvalidBranchException();
            }
        }
    }   
}