using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyMotorStator : IMyMotorBase
    {
        bool IsLocked { get; }
        float Angle { get; }
        float Torque { get; }
        float BrakingTorque { get; }
        float Velocity { get; }
        float LowerLimit { get; }
        float UpperLimit { get; }
        float Displacement { get; }
    }

}
