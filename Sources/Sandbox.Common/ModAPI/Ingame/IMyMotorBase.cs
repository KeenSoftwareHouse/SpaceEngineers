using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyMotorBase : IMyFunctionalBlock
    {
        /// <summary>
        /// Gets if the piston top is attached to something
        /// </summary>
        bool IsAttached { get; }

        /// <summary>
        /// Gets if the motor stator is looking for a rotor
        /// </summary>
        bool PendingAttachment { get; }

        /// <summary>
        /// Attempts to attach to a nearby rotor/wheel
        /// </summary>
        void Attach();

        /// <summary>
        /// Detaches the rotor/wheel from the stator/suspension
        /// </summary>
        void Detach();
    }
}
