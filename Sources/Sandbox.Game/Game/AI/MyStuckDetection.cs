using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI
{
    public class MyStuckDetection
    {
        private Vector3D m_translationStuckDetection;
        private Vector3 m_rotationStuckDetection;
        private static readonly float STUCK_ROTATION_RADIANS = MathHelper.ToRadians(2f);
        private static readonly float STUCK_ROTATION_RADIANS_SQ = STUCK_ROTATION_RADIANS * STUCK_ROTATION_RADIANS;

        public bool IsStuck { get; private set; }

        public MyStuckDetection()
        {
            Reset();
        }

        public void Update(Vector3D worldPosition, Vector3 rotation)
        {
            m_translationStuckDetection = m_translationStuckDetection * 0.5f + worldPosition * 0.5f;
            m_rotationStuckDetection = m_rotationStuckDetection * 0.5f + rotation * 0.5f;

            IsStuck = (m_translationStuckDetection - worldPosition).LengthSquared() < 0.0001f
                && (m_rotationStuckDetection - rotation).LengthSquared() < STUCK_ROTATION_RADIANS_SQ;
        }

        public void Reset()
        {
            m_translationStuckDetection = Vector3D.Zero;
            m_rotationStuckDetection = Vector3.Zero;
            IsStuck = false;
        }
    }
}
