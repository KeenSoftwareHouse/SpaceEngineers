using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyBlockGroup
    {
        List<IMyTerminalBlock> Blocks { get; }
        String Name { get;}
    }
}
