using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI.Ingame;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyPistonTop : IMyCubeBlock
    {
        bool IsAttached { get; }
    }
}
