
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyTerminalActionsHelper
    {
        void GetActions(Type blockType, List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect = null);
        void SearchActionsOfName(string name, Type blockType, List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect = null);
        Sandbox.ModAPI.Interfaces.ITerminalAction GetActionWithName(string nameType, Type blockType);
        ITerminalProperty GetProperty(string id, Type blockType);
        void GetProperties(Type blockType, List<ITerminalProperty> resultList, Func<ITerminalProperty, bool> collect = null);
        Sandbox.ModAPI.IMyGridTerminalSystem GetTerminalSystemForGrid(VRage.Game.ModAPI.IMyCubeGrid grid);
    }
}
