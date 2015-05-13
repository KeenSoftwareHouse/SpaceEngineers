using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Color = VRageMath.Color;

    internal class MyEffectDynamicLightingBase : MyEffectShadowBase
    {
        EffectHandle m_dynamicLightsCount;
        EffectHandle[] DynamicLightsPosition;
        EffectHandle[] DynamicLightsColor;
        EffectHandle[] DynamicLightsFalloff;
        EffectHandle[] DynamicLightsRange;

        EffectHandle m_sunColor;
        EffectHandle m_sunIntensity;
        EffectHandle m_directionToSun;
        EffectHandle m_ambientColor;


        public MyEffectDynamicLightingBase(Effect xnaEffect)
            : base(xnaEffect)
        {
            Init();
        }

        public MyEffectDynamicLightingBase(string asset)
            : base(asset)
        {
            Init();
        }

        private void Init()
        {
              m_dynamicLightsCount = m_D3DEffect.GetParameter(null, "DynamicLightsCount");

            m_sunColor = m_D3DEffect.GetParameter(null, "SunColor");
            m_sunIntensity = m_D3DEffect.GetParameter(null, "SunIntensity");
            m_directionToSun = m_D3DEffect.GetParameter(null, "DirectionToSun");
            m_ambientColor = m_D3DEffect.GetParameter(null, "AmbientColor");


            //  Dynamic lights array
            EffectHandle dynamicLights = m_D3DEffect.GetParameter(null, "DynamicLights");
            if (dynamicLights != null)
            {
                DynamicLightsPosition = new EffectHandle[MyLightsConstants.MAX_LIGHTS_FOR_EFFECT];
                DynamicLightsColor = new EffectHandle[MyLightsConstants.MAX_LIGHTS_FOR_EFFECT];
                DynamicLightsFalloff = new EffectHandle[MyLightsConstants.MAX_LIGHTS_FOR_EFFECT];
                DynamicLightsRange = new EffectHandle[MyLightsConstants.MAX_LIGHTS_FOR_EFFECT];
                for (int i = 0; i < MyLightsConstants.MAX_LIGHTS_FOR_EFFECT; i++)
                {
                    DynamicLightsPosition[i] = m_D3DEffect.GetParameter(m_D3DEffect.GetParameterElement(dynamicLights, i), "Position");
                    DynamicLightsColor[i] = m_D3DEffect.GetParameter(m_D3DEffect.GetParameterElement(dynamicLights, i), "Color");
                    DynamicLightsFalloff[i] = m_D3DEffect.GetParameter(m_D3DEffect.GetParameterElement(dynamicLights, i), "Falloff");
                    DynamicLightsRange[i] = m_D3DEffect.GetParameter(m_D3DEffect.GetParameterElement(dynamicLights, i), "Range");
                }
            }
        }

        public void SetDynamicLightsCount(int dynamicLightsCount)
        {
            m_D3DEffect.SetValue(m_dynamicLightsCount, dynamicLightsCount);
        }

        public void SetSunColor(Vector3 sunColor)
        {
            m_D3DEffect.SetValue(m_sunColor, sunColor);
    }

        public void SetSunIntensity(float sunIntensity)
        {
            m_D3DEffect.SetValue(m_sunIntensity, sunIntensity);
}

        public void SetDirectionToSun(Vector3 directionToSun)
        {
            m_D3DEffect.SetValue(m_directionToSun, directionToSun);
        }

        public void SetAmbientColor(Vector3 ambient)
        {
            m_D3DEffect.SetValue(m_ambientColor, ambient);
        }

        public void SetDynamicLightsPosition(int index, Vector3 pos)
        {
            m_D3DEffect.SetValue(DynamicLightsPosition[index], pos);
        }

        public void SetDynamicLightsColor(int index, Vector4 color)
        {
            m_D3DEffect.SetValue(DynamicLightsColor[index], color);
        }

        public void SetDynamicLightsFalloff(int index, float falloff)
        {
            m_D3DEffect.SetValue(DynamicLightsFalloff[index], falloff);
        }

        public void SetDynamicLightsRange(int index, float range)
        {
            m_D3DEffect.SetValue(DynamicLightsRange[index], range);
        }
    }
}
