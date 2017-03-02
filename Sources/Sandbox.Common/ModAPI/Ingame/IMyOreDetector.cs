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
        ///Returns your own List filled with visible ore markers.
        /// </summary>        
        void GetOreMarkers (List <MyOreMarker> outputList);        
    }

    public struct MyOreMarker
    {
        public readonly string ElementName;
        public readonly Vector3D Location;

        public MyOreMarker (string inputElement, Vector3D inputCoords)
        {
            this.ElementName = inputElement;
            this.Location = inputCoords;
        }
    }
}
