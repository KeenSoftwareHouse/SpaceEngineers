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
        /// Runs the script of a specified programmable block connected to the current grid via laser antenna with a custom argument.
        /// </summary>
        /// <param name="pb_name">The name of the target programmable block for this message.</param>
        /// <param name="message">The custom argument string.</param>
        /// <returns>Returns true if the pb could be reached. False otherwise.</returns>
        bool SendMessage(string pb_name, string message);
    }
}
