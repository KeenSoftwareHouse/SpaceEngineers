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
        float Damping { get;}
        float Strength { get;}
        float Friction { get;}
        float Power { get; }
        float Height { get; }
    }
}
