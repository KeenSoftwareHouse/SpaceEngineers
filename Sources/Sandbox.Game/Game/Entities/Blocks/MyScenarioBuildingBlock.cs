using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Blocks
{
    //dummy block to add scenario edit controls to the bottom of the terminal list. Static constructor must run last.
    public class MyScenarioBuildingBlock : MyTerminalBlock
    {
        public static List<MyTerminalBlock> Clipboard = new List<MyTerminalBlock>();
        private static TimeSpan m_lastAccess;
        private static void AddToClipboard(MyTerminalBlock block)
        {
            if (MySession.Static.ElapsedGameTime != m_lastAccess)
            {
                Clipboard.Clear();
                m_lastAccess = MySession.Static.ElapsedGameTime;
            }
            Clipboard.Add(block);
        }
        
        public MyScenarioBuildingBlock()
        {
            CreateTerminalControls();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyTerminalBlock>())
                return;
            base.CreateTerminalControls();
            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyTerminalBlock>());
            var idleButton = new MyTerminalControlButton<MyTerminalBlock>("CopyBlockID", MySpaceTexts.GuiScenarioEdit_CopyIds, MySpaceTexts.GuiScenarioEdit_CopyIdsTooltip,
                delegate(MyTerminalBlock self)
                {
                    AddToClipboard(self);
                });
            idleButton.Enabled = (x) => true;
            idleButton.Visible = (x) => MySession.Static.Settings.ScenarioEditMode;
            idleButton.SupportsMultipleBlocks = true;
            MyTerminalControlFactory.AddControl(idleButton);
        }
    }
}
