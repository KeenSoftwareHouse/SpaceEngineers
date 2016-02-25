using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.ModAPI.Ingame;
using Havok;

namespace Sandbox.Game.Entities.Cube
{
    partial class MyMotorSuspension : Sandbox.ModAPI.IMyMotorSuspension
    {
        bool IMyMotorSuspension.Steering { get { return Steering; } }
        bool IMyMotorSuspension.Propulsion { get { return Propulsion; } }
        bool IMyMotorSuspension.InvertSteer { get { return InvertSteer; } }
        bool IMyMotorSuspension.InvertPropulsion { get { return InvertPropulsion; } }
        float IMyMotorSuspension.Damping { get { return GetDampingForTerminal(); } }
        float IMyMotorSuspension.Strength { get { return GetStrengthForTerminal(); } }
        float IMyMotorSuspension.Friction { get { return GetFrictionForTerminal(); } }
        float IMyMotorSuspension.Power { get { return GetPowerForTerminal(); } }
        float IMyMotorSuspension.Height { get { return GetHeightForTerminal(); } }
        float IMyMotorSuspension.SteerAngle { get { return m_steerAngle; } }
        float IMyMotorSuspension.MaxSteerAngle { get { return GetMaxSteerAngleForTerminal(); } }
        float IMyMotorSuspension.SteerSpeed { get { return GetSteerSpeedForTerminal(); } }
        float IMyMotorSuspension.SteerReturnSpeed { get { return GetSteerReturnSpeedForTerminal(); } }
        float IMyMotorSuspension.SuspensionTravel { get { return GetSuspensionTravelForTerminal(); } }
    }
}
