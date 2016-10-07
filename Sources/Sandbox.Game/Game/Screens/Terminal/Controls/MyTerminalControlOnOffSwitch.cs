
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using VRage;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.Screens.Helpers;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage.Library.Utils;
using VRage.Library.Collections;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlOnOffSwitch<TBlock> : MyTerminalValueControl<TBlock, bool>, IMyTerminalControlOnOffSwitch
        where TBlock : MyTerminalBlock
    {
        MyGuiControlOnOffSwitch m_onOffSwitch;

        public MyStringId Title;
        public MyStringId OnText;
        public MyStringId OffText;
        public MyStringId Tooltip;
        
        private Action<MyGuiControlOnOffSwitch> m_valueChanged;

        public MyTerminalControlOnOffSwitch(string id, MyStringId title, MyStringId tooltip = default(MyStringId), MyStringId? on = null, MyStringId? off = null)
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
            m_onOffSwitch = new MyGuiControlOnOffSwitch(false, MyTexts.GetString(OnText), MyTexts.GetString(OffText));
            m_onOffSwitch.Size = new Vector2(PREFERRED_CONTROL_WIDTH, m_onOffSwitch.Size.Y);
            m_valueChanged = OnValueChanged;
            m_onOffSwitch.ValueChanged += m_valueChanged;

            var propertyControl = new MyGuiControlBlockProperty(MyTexts.GetString(Title), MyTexts.GetString(Tooltip), m_onOffSwitch, showExtraInfo: false);
            propertyControl.Size = new Vector2(PREFERRED_CONTROL_WIDTH, propertyControl.Size.Y);
            return propertyControl;
        }

        void OnValueChanged(MyGuiControlOnOffSwitch obj)
        {
            bool value = obj.Value;
            foreach (var item in TargetBlocks)
            {
                if (item.HasLocalPlayerAccess())
                {
                    SetValue(item, value);
                }
            }
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();
            var first = FirstBlock;
            if (first != null)
            {
                m_onOffSwitch.ValueChanged -= m_valueChanged;
                m_onOffSwitch.Value = GetValue(first);
                m_onOffSwitch.ValueChanged += m_valueChanged;
            }
        }

        void SwitchAction(TBlock block)
        {
            SetValue(block, !GetValue(block));
        }

        void OnAction(TBlock block)
        {
            SetValue(block, true);
        }

        void OffAction(TBlock block)
        {
            SetValue(block, false);
        }

        void Writer(TBlock block, StringBuilder result, StringBuilder onText, StringBuilder offText)
        {
            result.AppendStringBuilder(GetValue(block) ? onText : offText);
        }

        void AppendAction(MyTerminalAction<TBlock> action)
        {
            var arr = Actions ?? new MyTerminalAction<TBlock>[0];
            Array.Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = action;
            Actions = arr;
        }

        public MyTerminalAction<TBlock> EnableToggleAction(string icon, StringBuilder name, StringBuilder onText, StringBuilder offText)
        {
            var action = new MyTerminalAction<TBlock>(Id, name, SwitchAction, (x, r) => Writer(x, r, onText, offText), icon);
            AppendAction(action);

            return action;
        }

        public MyTerminalAction<TBlock> EnableOnAction(string icon, StringBuilder name, StringBuilder onText, StringBuilder offText)
        {
            var action = new MyTerminalAction<TBlock>(Id + "_On", name, OnAction, (x, r) => Writer(x, r, onText, offText), icon);
            AppendAction(action);

            return action;
        }

        public MyTerminalAction<TBlock> EnableOffAction(string icon, StringBuilder name, StringBuilder onText, StringBuilder offText)
        {
            var action = new MyTerminalAction<TBlock>(Id + "_Off", name, OffAction, (x, r) => Writer(x, r, onText, offText), icon);
            AppendAction(action);

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
        ///  Implements IMyTerminalControlOnOffSwitch for Mods
        /// </summary>
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

        MyStringId IMyTerminalControlOnOffSwitch.OnText
        {
            get
            {
                return OnText;
            }

            set
            {
                OnText = value;
            }
        }

        MyStringId IMyTerminalControlOnOffSwitch.OffText
        {
            get
            {
                return OffText;
            }

            set
            {
                OffText = value;
            }
        }
    }
}
