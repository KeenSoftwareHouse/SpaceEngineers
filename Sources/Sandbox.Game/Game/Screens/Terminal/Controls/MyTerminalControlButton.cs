
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlButton<TBlock> : MyTerminalControl<TBlock>
        where TBlock : MyTerminalBlock
    {
        Action<TBlock> m_action;
        Action<MyGuiControlButton> m_buttonClicked;

        public readonly MyStringId Title;
        public readonly MyStringId Tooltip;

        public MyTerminalControlButton(string id, MyStringId title, MyStringId tooltip, Action<TBlock> action)
            : base(id)
        {
            Title = title;
            Tooltip = tooltip;
            m_action = action;
        }

        protected override MyGuiControlBase CreateGui()
        {
            var button = new MyGuiControlButton(text: MyTexts.Get(Title), toolTip: MyTexts.GetString(Tooltip));
            m_buttonClicked = OnButtonClicked;
            button.ButtonClicked += m_buttonClicked;
            return button;
        }

        void OnButtonClicked(MyGuiControlButton obj)
        {
            foreach (var item in TargetBlocks)
            {
                m_action(item);
            }
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();
        }

        public MyTerminalAction<TBlock> EnableAction(string icon, StringBuilder name, WriterDelegate writer = null)
        {
            var action = new MyTerminalAction<TBlock>(Id, name, m_action, writer, icon);
            Actions = new MyTerminalAction<TBlock>[] { action };

            return action;
        }
    }
}
