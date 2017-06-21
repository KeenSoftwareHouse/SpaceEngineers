using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 555, typeof(MyObjectBuilder_SectorWeatherComponent))]
    public class MySectorWeatherComponent : MySessionComponentBase
    {
        // ReSharper disable CompareOfFloatsByEqualityOperator

        // Sun rotation speed
        private float m_speed;

        private Vector3 m_sunRotationAxis;
        private Vector3 m_baseSunDirection;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            var ob = (MyObjectBuilder_SectorWeatherComponent)sessionComponent;

            m_speed = MySession.Static.Settings.SunRotationIntervalMinutes;

            if (!ob.BaseSunDirection.IsZero)
            {
                m_baseSunDirection = ob.BaseSunDirection;
            }

            Enabled = MySession.Static.Settings.EnableSunRotation;
        }

        public override void BeforeStart()
        {
            // l1 norm
            var s = Math.Abs(m_baseSunDirection.X) + Math.Abs(m_baseSunDirection.Y) + Math.Abs(m_baseSunDirection.Z);

            if (s < 0.001) // If the total is too small
            {
                m_baseSunDirection = MySector.SunProperties.BaseSunDirectionNormalized;
                m_sunRotationAxis = MySector.SunProperties.SunRotationAxis;

                if (MySession.Static.ElapsedGameTime.Ticks != 0) // Calculate original base direction
                {
                    float angle = -2.0f * MathHelper.Pi * (float)(MySession.Static.ElapsedGameTime.TotalMinutes / m_speed);

                    var sunDirection = Vector3.Transform(m_baseSunDirection, Matrix.CreateFromAxisAngle(m_sunRotationAxis, angle));
                    sunDirection.Normalize();

                    m_baseSunDirection = sunDirection;
                }
            }
            else
            {
                m_sunRotationAxis = MySector.SunProperties.SunRotationAxis;
            }

            if (Enabled)
                MySector.SunProperties.SunDirectionNormalized = CalculateSunDirection();
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_SectorWeatherComponent)base.GetObjectBuilder();

            ob.BaseSunDirection = m_baseSunDirection;

            return ob;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!Enabled)
                return;
            var sunDirection = CalculateSunDirection();

            MySector.SunProperties.SunDirectionNormalized = sunDirection;
        }

        private Vector3 CalculateSunDirection()
        {
            float angle = 2.0f * MathHelper.Pi * (float)(MySession.Static.ElapsedGameTime.TotalMinutes / m_speed);

            var sunDirection = Vector3.Transform(m_baseSunDirection, Matrix.CreateFromAxisAngle(m_sunRotationAxis, angle));
            sunDirection.Normalize();
            return sunDirection;
        }

        private bool m_enabled;
        public bool Enabled
        {
            set
            {
                m_enabled = value;
            }
            get
            {
                return m_enabled;
            }
        }

        public float RotationInterval
        {
            set
            {
                m_speed = value;

                Enabled = m_speed != 0;
            }
            get { return m_speed; }
        }
    }
}
