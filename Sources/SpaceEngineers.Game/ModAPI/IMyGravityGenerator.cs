using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyGravityGenerator : IMyGravityGeneratorBase, Ingame.IMyGravityGenerator
    {
        /// <summary>
        /// Gets or sets the gravity field as a Vector3(W,H,D).
        /// </summary>
        /// <remarks>
        /// X is Width
        /// Y is Height
        /// Z is Depth
        /// This is not clamped like the Ingame one is.
        /// </remarks>
        new Vector3 FieldSize { get; set; }
    }
}
