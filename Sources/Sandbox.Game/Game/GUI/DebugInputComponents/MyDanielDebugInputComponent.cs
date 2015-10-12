using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Input;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyDanielDebugInputComponent : MyDebugComponent
    {

        public MyDanielDebugInputComponent()
        {
            AddShortcut(MyKeys.S, true, true, false, false, () => "Toggle sector work cycle", () => ToggleSectors());
        }

        private bool ToggleSectors()
        {
            return true;
        }

        public override void Draw()
        {
            base.Draw();

        }

        public override string GetName()
        {
            return "Daniel";
        }
    }
}
