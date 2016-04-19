using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Collections;

namespace Sandbox.ModAPI.Interfaces
{
    public interface ITerminalAction
    {
        string Id { get; }
        string Icon { get; }
        StringBuilder Name { get; }
        void Apply(VRage.Game.ModAPI.Ingame.IMyCubeBlock block);
        void Apply(VRage.Game.ModAPI.Ingame.IMyCubeBlock block, ListReader<TerminalActionParameter> terminalActionParameters);
        void WriteValue(VRage.Game.ModAPI.Ingame.IMyCubeBlock block, StringBuilder appendTo);
        bool IsEnabled(VRage.Game.ModAPI.Ingame.IMyCubeBlock block);
    }
}
