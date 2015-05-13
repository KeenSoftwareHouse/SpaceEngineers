using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyGravityGeneratorSphere : IMyGravityGeneratorBase
    {
        float Radius { get; }
        float Gravity { get; }
    }
}
