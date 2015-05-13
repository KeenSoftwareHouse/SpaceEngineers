using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using VRageMath.Graphics;
using VRage.Utils;
//using VRageMath;

using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;

    internal class MyEffectReflectorBase : MyEffectShadowBase
    {
        EffectHandle m_cameraPosition;
        EffectHandle m_reflectorDirection;
        EffectHandle m_reflectorConeMaxAngleCos;
        EffectHandle m_reflectorColor;
        EffectHandle m_reflectorRange;

        public MyEffectReflectorBase(Effect d3dEffect)
            : base(d3dEffect)
        {
            Init();
        }

        public MyEffectReflectorBase(string asset)
            : base(asset)
        {
            Init();
        }

        private void Init()
        {
            m_cameraPosition = m_D3DEffect.GetParameter(null, "CameraPosition");
            m_reflectorDirection = m_D3DEffect.GetParameter(null, "ReflectorDirection");
            m_reflectorConeMaxAngleCos = m_D3DEffect.GetParameter(null, "ReflectorConeMaxAngleCos");
            m_reflectorColor = m_D3DEffect.GetParameter(null, "ReflectorColor");
            m_reflectorRange = m_D3DEffect.GetParameter(null, "ReflectorRange");

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

    }
}
