using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyGravityGeneratorSphere : IMyGravityGeneratorBase
    {
        float Radius { get; }

        /// <summary>
        /// The gravity in Gs, from -1 to 1.
        /// </summary>
        float Gravity { get; }
    }
}
