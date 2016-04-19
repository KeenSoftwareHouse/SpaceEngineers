using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    public partial class MyShipController
    {
        IMyEntity VRage.Game.ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return Entity; }
        }

        void VRage.Game.ModAPI.Interfaces.IMyControllableEntity.DrawHud(IMyCameraController camera, long playerId)
        {
            if (camera is IMyCameraController)
                DrawHud(camera as IMyCameraController, playerId);
        }
    }
}
