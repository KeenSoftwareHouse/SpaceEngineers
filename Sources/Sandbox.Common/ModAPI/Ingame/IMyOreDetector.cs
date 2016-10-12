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
        ///Returns your own List filled with known ore markers.
        ///Each marker has an element name and a location.
        /// </summary>        
        void GetOreMarkers (ref List <MyOreMarker> outputList);        
    }

    public struct MyOreMarker
    {
        public readonly string ElementName;
        public readonly Vector3D Location;

        public MyOreMarker (string inputName, Vector3D inputCoords)
        {
            this.ElementName = inputName;
            this.Location = inputCoords;
        }
    }
}
