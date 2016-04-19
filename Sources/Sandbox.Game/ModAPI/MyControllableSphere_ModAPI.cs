using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    partial class MyControllableSphere
    {
        //ModAPI.IMyControllerInfo ModAPI.Interfaces.IControllableEntity.ControllerInfo
        //{
        //    get { return ControllerInfo; }
        //}

        IMyEntity VRage.Game.ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return Entity; }
        }

        void VRage.Game.ModAPI.Interfaces.IMyControllableEntity.DrawHud(IMyCameraController entity, long player)
        {
            if(entity is IMyCameraController)
                DrawHud(entity as IMyCameraController, player);
        }
    }
}
