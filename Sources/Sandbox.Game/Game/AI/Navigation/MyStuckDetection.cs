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
        private static readonly int LONGTERM_COUNTDOWN = 60 * 5; // 5 seconds
        private static readonly double LONGTERM_TOLERANCE = 0.025; // 0.5 meters per 5 seconds means that the bot is stuck

        private Vector3D m_translationStuckDetection;
        private Vector3D m_longTermTranslationStuckDetection;
        private Vector3 m_rotationStuckDetection;
        private float m_positionToleranceSq;
        private float m_rotationToleranceSq;

        private bool m_isRotating;
        private int m_counter;
        private int m_longTermCounter;
        private int m_tickCounter;
        private int m_stoppedTime;

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

            bool isStuck = (m_translationStuckDetection - worldPosition).LengthSquared() < m_positionToleranceSq
                 && (m_rotationStuckDetection - rotation).LengthSquared() < m_rotationToleranceSq
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
            }
            else if (m_counter == STUCK_COUNTDOWN && !isStuck)
            {
                IsStuck = false;
                return;
            }
            else
            {
                m_counter--;
            }

            if (m_longTermCounter <= 0)
            {
                if ((m_longTermTranslationStuckDetection - worldPosition).LengthSquared() < LONGTERM_TOLERANCE)
                {
                    IsStuck = true;
                }
                else
                {
                    m_longTermCounter = LONGTERM_COUNTDOWN;
                    m_longTermTranslationStuckDetection = worldPosition;
                }
            }
            else
            {
                m_longTermCounter--;
            }

        }

        public void Reset()
        {
            // Only reset the stuck detection if we were stopped in a different frame
            if (m_stoppedTime != m_tickCounter)
            {
                m_translationStuckDetection = Vector3D.Zero;
                m_rotationStuckDetection = Vector3.Zero;
                IsStuck = false;
                m_counter = STUCK_COUNTDOWN;
                m_longTermCounter = LONGTERM_COUNTDOWN;
                m_isRotating = false;
            }
        }

        public void Stop()
        {
            m_stoppedTime = m_tickCounter;
        }

        public void SetCurrentTicks(int behaviorTicks)
        {
            m_tickCounter = behaviorTicks;
        }
    }
}
