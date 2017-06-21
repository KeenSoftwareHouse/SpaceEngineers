using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyLightingBlock : IMyFunctionalBlock
    {
        float Radius { get; }
        float ReflectorRadius { get; }
        float Intensity { get; }
        float BlinkIntervalSeconds{get;}
        [Obsolete("Use BlinkLength instead.")]
        float BlinkLenght { get; }
        float BlinkLength { get; }
        float BlinkOffset{get;}
    }
}
