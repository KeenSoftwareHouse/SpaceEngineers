using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;

namespace Sandbox.ModAPI
{
    public interface IMyPistonBase : IMyFunctionalBlock, Sandbox.ModAPI.Ingame.IMyPistonBase
    {
        event Action<bool> LimitReached;

        /// <summary>
        /// Gets the grid attached to the piston top part
        /// </summary>
        IMyCubeGrid TopGrid { get; }

        /// <summary>
        /// Gets the attached piston top part entity
        /// </summary>
        IMyCubeBlock Top { get; }

        /// <summary>
        /// Notifies when the top grid is attached or detached
        /// </summary>
        event Action<ModAPI.IMyPistonBase> AttachedEntityChanged;

        /// <summary>
        /// Attaches a specified nearby top part to the piston block
        /// </summary>
        /// <param name="top">Entity to attach</param>
        /// <remarks>The top to attach must already be in position before calling this method.</remarks>
        void Attach(IMyPistonTop top);
    }
}
