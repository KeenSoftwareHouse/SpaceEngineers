using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyMotorSuspension : IMyMotorBase
    {
        bool Steering { get; }

        bool Propulsion { get; }

        bool InvertSteer { get; }

        bool InvertPropulsion { get; }

        float Damping { get; }

        float Strength { get; }

        float Friction { get; }

        float Power { get; }

        float Height { get; }

        /// <summary>
        /// Wheel's current steering angle
        /// </summary>
        float SteerAngle { get; }

        /// <summary>
        /// Max steering angle in radians.
        /// </summary>
        float MaxSteerAngle { get; }

        /// <summary>
        /// Speed at which wheel steers.
        /// </summary>
        float SteerSpeed { get; }

        /// <summary>
        /// Speed at which wheel returns from steering.
        /// </summary>
        float SteerReturnSpeed { get; }

        /// <summary>
        /// Suspension travel, value from 0 to 1.
        /// </summary>
        float SuspensionTravel { get; }

        /// <summary>
        /// Set/get brake applied to the wheel.
        /// </summary>
        bool Brake { get; set; }
    }
}
