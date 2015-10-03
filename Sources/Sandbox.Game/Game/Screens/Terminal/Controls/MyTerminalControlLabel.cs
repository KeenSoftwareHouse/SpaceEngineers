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
    public class MyTerminalControlLabel<TBlock> : MyTerminalValueControl<TBlock, string>       
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

        public override string GetValue(TBlock block)
        {
            return MyTexts.GetString(Label);
        }

        // label is readonly
        public override void SetValue(TBlock block, string value)
        {
        }

        public override string GetDefaultValue(TBlock block)
        {
            return "";
        }

        public override string GetMininum(TBlock block)
        {
            return "";
        }

        public override string GetMaximum(TBlock block)
        {
            return "";
        }
    }
}
