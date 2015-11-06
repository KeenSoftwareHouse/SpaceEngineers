using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyLightingBlock : IMyFunctionalBlock
    {
        float Radius{ get;}
        float Intensity{get;}
        float BlinkIntervalSeconds{get;}
        float BlinkLength{get;}
        float BlinkOffset{get;}
    }
}
