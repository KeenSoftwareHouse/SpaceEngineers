using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Cube
{
    public partial class MyTerminalBlock : Sandbox.ModAPI.IMyTerminalBlock
    {
        string ModAPI.Ingame.IMyTerminalBlock.CustomName
        {
            get { return CustomName.ToString(); }
        }

        string ModAPI.Ingame.IMyTerminalBlock.CustomNameWithFaction
        {
            get { return CustomNameWithFaction.ToString(); }
        }

        string ModAPI.Ingame.IMyTerminalBlock.DetailedInfo
        {
            get { return DetailedInfo.ToString(); }
        }

        string ModAPI.Ingame.IMyTerminalBlock.CustomInfo
        {
            get { return CustomInfo.ToString(); }
        }

        Action<MyTerminalBlock> GetDelegate(Action<ModAPI.IMyTerminalBlock> value)
        {
            return (Action<MyTerminalBlock>)Delegate.CreateDelegate(typeof(Action<MyTerminalBlock>), value.Target, value.Method);
        }

        Action<MyTerminalBlock, StringBuilder> GetDelegate(Action<ModAPI.IMyTerminalBlock, StringBuilder> value)
        {
            return (Action<MyTerminalBlock, StringBuilder>)Delegate.CreateDelegate(typeof(Action<MyTerminalBlock, StringBuilder>), value.Target, value.Method);
        }

        event Action<ModAPI.IMyTerminalBlock> ModAPI.IMyTerminalBlock.CustomNameChanged
        {
            add { CustomNameChanged += GetDelegate(value); }
            remove { CustomNameChanged -= GetDelegate(value); }
        }

        event Action<ModAPI.IMyTerminalBlock> ModAPI.IMyTerminalBlock.OwnershipChanged
        {
            add { OwnershipChanged += GetDelegate(value); }
            remove { OwnershipChanged -= GetDelegate(value); }
        }

        event Action<ModAPI.IMyTerminalBlock> ModAPI.IMyTerminalBlock.PropertiesChanged
        {
            add { PropertiesChanged += GetDelegate(value); }
            remove { PropertiesChanged -= GetDelegate(value); }
        }

        event Action<ModAPI.IMyTerminalBlock> ModAPI.IMyTerminalBlock.ShowOnHUDChanged
        {
            add { ShowOnHUDChanged += GetDelegate(value); }
            remove { ShowOnHUDChanged -= GetDelegate(value); }
        }

        event Action<ModAPI.IMyTerminalBlock> ModAPI.IMyTerminalBlock.VisibilityChanged
        {
            add { VisibilityChanged += GetDelegate(value); }
            remove { VisibilityChanged -= GetDelegate(value); }
        }

        event Action<ModAPI.IMyTerminalBlock, StringBuilder> ModAPI.IMyTerminalBlock.AppendingCustomInfo
        {
            add { AppendingCustomInfo += GetDelegate(value); }
            remove { AppendingCustomInfo -= GetDelegate(value); }
        }

        void ModAPI.Ingame.IMyTerminalBlock.GetActions(List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect)
        {
            (MyTerminalControlFactoryHelper.Static as IMyTerminalActionsHelper).GetActions(this.GetType(), resultList, collect);
        }
        void ModAPI.Ingame.IMyTerminalBlock.SearchActionsOfName(string name, List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect = null)
        {
            (MyTerminalControlFactoryHelper.Static as IMyTerminalActionsHelper).SearchActionsOfName(name, this.GetType(), resultList, collect);
        }

        Sandbox.ModAPI.Interfaces.ITerminalAction ModAPI.Ingame.IMyTerminalBlock.GetActionWithName(string name)
        {
            return (MyTerminalControlFactoryHelper.Static as IMyTerminalActionsHelper).GetActionWithName(name, this.GetType());
        }

        public Sandbox.ModAPI.Interfaces.ITerminalProperty GetProperty(string id)
        {
            return (MyTerminalControlFactoryHelper.Static as IMyTerminalActionsHelper).GetProperty(id, this.GetType());
        }

        public void GetProperties(List<Sandbox.ModAPI.Interfaces.ITerminalProperty> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalProperty, bool> collect = null)
        {
            (MyTerminalControlFactoryHelper.Static as IMyTerminalActionsHelper).GetProperties(this.GetType(), resultList, collect);
        }
    }
}
