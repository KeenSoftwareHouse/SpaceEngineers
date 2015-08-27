using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender.Effects
{
    class MyEffectAtmosphereBase : MyEffectBase
    {
        protected readonly EffectHandle m_cameraHeight;
        protected readonly EffectHandle m_cameraHeight2;
        protected readonly EffectHandle m_cameraPos;
        protected readonly EffectHandle m_outerRadius;
        protected readonly EffectHandle m_outerRadius2;
        protected readonly EffectHandle m_innerRadius;
        protected readonly EffectHandle m_innerRadius2;

        protected readonly EffectHandle m_scaleAtmosphere;
        protected readonly EffectHandle m_scaleDepth;
        protected readonly EffectHandle m_scaleOverScaleDepth;

        protected readonly EffectHandle m_isInside;
        protected readonly EffectHandle m_lightPosition;

        protected readonly EffectHandle m_invWavelength;

        public MyEffectAtmosphereBase(string effectPath)
            : base(effectPath)
        {
            m_cameraHeight2 = m_D3DEffect.GetParameter(null, "CameraHeight2");
            m_cameraHeight = m_D3DEffect.GetParameter(null, "CameraHeight");
            m_cameraPos = m_D3DEffect.GetParameter(null, "CameraPos");

            m_outerRadius = m_D3DEffect.GetParameter(null, "OuterRadius");
            m_outerRadius2 = m_D3DEffect.GetParameter(null, "OuterRadius2");
            m_innerRadius = m_D3DEffect.GetParameter(null, "InnerRadius");
            m_innerRadius2 = m_D3DEffect.GetParameter(null, "InnerRadius2");

            m_scaleAtmosphere = m_D3DEffect.GetParameter(null, "ScaleAtmosphere");

            m_scaleDepth = m_D3DEffect.GetParameter(null, "ScaleDepth");

            m_scaleOverScaleDepth = m_D3DEffect.GetParameter(null, "ScaleOverScaleDepth");

            m_lightPosition = m_D3DEffect.GetParameter(null, "LightPosition");

            m_isInside = m_D3DEffect.GetParameter(null, "IsInAtmosphere");

            m_invWavelength = m_D3DEffect.GetParameter(null, "InvWavelength");

        }

        public void SetInnerRadius(float innerRadius)
        {
            m_D3DEffect.SetValue(m_innerRadius, innerRadius);
            m_D3DEffect.SetValue(m_innerRadius2, innerRadius * innerRadius);
        }

        public void SetOutherRadius(float outerRadius)
        {
            m_D3DEffect.SetValue(m_outerRadius, outerRadius);
            m_D3DEffect.SetValue(m_outerRadius2, outerRadius * outerRadius);
        }

        public void SetRelativeCameraPos(Vector3 cameraPos)
        {
            m_D3DEffect.SetValue(m_cameraPos, cameraPos);
            float height = cameraPos.Length();
            m_D3DEffect.SetValue(m_cameraHeight2, height * height);
            m_D3DEffect.SetValue(m_cameraHeight, height);
        }

        public void SetLightPos(Vector3 lightPos)
        {
            m_D3DEffect.SetValue(m_lightPosition, Vector3.Normalize(lightPos));
        }

        public void SetScaleDepth(float scaleDepth)
        {
            m_D3DEffect.SetValue(m_scaleDepth, scaleDepth);
        }

        public void SetScaleAtmosphere(float scaleAtmosphere)
        {
            m_D3DEffect.SetValue(m_scaleAtmosphere, scaleAtmosphere);
        }

        public void SetScaleAtmosphereOverScaleDepth(float scale)
        {
            m_D3DEffect.SetValue(m_scaleOverScaleDepth, scale);
        }

        public void SetIsInside(bool isInside)
        {
            m_D3DEffect.SetValue(m_isInside, isInside);
        }

        public void SetWavelength(Vector3 wavelenght)
        {
            m_D3DEffect.SetValue(m_invWavelength, wavelenght);
        }
 
    }
}
