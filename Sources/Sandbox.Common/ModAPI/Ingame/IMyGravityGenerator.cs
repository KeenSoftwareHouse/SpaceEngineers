using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyGravityGenerator : IMyGravityGeneratorBase
    {
        float FieldWidth { get; }
        float FieldHeight { get; }
        float FieldDepth { get; }
        float Gravity { get; }
    }
}
