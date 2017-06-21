using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using IMyInventoryOwner = VRage.Game.ModAPI.Ingame.IMyInventoryOwner;
namespace Sandbox.ModAPI.Ingame
{
    public interface IMyTerminalBlock : IMyCubeBlock
    {
        string CustomName { get; }
        string CustomNameWithFaction { get; }
        string DetailedInfo { get; }
        string CustomInfo { get; }
        /// <summary>
        /// Gets or sets the Custom Data string.
        /// NOTE: Only use this for user input. For storing large mod configs, create your own MyModStorageComponent
        /// </summary>
        string CustomData { get; set; }
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
        public static long GetId(this IMyTerminalBlock block)
        {
            return block.EntityId;
        }

        public static void ApplyAction(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string actionName)
        {
            block.GetActionWithName(actionName).Apply(block);
        }

        public static void ApplyAction(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string actionName, List<TerminalActionParameter> parameters)
        {
            block.GetActionWithName(actionName).Apply(block, parameters);
        }
        
        public static bool HasAction(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, string actionName)
        {
            return block.GetActionWithName(actionName) != null;
        }

        public static bool HasInventory(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block)
        {
            var entity = block as MyEntity;
            if (entity == null)
                return false;
            if (!(block is IMyInventoryOwner))
                return false;

            return entity.HasInventory;
        }

        public static VRage.Game.ModAPI.Ingame.IMyInventory GetInventory(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, int index)
        {
            var entity = block as MyEntity;
            if (entity == null)
                return null;

            if (!entity.HasInventory)
                return null;

            return entity.GetInventoryBase(index) as IMyInventory;
        }

        public static int GetInventoryCount(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block)
        {
            var entity = block as MyEntity;
            if (entity == null)
                return 0;

            return entity.InventoryCount;
        }

        [Obsolete("Use the blocks themselves, this method is no longer reliable")]
        public static bool GetUseConveyorSystem(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block)
        {
            if (block is IMyInventoryOwner)
            {
                return ((IMyInventoryOwner)block).UseConveyorSystem;
            }
            else
            {
                return false;
            }
        }

        [Obsolete("Use the blocks themselves, this method is no longer reliable")]
        public static void SetUseConveyorSystem(this Sandbox.ModAPI.Ingame.IMyTerminalBlock block, bool use)
        {
            if (block is IMyInventoryOwner)
            {
                ((IMyInventoryOwner)block).UseConveyorSystem = use;
            }
        }
    }
}
