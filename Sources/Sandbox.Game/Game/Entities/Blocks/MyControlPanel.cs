using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ControlPanel))]
    class MyControlPanel : MyTerminalBlock, IMyControlPanel
    {
    }
}