using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyGravityGeneratorSphere : IMyGravityGeneratorBase, Ingame.IMyGravityGeneratorSphere
    {
        /// <summary>
        /// Radius of the gravity field, in meters
        /// </summary>
        /// <remarks>This is not clamped like the Ingame one is.</remarks>
        new float Radius { get; set; }
    }
}
