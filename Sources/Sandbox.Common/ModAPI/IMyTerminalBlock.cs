using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;

namespace Sandbox.ModAPI
{
    public interface IMyTerminalBlock : IMyCubeBlock, Sandbox.ModAPI.Ingame.IMyTerminalBlock
    {
        event Action<IMyTerminalBlock> CustomNameChanged;
        event Action<IMyTerminalBlock> OwnershipChanged;
        event Action<IMyTerminalBlock> PropertiesChanged;
        event Action<IMyTerminalBlock> ShowOnHUDChanged;
        event Action<IMyTerminalBlock> VisibilityChanged;

        /// <summary>
        /// Event to append custom info.
        /// </summary>
        event Action<IMyTerminalBlock, StringBuilder> AppendingCustomInfo;

        /// <summary>
        /// Raises AppendingCustomInfo so every subscriber can append custom info.
        /// </summary>
        void RefreshCustomInfo();
    }
}
