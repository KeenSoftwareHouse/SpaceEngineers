using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.World;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage;
using VRage.Library.Utils;

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlCheckbox<TBlock> : MyTerminalValueControl<TBlock, bool>
        where TBlock : MyTerminalBlock
    {
        Action<TBlock> m_action;

        public Func<TBlock, bool> Getter;
        public Action<TBlock, bool> Setter;

        private MyGuiControlCheckbox m_checkbox;
        private Action<MyGuiControlCheckbox> m_checkboxClicked;

        public readonly MyStringId Title;
        public readonly MyStringId OnText;
        public readonly MyStringId OffText;
        public readonly MyStringId Tooltip;

        public MyTerminalControlCheckbox(string id, MyStringId title, MyStringId tooltip, MyStringId? on = null, MyStringId? off = null)
            : base(id)
        {
            Title = title;
            OnText = on ?? MySpaceTexts.SwitchText_On;
            OffText = off ?? MySpaceTexts.SwitchText_Off;
            Tooltip = tooltip;
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_checkbox = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(Tooltip));
            m_checkboxClicked = OnCheckboxClicked;
            m_checkbox.IsCheckedChanged = m_checkboxClicked;
            return new MyGuiControlBlockProperty(MyTexts.GetString(Title), MyTexts.GetString(Tooltip), m_checkbox, MyGuiControlBlockPropertyLayoutEnum.Horizontal);
        }

        void OnCheckboxClicked(MyGuiControlCheckbox obj)
        {
            foreach (var item in TargetBlocks)
            {
                Setter(item, obj.IsChecked);
            }
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();

            var first = FirstBlock;
            if (first != null)
                m_checkbox.IsCheckedChanged = null;
            m_checkbox.IsChecked = Getter(first);
            m_checkbox.IsCheckedChanged = m_checkboxClicked;
        }

        void SwitchAction(TBlock block)
        {
            Setter(block, !Getter(block));
        }

        void CheckAction(TBlock block)
        {
            Setter(block, true);
        }

        void UncheckAction(TBlock block)
        {
            Setter(block, false);
        }

        void Writer(TBlock block, StringBuilder result, StringBuilder onText, StringBuilder offText)
        {
            result.Append(Getter(block) ? onText : offText);
        }

        public MyTerminalAction<TBlock> EnableAction(string icon, StringBuilder name, StringBuilder onText, StringBuilder offText)
        {
            var action = new MyTerminalAction<TBlock>(Id, name, SwitchAction, (x, r) => Writer(x, r, onText, offText), icon);
            Actions = new MyTerminalAction<TBlock>[] { action };

            return action;
        }

        public override bool GetValue(TBlock block)
        {
            return Getter(block);
        }

        public override void SetValue(TBlock block, bool value)
        {
            Setter(block, value);
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
