using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyLaserAntenna : IMyFunctionalBlock
    {
        Vector3D TargetCoords
        {
            get;
        }

        void SetTargetCoords(string coords);

        void Connect();

        /// <summary>
        /// Sets the argument for a specified programmable block connected to the current grid via laser antenna.
        /// </summary>
        /// <param name="pb_name">The name of the target programmable block for this message.</param>
        /// <param name="message">The new argument string.</param>
        /// <param name="run">Should the programmable block be run after the argument was changed.</param>
        /// <returns>Returns true if the pb could be reached. False otherwise.</returns>
        bool SendMessage(string pb_name, string message, bool run = false);

        /// <summary>
        /// Runs the script of a specified programmable block connected to the current grid via laser antenna.
        /// </summary>
        /// <param name="pb_name">The name of the target programmable block for this run command.</param>
        /// <returns>Returns true if the pb could be reached. False otherwise.</returns>
        bool SendRunCommand(string pb_name);

    }
}
