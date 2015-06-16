using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;

namespace Sandbox.Game.Screens.Terminal.Controls
{
    public class MyTerminalControlLabel<TBlock> : MyTerminalValueControl<TBlock, bool>       
        where TBlock : MyTerminalBlock
    {
        public readonly MyStringId Label;
        MyGuiControlLabel m_label;
        public MyTerminalControlLabel(string id, MyStringId label)
            : base(id)
        {
            Label = label;
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_label = new MyGuiControlLabel();
            return new MyGuiControlBlockProperty(MyTexts.GetString(Label), null, m_label, MyGuiControlBlockPropertyLayoutEnum.Horizontal);
        }



        public override bool GetValue(TBlock block)
        {
            return true;
        }

        public override void SetValue(TBlock block, bool value)
        {
        }

        public override bool GetDefaultValue(TBlock block)
        {
            return false;
        }

        public override bool GetMininum(TBlock block)
        {
            return false;
        }

        public override bool GetMaximum(TBlock block)
        {
            return true;
        }
    }
}
