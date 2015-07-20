using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyOreDetector : IMyFunctionalBlock
    {
        float Range {get;}
        bool BroadcastUsingAntennas {get;}

        /// <summary>
        /// Gets the world position and name of all the ores detected by this Ore Detector.
        /// </summary>
        /// <returns>A Dictionary with all the world positions and names of detected ores.</returns>
        Dictionary<VRageMath.Vector3D, string> GetOreLocations();
    }
}
