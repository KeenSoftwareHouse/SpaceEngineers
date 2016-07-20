using System;
using Sandbox.Game.GameSystems;
using VRage.Game.Components;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public class MySphericalNaturalGravityComponent : MyGravityProviderComponent
    {
        const double GRAVITY_LIMIT_STRENGTH = 0.05;

        // Parameters
        private readonly double m_minRadius;
        private readonly double m_maxRadius;
        private readonly double m_falloff;
        private readonly double m_intensity;

        public Vector3D Position { get; private set; }

        private float m_gravityLimit;
        private float m_gravityLimitSq;

        public MySphericalNaturalGravityComponent(double minRadius, double maxRadius, double falloff, double intensity)
        {
            m_minRadius = minRadius;
            m_maxRadius = maxRadius;
            m_falloff = falloff;
            m_intensity = intensity;

            // Calculate range where gravity is negligible
            double s = intensity;
            double radius = maxRadius;
            double invFalloff = 1.0 / falloff;
            GravityLimit = (float)(radius * Math.Pow(s / GRAVITY_LIMIT_STRENGTH, invFalloff));
        }

        public override bool IsWorking
        {
            get { return true; }
        }

        public float GravityLimit
        {
            get { return m_gravityLimit; }
            private set
            {
                m_gravityLimitSq = value * value;
                m_gravityLimit = value;
            }
        }

        public float GravityLimitSq
        {
            get { return m_gravityLimitSq; }
            private set
            {
                m_gravityLimitSq = value;
                m_gravityLimit = (float)Math.Sqrt(value);
            }
        }

        public override bool IsPositionInRange(Vector3D worldPoint)
        {
            return (Position - worldPoint).LengthSquared() <= m_gravityLimitSq;
        }

        public override Vector3 GetWorldGravity(Vector3D worldPoint)
        {
            Vector3 direction = GetWorldGravityNormalized(ref worldPoint);
            var gravityMultiplier = GetGravityMultiplier(worldPoint);

            return direction * MyGravityProviderSystem.G * gravityMultiplier;
        }

        public override float GetGravityMultiplier(Vector3D worldPoint)
        {
            double distanceToCenter = (Position - worldPoint).Length();
            // The Gravity limit should be calculated so that the gravity cuts-off at GRAVITY_LIMIT_STRENGTH
            if (distanceToCenter > m_gravityLimit) return 0.0f;

            float attenuation = 1.0f;

            if (distanceToCenter > m_maxRadius)
            {
                attenuation = (float)Math.Pow(distanceToCenter / m_maxRadius, -m_falloff);
            }
            else if (distanceToCenter < m_minRadius)
            {
                attenuation = (float)(distanceToCenter / m_minRadius);
                if (attenuation < 0.01f)
                    attenuation = 0.01f;
            }

            return (float)(attenuation * m_intensity);
        }

        public Vector3 GetWorldGravityNormalized(ref Vector3D worldPoint)
        {
            Vector3 direction = Position - worldPoint;
            direction.Normalize();
            return direction;
        }

        public override string ComponentTypeDebugString
        {
            get { return GetType().Name; }
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            Position = Entity.PositionComp.GetPosition();
        }
    }
}