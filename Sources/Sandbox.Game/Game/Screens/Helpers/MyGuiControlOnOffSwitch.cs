using Sandbox.Common;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Diagnostics;
using VRage;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlOnOffSwitch))]
    public class MyGuiControlOnOffSwitch : MyGuiControlBase
    {
        private MyGuiControlCheckbox m_onButton;
        private MyGuiControlLabel m_onLabel;

        private MyGuiControlCheckbox m_offButton;
        private MyGuiControlLabel m_offLabel;
        /// <summary>
        /// On/Off value of this switch. true = On; false = Off
        /// </summary>
        public bool Value
        {
            get { return m_value; }
            set
            {
                if (m_value != value)
                {
                    m_value = value;
                    UpdateButtonState();
                    if (ValueChanged != null)
                        ValueChanged(this);
                }
            }
        }
        private bool m_value;

        public event Action<MyGuiControlOnOffSwitch> ValueChanged;

        // mk:TODO Modify MyGuiControlList to allow different alignments and remove scale from this control.
        public MyGuiControlOnOffSwitch(
            bool initialValue = false,
            String onText = null,
            String offText = null)
        : base(canHaveFocus: true)
        {
            m_onButton = new MyGuiControlCheckbox(
                visualStyle: MyGuiControlCheckboxStyleEnum.SwitchOnOffLeft,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);

            m_offButton = new MyGuiControlCheckbox(
                visualStyle: MyGuiControlCheckboxStyleEnum.SwitchOnOffRight,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

            m_onLabel = new MyGuiControlLabel(
                position: new Vector2(m_onButton.Size.X * -0.5f, 0.0f),
                text: onText ?? MyTexts.GetString(MySpaceTexts.SwitchText_On),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            m_offLabel = new MyGuiControlLabel(
                position: new Vector2(m_onButton.Size.X * 0.5f, 0.0f),
                text: offText ?? MyTexts.GetString(MySpaceTexts.SwitchText_Off),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            // Set the size to contain the 2 buttons and space between them.
            // Buttons should not overlap, otherwise this will give us wrong results.
            Size = new Vector2(m_onButton.Size.X + m_offButton.Size.X, Math.Max(m_onButton.Size.Y, m_offButton.Size.Y));

            Elements.Add(m_onButton);
            Elements.Add(m_offButton);
            Elements.Add(m_onLabel);
            Elements.Add(m_offLabel);

            m_value = initialValue;
            UpdateButtonState();
        }

        public override void Init(MyObjectBuilder_GuiControlBase builder)
        {
            base.Init(builder);

            Size = new Vector2(m_onButton.Size.X + m_offButton.Size.X,
                               Math.Max(m_onButton.Size.Y, m_offButton.Size.Y));
            Debug.Assert(m_onButton != null);
            Debug.Assert(m_offButton != null);
            Debug.Assert(Elements.Contains(m_onButton));
            Debug.Assert(Elements.Contains(m_offButton));
            Debug.Assert(m_onButton.Owner == this);
            Debug.Assert(m_offButton.Owner == this);
            UpdateButtonState();
        }

        public override MyGuiControlBase HandleInput()
        {
            var captureInput = base.HandleInput();

            if (captureInput != null)
                return captureInput;

            bool isControl = MyInput.Static.IsNewLeftMouseReleased() || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.ACCEPT, MyControlStateType.NEW_RELEASED);
            if (Enabled && IsMouseOver && isControl ||
                HasFocus && MyInput.Static.IsNewKeyPressed(MyKeys.Enter))
            {
                Value = !Value;
                captureInput = this;
                MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
            }

            return captureInput;
        }

        private void UpdateButtonState()
        {
            m_onButton.IsChecked = Value;
            m_offButton.IsChecked = !Value;

            m_onLabel.Font  = (Value) ? MyFontEnum.White : MyFontEnum.Blue;
            m_offLabel.Font = (Value) ? MyFontEnum.Blue : MyFontEnum.White;
        }

        protected override void OnVisibleChanged()
        {
            if (m_onButton != null) m_onButton.Visible = Visible;
            if (m_offButton != null) m_offButton.Visible = Visible;
            base.OnVisibleChanged();
        }
    }
}
