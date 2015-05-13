using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Character
{
    partial class MyCharacter
    {
        ModAPI.IMyEntity ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return Entity; }
        }

        void ModAPI.Interfaces.IMyControllableEntity.DrawHud(ModAPI.Interfaces.IMyCameraController camera, long playerId)
        {
            if (camera is IMyCameraController)
                DrawHud(camera as IMyCameraController, playerId);
        }
    }
}
