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
        ///This information is only available when owning player is in the world.
        /// </summary>        
        void GetOreMarkers (ref List <MyOreMarker> outputList);        
    }

    public struct MyOreMarker
    {
        public readonly string ElementName;
        public readonly string TypeID;
        public readonly bool IsRare;
        public readonly Vector3D Location;

        public MyOreMarker (string inputName, string inputID, bool inputRarity, Vector3D inputCoords)
        {
            this.ElementName = inputName;
            this.TypeID = inputID;
            this.IsRare = inputRarity;
            this.Location = inputCoords;
        }
    }
}
