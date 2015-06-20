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

        /// <summary>
        /// The gravity in Gs, from -1 to 1.
        /// </summary>
        float Gravity { get; }
    }
}
