using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyPistonBase : IMyFunctionalBlock
    {
        /// <summary>
        /// Param - limit is top
        /// </summary>
        float Velocity { get; }
        float MinLimit { get; }
        float MaxLimit { get; }
    }
}
