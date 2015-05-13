using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities
{
    partial class MyControllableSphere
    {
        //ModAPI.IMyControllerInfo ModAPI.Interfaces.IControllableEntity.ControllerInfo
        //{
        //    get { return ControllerInfo; }
        //}

        ModAPI.IMyEntity ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return Entity; }
        }

        void ModAPI.Interfaces.IMyControllableEntity.DrawHud(Sandbox.ModAPI.Interfaces.IMyCameraController entity, long player)
        {
            if(entity is IMyCameraController)
                DrawHud(entity as IMyCameraController, player);
        }
    }
}
