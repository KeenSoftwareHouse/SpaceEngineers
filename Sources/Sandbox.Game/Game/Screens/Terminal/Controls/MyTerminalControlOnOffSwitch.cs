
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

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlOnOffSwitch<TBlock> : MyTerminalValueControl<TBlock, bool>
        where TBlock : MyTerminalBlock
    {
        public delegate bool GetterDelegate(TBlock block);
        public delegate void SetterDelegate(TBlock block, bool value);

        MyGuiControlOnOffSwitch m_onOffSwitch;

        public readonly MyStringId Title;
        public readonly MyStringId OnText;
        public readonly MyStringId OffText;
        public readonly MyStringId Tooltip;

        public GetterDelegate Getter;
        public SetterDelegate Setter;

        public Expression<Func<TBlock, bool>> MemberExpression
        {
            set
            {
                Getter = new GetterDelegate(value.CreateGetter());
                Setter = new SetterDelegate(value.CreateSetter());
            }
        }

        private Action<MyGuiControlOnOffSwitch> m_valueChanged;

        public MyTerminalControlOnOffSwitch(string id, MyStringId title, MyStringId tooltip = default(MyStringId), MyStringId? on = null, MyStringId? off = null)
            : base(id)
        {
            Title = title;
            OnText = on ?? MySpaceTexts.SwitchText_On;
            OffText = off ?? MySpaceTexts.SwitchText_Off;
            Tooltip = tooltip;
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
                    Setter(item, value);
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
                m_onOffSwitch.Value = Getter(first);
                m_onOffSwitch.ValueChanged += m_valueChanged;
            }
        }

        void SwitchAction(TBlock block)
        {
            Setter(block, !Getter(block));
        }

        void OnAction(TBlock block)
        {
            Setter(block, true);
        }

        void OffAction(TBlock block)
        {
            Setter(block, false);
        }

        void Writer(TBlock block, StringBuilder result, StringBuilder onText, StringBuilder offText)
        {
            result.AppendStringBuilder(Getter(block) ? onText : offText);
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
