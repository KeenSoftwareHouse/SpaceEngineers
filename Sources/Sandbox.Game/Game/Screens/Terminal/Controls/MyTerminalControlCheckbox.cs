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
using VRage.Library.Collections;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlCheckbox<TBlock> : MyTerminalValueControl<TBlock, bool>, IMyTerminalControlCheckbox
        where TBlock : MyTerminalBlock
    {
        Action<TBlock> m_action;

        private MyGuiControlCheckbox m_checkbox;
        private Action<MyGuiControlCheckbox> m_checkboxClicked;

        public MyStringId Title;
        public MyStringId OnText;
        public MyStringId OffText;
        public MyStringId Tooltip;

        public MyTerminalControlCheckbox(string id, MyStringId title, MyStringId tooltip, MyStringId? on = null, MyStringId? off = null)
            : base(id)
        {
            Title = title;
            OnText = on ?? MySpaceTexts.SwitchText_On;
            OffText = off ?? MySpaceTexts.SwitchText_Off;
            Tooltip = tooltip;
            Serializer = delegate(BitStream stream, ref bool value) { stream.Serialize(ref value); };
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
                SetValue(item, obj.IsChecked);
            }
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();

            var first = FirstBlock;
            if (first != null)
                m_checkbox.IsCheckedChanged = null;
            m_checkbox.IsChecked = GetValue(first);
            m_checkbox.IsCheckedChanged = m_checkboxClicked;
        }

        void SwitchAction(TBlock block)
        {
            SetValue(block, !GetValue(block));
        }

        void CheckAction(TBlock block)
        {
            SetValue(block, true);
        }

        void UncheckAction(TBlock block)
        {
            SetValue(block, false);
        }

        void Writer(TBlock block, StringBuilder result, StringBuilder onText, StringBuilder offText)
        {
            result.Append(GetValue(block) ? onText : offText);
        }

        public MyTerminalAction<TBlock> EnableAction(string icon, StringBuilder name, StringBuilder onText, StringBuilder offText)
        {
            var action = new MyTerminalAction<TBlock>(Id, name, SwitchAction, (x, r) => Writer(x, r, onText, offText), icon);
            Actions = new MyTerminalAction<TBlock>[] { action };

            return action;
        }

        public override bool GetDefaultValue(TBlock block)
        {
            return false;
        }

        public override bool GetMinimum(TBlock block)
        {
            return false;
        }

        public override bool GetMaximum(TBlock block)
        {
            return true;
        }

        public override void SetValue(TBlock block, bool value)
        {
            base.SetValue(block, value);
        }

        public override bool GetValue(TBlock block)
        {
            return base.GetValue(block);
        }

        /// <summary>
        /// Implement IMyTerminalControlCheckbox for mods
        /// </summary>
        MyStringId IMyTerminalControlCheckbox.OnText
        {
            get
            {
                return this.OnText;
            }

            set
            {
                this.OnText = value;
            }
        }

        MyStringId IMyTerminalControlCheckbox.OffText
        {
            get
            {
                return this.OffText;
            }

            set
            {
                OffText = value;
            }
        }

        MyStringId IMyTerminalControlTitleTooltip.Title
        {
            get
            {
                return Title;
            }

            set
            {
                Title = value;
            }
        }

        MyStringId IMyTerminalControlTitleTooltip.Tooltip
        {
            get
            {
                return Tooltip;
            }

            set
            {
                Tooltip = value;
            }
        }
    }
}
