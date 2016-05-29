using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyThrust: IMyFunctionalBlock
    {
        float ThrustOverride { get;}
        float MaxThrust { get; }
        float CurrentThrust { get; }
    }
}
