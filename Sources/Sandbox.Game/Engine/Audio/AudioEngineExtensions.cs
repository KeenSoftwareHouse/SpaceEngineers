using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.X3DAudio;
using VRageMath;
using Sandbox.Engine.Platform;
using Sandbox.Game.World;
using Sandbox.CommonLib.ObjectBuilders.Audio;
using Sandbox.Game.Entities.Character;

namespace Sandbox.Engine.Audio
{
    public static class AudioEngineExtensions
    {
        /// <summary>
        /// Curve y = 1 - x in [0..1]
        /// </summary>
        static CurvePoint[] CURVE_LINEAR = new CurvePoint[] 
        {
            new CurvePoint() { Distance = 0f, DspSetting = 1f },
            new CurvePoint() { Distance = 1f, DspSetting = 0f }
        };

        /// <summary>
        /// Curve y = 1 - x^2 in [0..1]
        /// </summary>
        static CurvePoint[] CURVE_QUADRATIC = new CurvePoint[] 
        {
            new CurvePoint() { Distance = 0f, DspSetting = 1f },
            new CurvePoint() { Distance = 0.25f, DspSetting = 0.9375f },
            new CurvePoint() { Distance = 0.5f, DspSetting = 0.75f },
            new CurvePoint() { Distance = 0.75f, DspSetting = 0.4375f },
            new CurvePoint() { Distance = 1f, DspSetting = 0f }
        };

        /// <summary>
        /// Curve y = (1 - x)^2 in [0..1]
        /// </summary>
        static CurvePoint[] CURVE_INVQUADRATIC = new CurvePoint[] 
        {
            new CurvePoint() { Distance = 0f, DspSetting = 1f },
            new CurvePoint() { Distance = 0.25f, DspSetting = 0.5625f },
            new CurvePoint() { Distance = 0.5f, DspSetting = 0.25f },
            new CurvePoint() { Distance = 0.75f, DspSetting = 0.0625f },
            new CurvePoint() { Distance = 1f, DspSetting = 0f }
        };

        static CurvePoint[] CURVE_CUSTOM_1 = new CurvePoint[] 
        {
            new CurvePoint() { Distance = 0f, DspSetting = 1f },
            new CurvePoint() { Distance = 0.038462f, DspSetting = 0.979592f },
            new CurvePoint() { Distance = 0.384615f, DspSetting = 0.938776f },
            new CurvePoint() { Distance = 0.576923f, DspSetting = 0.928571f },
            new CurvePoint() { Distance = 0.769231f, DspSetting = 0.826531f },
            new CurvePoint() { Distance = 1f, DspSetting = 0f }
        };

        static CurvePoint[][] m_curves;
        static Cone m_cone;
        static MyAudioHelpers.CurveType m_currVolumeCurve = MyAudioHelpers.CurveType.Poly2;

        static AudioEngineExtensions()
        {
            m_curves = new CurvePoint[4][] { CURVE_LINEAR, CURVE_QUADRATIC, CURVE_INVQUADRATIC, CURVE_CUSTOM_1 };

            m_cone = new Cone();
            m_cone.InnerAngle = 0.5f * SharpDX.AngleSingle.RightAngle.Radians;
            m_cone.OuterAngle = 2f * m_cone.InnerAngle;
            m_cone.InnerVolume = 1f;
            m_cone.OuterVolume = 0.5f;
        }

        /// <summary>
        /// Sets default values for emitter, makes it valid
        /// </summary>
        public static void SetDefaultValues(this Emitter emitter)
        {
            emitter.Position = SharpDX.Vector3.Zero;
            emitter.Velocity = SharpDX.Vector3.Zero;
            emitter.OrientFront = SharpDX.Vector3.UnitZ;
            emitter.OrientTop = SharpDX.Vector3.UnitY;
            emitter.ChannelCount = 1;
            emitter.CurveDistanceScaler = float.MinValue;

            emitter.Cone = m_cone;
        }

        /// <summary>
        /// Sets default values for listener, makes it valid
        /// </summary>
        public static void SetDefaultValues(this Listener listener)
        {
            listener.Position = SharpDX.Vector3.Zero;
            listener.Velocity = SharpDX.Vector3.Zero;
            listener.OrientFront = SharpDX.Vector3.UnitZ;
            listener.OrientTop = SharpDX.Vector3.UnitY;
        }

        public static void UpdateFromMainCamera(this Listener listener)
        {
            if (MySector.MainCamera == null)
                return;

            listener.Position = SharpDXHelper.ToSharpDX(MySector.MainCamera.Position);
            listener.OrientFront = -SharpDXHelper.ToSharpDX(MySector.MainCamera.ForwardVector);
            listener.OrientTop = SharpDXHelper.ToSharpDX(MySector.MainCamera.UpVector);
            listener.Velocity = SharpDXHelper.ToSharpDX(MySector.MainCamera.Velocity);
        }

        public static bool UpdateFromPlayer(this Listener listener)
        {
            if ((MySession.Player == null) || (MySession.Player.PlayerEntity == null) || (MySession.Player.PlayerEntity.Entity == null))
                return false;

            if (MySession.Player.PlayerEntity is MyCharacter)
                listener.Position = SharpDXHelper.ToSharpDX(MySession.Player.PlayerEntity.Entity.GetPosition() + 1f * MySession.Player.PlayerEntity.Entity.WorldMatrix.Up);
            else
                listener.Position = SharpDXHelper.ToSharpDX(MySession.Player.PlayerEntity.Entity.GetPosition());

            listener.OrientFront = -SharpDXHelper.ToSharpDX(MySession.Player.PlayerEntity.Entity.WorldMatrix.Forward);
            listener.OrientTop = SharpDXHelper.ToSharpDX(MySession.Player.PlayerEntity.Entity.WorldMatrix.Up);
            listener.Velocity = SharpDXHelper.ToSharpDX(MySession.Player.PlayerEntity.Entity.Physics.LinearVelocity);
            return true;
        }

        /// <summary>
        /// Updates emitter position, forward, up and velocity
        /// </summary>
        public static void UpdateValues(this Emitter emitter, Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity, MyObjectBuilder_CueDefinition cue, int channelsCount)
        {
            emitter.Position = SharpDXHelper.ToSharpDX(position);
            emitter.OrientFront = SharpDXHelper.ToSharpDX(forward);
            emitter.OrientTop = SharpDXHelper.ToSharpDX(up);
            emitter.Velocity = SharpDXHelper.ToSharpDX(velocity);

            emitter.DopplerScaler = 1f;
            emitter.CurveDistanceScaler = cue.MaxDistance;
            if (m_currVolumeCurve != cue.VolumeCurve)
            {
                emitter.VolumeCurve = m_curves[(int)cue.VolumeCurve];
                m_currVolumeCurve = cue.VolumeCurve;
            }

            emitter.InnerRadius = (channelsCount > 2) ? cue.MaxDistance : 0f;
            emitter.InnerRadiusAngle = (channelsCount > 2) ? 0.5f * SharpDX.AngleSingle.RightAngle.Radians : 0f;
        }
    }
}
