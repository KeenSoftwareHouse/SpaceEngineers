using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMySensorBlock : IMyFunctionalBlock
    {
        /// <summary>
        /// Param - active
        /// </summary>
        float MaxRange { get; }
        float LeftExtend { get; }
        float RightExtend { get; }
        float TopExtend { get; }
        float BottomExtend { get; }
        float FrontExtend { get; }
        float BackExtend { get; }

        bool PlayProximitySound { get; }
        bool DetectPlayers { get; }
        bool DetectFloatingObjects { get; }
        bool DetectSmallShips { get; }
        bool DetectLargeShips { get; }
        bool DetectStations { get; }
        bool DetectSubgrids { get; }
        bool DetectAsteroids { get; }
        
        bool DetectOwner { get; }
        bool DetectFriendly { get; }
        bool DetectNeutral { get; }
        bool DetectEnemy { get; }

        bool IsActive { get; }

        MyDetectedEntityInfo LastDetectedEntity { get; }
        void DetectedEntities(List<MyDetectedEntityInfo> entities);
    }
}
