using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyTerminalBlock : IMyCubeBlock
    {
        string CustomName { get; }
        string CustomNameWithFaction { get; }
        string DetailedInfo { get; }
        bool HasLocalPlayerAccess();
        bool HasPlayerAccess(long playerId);
        void RequestShowOnHUD(bool enable);
        void SetCustomName(string text);
        void SetCustomName(StringBuilder text);
        bool ShowOnHUD { get; }
        event Action<IMyTerminalBlock> CustomNameChanged;
        event Action<IMyTerminalBlock> OwnershipChanged;
        event Action<IMyTerminalBlock> PropertiesChanged;
        event Action<IMyTerminalBlock> ShowOnHUDChanged;
        event Action<IMyTerminalBlock> VisibilityChanged;
        void GetActions(List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect = null);
    }
}
