using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyTerminalBlock : IMyCubeBlock
    {
        string CustomName { get; }
        string CustomNameWithFaction { get; }
        string DetailedInfo { get; }
        string CustomInfo { get; }
        bool HasLocalPlayerAccess();
        bool HasPlayerAccess(long playerId);
        void SetCustomName(string text);
        void SetCustomName(StringBuilder text);
        bool ShowOnHUD { get; }
        void GetActions(List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect = null);
        void SearchActionsOfName(string name,List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect = null);
        Sandbox.ModAPI.Interfaces.ITerminalAction GetActionWithName(string name);
        Sandbox.ModAPI.Interfaces.ITerminalProperty GetProperty(string id);
        void GetProperties(List<Sandbox.ModAPI.Interfaces.ITerminalProperty> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalProperty, bool> collect = null);
    }

    /*
    Written by Kalvin Osborne, AKA Night Lone. Please do not remove this line.
    */
    public static class TerminalBlockExtentions
    {
        public static void ApplyAction(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string actionName)
        {
            block.GetActionWithName(actionName).Apply(block);
        }
        public static void ApplyAction(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string actionName, List<TerminalActionParameter> parameters)
        {
            block.GetActionWithName(actionName).Apply(block, parameters);
        }
        
        public static bool HasAction(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string Action)
        {
            return !(block.GetActionWithName(Action) == null);
        }
        public static bool HasInventory(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block)
        {
            return block is Sandbox.ModAPI.Interfaces.IMyInventoryOwner;
        }
        public static Sandbox.ModAPI.Interfaces.IMyInventory GetInventory(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, int index)
        {
            if (block.HasInventory())
            {
                return ((Sandbox.ModAPI.Interfaces.IMyInventoryOwner)block).GetInventory(index);
            }
            else
            {
                return null;
            }
        }
        public static int GetInventoryCount(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block)
        {
            if (block.HasInventory())
            {
                return ((Sandbox.ModAPI.Interfaces.IMyInventoryOwner)block).InventoryCount;
            }
            else
            {
                return 0;
            }
        }
        public static bool GetUseConveyorSystem(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block)
        {
            if (block.HasInventory())
            {
                return ((Sandbox.ModAPI.Interfaces.IMyInventoryOwner)block).UseConveyorSystem;
            }
            else
            {
                return false;
            }
        }
        public static void SetUseConveyorSystem(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, bool use)
        {
            if (block.HasInventory())
            {
                ((Sandbox.ModAPI.Interfaces.IMyInventoryOwner)block).UseConveyorSystem = use;
            }
        }
    }
}
