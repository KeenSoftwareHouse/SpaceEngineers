using System;

namespace VRage.Game.ModAPI.Ingame
{
    [Obsolete("IMyInventoryOwner interface and MyInventoryOwnerTypeEnum enum is obsolete. Use type checking and inventory methods on MyEntity.")]
    public interface IMyInventoryOwner 
    {
        int InventoryCount { get; }
        IMyInventory GetInventory(int index);
        long EntityId { get; }
        bool UseConveyorSystem { get; set; }
        bool HasInventory { get; }
    }
}
