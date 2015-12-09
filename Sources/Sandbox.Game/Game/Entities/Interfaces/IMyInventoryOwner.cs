using System.Text;
using System;

namespace Sandbox.Game
{
    public enum MyInventoryOwnerTypeEnum
    {
        Character,
        Storage,
        Energy,
        System,
        Conveyor,
    }

    public interface IMyInventoryOwner : Sandbox.ModAPI.Interfaces.IMyInventoryOwner
    {
        new int InventoryCount { get; }
        String DisplayNameText { get; }
        new MyInventory GetInventory(int index);
        MyInventoryOwnerTypeEnum InventoryOwnerType { get; }
        new long EntityId { get; }
        new bool UseConveyorSystem { get; set; }
        void SetInventory(MyInventory inventory, int index);
    }
}
