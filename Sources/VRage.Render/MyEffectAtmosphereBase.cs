using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public MyEffectAtmosphere()
            : base("Effects2\\Models\\MyEffectAtmosphere")
        {
            m_viewMatrix = m_D3DEffect.GetParameter(null, "ViewMatrix");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_projectionMatrix = m_D3DEffect.GetParameter(null, "ProjectionMatrix");

            m_normalTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormal");
        
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

        }

    }
}
