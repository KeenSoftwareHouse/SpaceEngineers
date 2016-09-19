using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineers.Game.ModAPI
{
    public interface IMyGravityGeneratorBase : IMyFunctionalBlock, Ingame.IMyGravityGeneratorBase, Sandbox.Game.Entities.IMyGravityProvider
    {
        /// <summary>
        /// Gets or sets the gravity acceleration in Gs.
        /// </summary>
        /// <remarks>This is not clamped like the Ingame one is.</remarks>
        new float Gravity { get; set; }

        /// <summary>
        /// Gets or sets the gravity acceleration in m/s^2.
        /// </summary>
        /// <remarks>This is not clamped like the Ingame one is.</remarks>
        new float GravityAcceleration { get; set; }
    }
}
