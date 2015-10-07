using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Navigation
{
    public class MyStuckDetection
    {
        private static readonly int STUCK_COUNTDOWN = 60;

        private Vector3D m_translationStuckDetection;
        private Vector3 m_rotationStuckDetection;
        private float m_positionToleranceSq;
        private float m_rotationToleranceSq;

        private bool m_isRotating;
        private int m_counter;

        public bool IsStuck { get; private set; }

        public MyStuckDetection(float positionTolerance, float rotationTolerance)
        {
            m_positionToleranceSq = positionTolerance * positionTolerance;
            m_rotationToleranceSq = rotationTolerance * rotationTolerance;

            Reset();
        }

        public void SetRotating(bool rotating)
        {
            m_isRotating = rotating;
        }

        public void Update(Vector3D worldPosition, Vector3 rotation)
        {
            m_translationStuckDetection = m_translationStuckDetection * 0.8 + worldPosition * 0.2;
            m_rotationStuckDetection = m_rotationStuckDetection * 0.95f + rotation * 0.05f;

            bool isStuck = (m_translationStuckDetection - worldPosition).LengthSquared() < 0.0001// m_positionToleranceSq
                 && (m_rotationStuckDetection - rotation).LengthSquared() < 0.0001//m_rotationToleranceSq
                 && !m_isRotating;

            if (m_counter <= 0)
            {
                if (isStuck)
                {
                    IsStuck = true;
                }
                else
                {
                    m_counter = STUCK_COUNTDOWN;
                }

                return;
            }

            if (m_counter == STUCK_COUNTDOWN && !isStuck)
            {
                return;
            }

            m_counter--;
        }

        public void Reset()
        {
            m_translationStuckDetection = Vector3D.Zero;
            m_rotationStuckDetection = Vector3.Zero;
            IsStuck = false;
            m_counter = STUCK_COUNTDOWN;
            m_isRotating = false;
        }
    }
}
