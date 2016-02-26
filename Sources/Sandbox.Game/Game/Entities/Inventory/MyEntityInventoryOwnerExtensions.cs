using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.ModAPI;


namespace Sandbox.Game.Entities
{
    [Obsolete("IMyInventoryOwner interface and MyInventoryOwnerTypeEnum enum is obsolete. Use type checking and inventory methods on MyEntity.")]
    public enum MyInventoryOwnerTypeEnum
    {
        Character,
        Storage,
        Energy,
        System,
        Conveyor,
    }

    /// <summary>
    /// This class simplifies backward compatibility to IMyInventoryOwner in the code
    /// Instead checking Entity type, you need to check now if the Entity has Inventory
    /// </summary>
    public static class MyEntityInventoryOwnerExtensions
    {
        [Obsolete("IMyInventoryOwner interface and MyInventoryOwnerTypeEnum enum is obsolete. Use type checking and inventory methods on MyEntity or MyInventory. Inventories will have this attribute as member.")]    
        public static MyInventoryOwnerTypeEnum InventoryOwnerType(this MyEntity entity)
        {
            // TODO: This should be handled differently, probably as attribute on MyInventory..
            if (entity.GetType() == typeof(MySmallMissileLauncher))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (entity.GetType() == typeof(MyProductionBlock))
            {
                 return MyInventoryOwnerTypeEnum.System;
            }
            else if (entity.GetType() == typeof(MySmallGatlingGun))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (entity.GetType() == typeof(MyConveyorSorter))
            {
                return MyInventoryOwnerTypeEnum.Storage;
            }
            else if (entity.GetType() == typeof(MyGasGenerator))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (entity.GetType() == typeof(MyShipToolBase))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (entity.GetType() == typeof(MyGasTank))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (entity.GetType() == typeof(MyReactor))
            {
                return MyInventoryOwnerTypeEnum.Energy; 
            }
            else if (entity.GetType() == typeof(MyCollector))
            {
                return MyInventoryOwnerTypeEnum.Storage;
            }
            else if (entity.GetType() == typeof(MyCargoContainer))
            {
                return MyInventoryOwnerTypeEnum.Storage;
            }
            else if (entity.GetType() == typeof(MyShipDrill))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (entity.GetType() == typeof(MyCharacter))
            {
                return MyInventoryOwnerTypeEnum.Character;
            }

            return MyInventoryOwnerTypeEnum.Storage;
        }                
    }
}
