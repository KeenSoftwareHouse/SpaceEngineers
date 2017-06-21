using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Game.World;
using VRage.Utils;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlSeparator<TBlock> : MyTerminalControl<TBlock>, IMyTerminalControlSeparator
        where TBlock : MyTerminalBlock
    {
        public MyTerminalControlSeparator()
            : base("Separator")
        {
        }

        protected override MyGuiControlBase CreateGui()
        {
            var control = new MyGuiControlSeparatorList();
            control.Size = new Vector2(1, 0.01f);
            control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            control.AddHorizontal(Vector2.Zero, 1);
            return control;
        }

        string IMyTerminalControl.Id
        {
            get
            {
                return "";
            }
        }
    }
}
