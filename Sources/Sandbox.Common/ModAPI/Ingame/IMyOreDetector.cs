using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyOreDetector : IMyFunctionalBlock
    {
        float Range {get;}
        bool BroadcastUsingAntennas {get;}

        /// <summary>
        ///Returns a list of ore deposits.
        ///Each ore deposit has a substance name and a location.
        /// </summary>        
        List <OreDeposit> GetOreMarkers();        
    }

    public struct OreDeposit
    {
        public readonly string substanceName;
        public readonly Vector3D coordinates;

        public OreDeposit (string inputName, Vector3D inputCoords)
        {
            this.substanceName = inputName;
            this.coordinates = inputCoords;
        }
    }
}
