using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ControlPanel))]
    class MyControlPanel : MyTerminalBlock, IMyControlPanel
    {
    }
}