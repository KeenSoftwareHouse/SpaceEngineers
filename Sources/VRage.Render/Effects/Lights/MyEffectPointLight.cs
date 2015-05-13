
using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;
    using System;

    class MyEffectPointLight : MyEffectSpotShadowBase
    {
        public enum MyEffectPointLightTechnique
        {
            Point,    // for Point and Hemisphere
            PointShadows, //shadows from reflector
            Spot,
            SpotShadows,
        }

        public MyEffectPointLightTechnique PointTechnique { get; set; }
        public MyEffectPointLightTechnique PointWithShadowsTechnique { get; set; }
        public MyEffectPointLightTechnique HemisphereTechnique { get; set; }
        public MyEffectPointLightTechnique SpotTechnique { get; set; }
        public MyEffectPointLightTechnique SpotShadowTechnique { get; set; }


        readonly EffectHandle m_depthsRT;
        readonly EffectHandle m_normalsRT;
        readonly EffectHandle m_diffuseRT;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_scale;
        readonly EffectHandle m_lightPosition;
        readonly EffectHandle m_lightRadius;
        readonly EffectHandle m_lightColor;
        readonly EffectHandle m_lightSpecularColor;
        readonly EffectHandle m_lightIntensity;
        readonly EffectHandle m_lightFalloff;
        readonly EffectHandle m_viewMatrix;

        readonly EffectHandle m_worldViewProjMatrix;
        readonly EffectHandle m_viewProjMatrix;
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_lightViewProjection;

        readonly EffectHandle m_cameraPosition;
        readonly EffectHandle m_reflectorDirection;
        readonly EffectHandle m_reflectorConeMaxAngleCos;
        readonly EffectHandle m_reflectorColor;
        readonly EffectHandle m_reflectorRange;
        readonly EffectHandle m_reflectorIntensity;
        readonly EffectHandle m_reflectorFalloff;
        readonly EffectHandle m_reflectorTexture;
        readonly EffectHandle m_reflectorTextureEnabled;

        readonly EffectHandle m_nearSlopeBiasDistance;

        readonly EffectHandle m_pointTechnique;
        readonly EffectHandle m_pointShadowsTechnique;
        readonly EffectHandle m_spotTechnique;
        readonly EffectHandle m_spotShadowsTechnique;

        public MyEffectPointLight()
            : base("Effects2\\Lights\\MyEffectPointLight")
        {
            m_normalsRT = m_D3DEffect.GetParameter(null, "NormalsRT");
            m_diffuseRT = m_D3DEffect.GetParameter(null, "DiffuseRT");
            m_depthsRT = m_D3DEffect.GetParameter(null, "DepthsRT");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_scale = m_D3DEffect.GetParameter(null, "Scale");

            m_lightPosition = m_D3DEffect.GetParameter(null, "LightPosition");
            m_lightRadius = m_D3DEffect.GetParameter(null, "LightRadius");
            m_lightColor = m_D3DEffect.GetParameter(null, "LightColor");
            m_lightSpecularColor = m_D3DEffect.GetParameter(null, "LightSpecularColor");
            m_lightIntensity = m_D3DEffect.GetParameter(null, "LightIntensity");
            m_lightFalloff = m_D3DEffect.GetParameter(null, "Falloff");

            m_worldViewProjMatrix = m_D3DEffect.GetParameter(null, "WorldViewProjMatrix");
            m_viewProjMatrix = m_D3DEffect.GetParameter(null, "ViewProjMatrix");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_viewMatrix = m_D3DEffect.GetParameter(null, "ViewMatrix");
            m_lightViewProjection = m_D3DEffect.GetParameter(null, "LightViewProjection");
            
            m_cameraPosition = m_D3DEffect.GetParameter(null, "CameraPosition");
            m_reflectorDirection = m_D3DEffect.GetParameter(null, "ReflectorDirection");
            m_reflectorConeMaxAngleCos = m_D3DEffect.GetParameter(null, "ReflectorConeMaxAngleCos");
            m_reflectorColor = m_D3DEffect.GetParameter(null, "ReflectorColor");
            m_reflectorRange = m_D3DEffect.GetParameter(null, "ReflectorRange");
            m_reflectorIntensity = m_D3DEffect.GetParameter(null, "ReflectorIntensity");
            m_reflectorFalloff = m_D3DEffect.GetParameter(null, "ReflectorFalloff");
            m_reflectorTexture = m_D3DEffect.GetParameter(null, "ReflectorTexture");
            m_reflectorTextureEnabled = m_D3DEffect.GetParameter(null, "ReflectorTextureEnabled");

            m_nearSlopeBiasDistance = m_D3DEffect.GetParameter(null, "NearSlopeBiasDistance");

            m_pointTechnique = m_D3DEffect.GetTechnique("Technique_Point");
            m_pointShadowsTechnique = m_D3DEffect.GetTechnique("Technique_PointShadows");
            m_spotTechnique = m_D3DEffect.GetTechnique("Technique_Spot");
            m_spotShadowsTechnique = m_D3DEffect.GetTechnique("Technique_SpotShadows");

            // Reflector texture disabled unless set
            m_D3DEffect.SetValue(m_reflectorTextureEnabled, false);

            PointTechnique = MyEffectPointLightTechnique.Point;
            HemisphereTechnique = MyEffectPointLightTechnique.Point;
            PointWithShadowsTechnique = MyEffectPointLightTechnique.PointShadows;
            SpotTechnique = MyEffectPointLightTechnique.Spot;
            SpotShadowTechnique = MyEffectPointLightTechnique.SpotShadows;

            SetTechnique(PointTechnique);
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

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetScale(Vector2 scale)
        {
            m_D3DEffect.SetValue(m_scale, scale);
        }

        public void SetLightPosition(Vector3 lightPosition)
        {
            m_D3DEffect.SetValue(m_lightPosition, lightPosition);
        }

        public void SetLightColor(Vector3 lightColor)
        {
            m_D3DEffect.SetValue(m_lightColor, lightColor);
        }

        public void SetSpecularLightColor(Vector3 lightColor)
        {
            m_D3DEffect.SetValue(m_lightSpecularColor, lightColor);
        }

        public void SetLightIntensity(float lightIntensity)
        {
            m_D3DEffect.SetValue(m_lightIntensity, lightIntensity);
        }

        public void SetFalloff(float falloff)
        {
            m_D3DEffect.SetValue(m_lightFalloff, falloff);
        }

        public void SetLightRadius(float lightRadius)
        {
            m_D3DEffect.SetValue(m_lightRadius, lightRadius);
        }

        public void SetViewProjMatrix(ref Matrix matrix)
        {
            m_D3DEffect.SetValue(m_viewProjMatrix, matrix);
        }

        public void SetWorldViewProjMatrix(ref Matrix matrix)
        {
            m_D3DEffect.SetValue(m_worldViewProjMatrix, matrix);
        }

        public void SetWorldMatrix(ref Matrix matrix)
        {
            m_D3DEffect.SetValue(m_worldMatrix, matrix);
        }

        public void SetTechnique(MyEffectPointLightTechnique technique)
        {
            switch (technique)
            {
                case MyEffectPointLightTechnique.Point:
                    m_D3DEffect.Technique = m_pointTechnique;
                    break;

                case MyEffectPointLightTechnique.PointShadows:
                    m_D3DEffect.Technique = m_pointShadowsTechnique;
                    break;

                case MyEffectPointLightTechnique.Spot:
                    m_D3DEffect.Technique = m_spotTechnique;
                    break;

                case MyEffectPointLightTechnique.SpotShadows:
                    m_D3DEffect.Technique = m_spotShadowsTechnique;
                    break;

                default:
                    throw new InvalidBranchException();
            }
        }

        public void SetCameraPosition(Vector3 cameraPosition)
        {
            m_D3DEffect.SetValue(m_cameraPosition, cameraPosition);
        }
        
        public void SetReflectorDirection(Vector3 reflectorDirection)
        {
            m_D3DEffect.SetValue(m_reflectorDirection, reflectorDirection);
        }

        public void SetReflectorConeMaxAngleCos(float reflectorConeMax)
        {
            m_D3DEffect.SetValue(m_reflectorConeMaxAngleCos, reflectorConeMax);
        }

        public void SetReflectorColor(Vector4 reflectorColor)
        {
            m_D3DEffect.SetValue(m_reflectorColor, reflectorColor);
        }

        public void SetReflectorRange(float reflectorRange)
        {
            m_D3DEffect.SetValue(m_reflectorRange, reflectorRange);
        }

        public void SetReflectorIntensity(float reflectorIntensity)
        {
            m_D3DEffect.SetValue(m_reflectorIntensity, reflectorIntensity);
        }

        public void SetReflectorFalloff(float reflectorFalloff)
        {
            m_D3DEffect.SetValue(m_reflectorFalloff, reflectorFalloff);
        }

        public void SetReflectorTexture(Texture reflectorTexture)
        {
            m_D3DEffect.SetTexture(m_reflectorTexture, reflectorTexture);
            m_D3DEffect.SetValue(m_reflectorTextureEnabled, reflectorTexture != null ? 1 : 0);
        }

        public void SetViewMatrix(Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_viewMatrix, viewMatrix);
        }

        // Sets light view projection matrix for texturing purposes (reflector texture)
        public void SetLightViewProjection(Matrix lightViewProjection)
        {
            m_D3DEffect.SetValue(m_lightViewProjection, lightViewProjection);
        }

        public void SetNearSlopeBiasDistance(float nearSlopeBiasDistance)
        {
            m_D3DEffect.SetValue(m_nearSlopeBiasDistance, nearSlopeBiasDistance);
        }
    }   
}