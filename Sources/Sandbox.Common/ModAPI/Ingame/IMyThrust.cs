using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyThrust: IMyFunctionalBlock
    {
        /// <summary>
        /// Gets the override thrust amount, in Newtons (N)
        /// </summary>
        float ThrustOverride { get; }

        /// <summary>
        /// Gets the maximum thrust amount, in Newtons (N)
        /// </summary>
        float MaxThrust { get; }

        /// <summary>
        /// Gets the current thrust amount, in Newtons (N)
        /// </summary>
        float CurrentThrust { get; }
    }
}
