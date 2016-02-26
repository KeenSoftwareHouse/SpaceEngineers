using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI.Ingame;

namespace Sandbox.Game.Entities.Character
{
    partial class MyCharacter
    {
        IMyEntity ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return Entity; }
        }

        void ModAPI.Interfaces.IMyControllableEntity.DrawHud(ModAPI.Interfaces.IMyCameraController camera, long playerId)
        {
            if (camera is IMyCameraController)
                DrawHud(camera as IMyCameraController, playerId);
        }

        int IMyInventoryOwner.InventoryCount
        {
            get { return InventoryCount; }
        }

        long IMyInventoryOwner.EntityId
        {
            get { return EntityId; }
        }

        bool IMyInventoryOwner.HasInventory
        {
            get { return HasInventory; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {            
            return MyEntityExtensions.GetInventory(this, index);
        }
    }
}
