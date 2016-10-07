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
            if (IsSameOrSubclass(typeof(MyUserControllableGun), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (IsSameOrSubclass(typeof(MyProductionBlock), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (IsSameOrSubclass(typeof(MyConveyorSorter), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.Storage;
            }
            else if (IsSameOrSubclass(typeof(MyGasGenerator), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (IsSameOrSubclass(typeof(MyShipToolBase), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (IsSameOrSubclass(typeof(MyGasTank), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (IsSameOrSubclass(typeof(MyReactor), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.Energy;
            }
            else if (IsSameOrSubclass(typeof(MyCollector), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.Storage;
            }
            else if (IsSameOrSubclass(typeof(MyCargoContainer), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.Storage;
            }
            else if (IsSameOrSubclass(typeof(MyShipDrill), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.System;
            }
            else if (IsSameOrSubclass(typeof(MyCharacter), entity.GetType()))
            {
                return MyInventoryOwnerTypeEnum.Character;
            }

            return MyInventoryOwnerTypeEnum.Storage;
        }

        private static bool IsSameOrSubclass(Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase)
                   || potentialDescendant == potentialBase;
        }
    }
}
