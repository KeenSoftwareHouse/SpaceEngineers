using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using SpaceEngineers.Game.ModAPI;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ControlPanel))]
    public class MyControlPanel : MyTerminalBlock, IMyControlPanel
    {
    }
}