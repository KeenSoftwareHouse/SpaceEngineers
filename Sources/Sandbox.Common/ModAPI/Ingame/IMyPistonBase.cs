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

        /// <summary>
        /// Gets the current position of the piston head relative to the base.
        /// </summary>
        float CurrentPosition { get; }

        /// <summary>
        /// Gets the current status.
        /// </summary>
        PistonStatus Status { get; }

        /// <summary>
        /// Gets if the piston base is attached to the top piece
        /// </summary>
        bool IsAttached { get; }

        /// <summary>
        /// Gets if the piston is safety locked (welded)
        /// </summary>
        bool IsLocked { get; }

        /// <summary>
        /// Gets if the piston is looking for a top part
        /// </summary>
        bool PendingAttachment { get; }

        /// <summary>
        /// Attaches a nearby top part to the piston block
        /// </summary>
        void Attach();

        /// <summary>
        /// Detaches the top from the piston
        /// </summary>
        void Detach();
    }
}
