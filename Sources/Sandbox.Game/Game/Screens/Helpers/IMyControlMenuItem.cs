using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Input;
using VRage.Utils;

namespace Sandbox.Game.Screens.Helpers
{
    [Flags]
    public enum MySupportKeysEnum : byte
    {
        NONE           = 0,
        CTRL           = 1 << 0,
        ALT            = 1 << 1,
        SHIFT          = 1 << 2,
        CTRL_ALT       = CTRL | ALT,
        CTRL_SHIFT     = CTRL | SHIFT,
        ALT_SHIFT      = ALT | SHIFT,
        CTRL_ALT_SHIFT = CTRL_ALT | SHIFT,
    }

    public abstract class MyAbstractControlMenuItem
    {
        private static string CTRL = "ctrl";
        private static string SHIFT = "shift";
        private static string ALT = "alt";
        private static string PLUS = " + ";
        private static string COLON = ":";
        private static string FORMAT_LABEL = "{0}";
        private static string FORMAT_LABEL_HINT = "{0} ({1})";
        private static StringBuilder m_tmpBuilder = new StringBuilder();

        public abstract string Label { get; }
        public virtual string CurrentValue { get { return null; } }
        public MyStringId ControlCode { get; private set; }

        public string ControlName { get; private set; }

        public virtual bool Enabled
        {
            get { return true; }
        }

        public string ControlLabel
        {
            get
            {
                if (string.IsNullOrEmpty(Label))
                {
                    Debug.Assert(false, "Label not specificed. Invalid state");
                    return null;
                }

                m_tmpBuilder.Clear();
                if (string.IsNullOrEmpty(ControlName))
                    m_tmpBuilder.AppendFormat(FORMAT_LABEL, Label);
                else
                    m_tmpBuilder.AppendFormat(FORMAT_LABEL_HINT, Label, ControlName);
                if (!string.IsNullOrEmpty(CurrentValue))
                    m_tmpBuilder.Append(COLON);
                return m_tmpBuilder.ToString();
            }
        }

        public MyAbstractControlMenuItem(MyStringId controlCode, MySupportKeysEnum supportKeys = MySupportKeysEnum.NONE)
        {
            ControlName = ConstructCompleteControl(GetControlName(controlCode), supportKeys);
        }

        public MyAbstractControlMenuItem(string controlName, MySupportKeysEnum supportKeys = MySupportKeysEnum.NONE)
        {
            ControlName = ConstructCompleteControl(controlName, supportKeys);
        }

        public abstract void Activate();

        public virtual void Next()
        {
        }

        public virtual void Previous()
        {
        }

        public virtual void UpdateValue()
        {
        }

        private string GetControlName(MyStringId controlCode)
        {
            if (controlCode == MyStringId.NullOrEmpty)
                return null;
            else
            {
                string name = null;
                var control = MyInput.Static.GetGameControl(controlCode);
                if (control != null)
                {
                    var mouseControl = control.GetMouseControl();
                    var keyboardControl = control.GetKeyboardControl();
                    if (mouseControl != MyMouseButtonsEnum.None)
                        name = MyInput.Static.GetName(mouseControl);
                    else if (keyboardControl != MyKeys.None)
                        name = MyInput.Static.GetKeyName(keyboardControl);
                }

                return name;
            }
        }

        private string ConstructCompleteControl(string controlName, MySupportKeysEnum supportKeys)
        {
            m_tmpBuilder.Clear();
            if (HasSupportKey(supportKeys, MySupportKeysEnum.CTRL)) m_tmpBuilder.Append(CTRL).Append(PLUS);
            if (HasSupportKey(supportKeys, MySupportKeysEnum.SHIFT)) m_tmpBuilder.Append(SHIFT).Append(PLUS);
            if (HasSupportKey(supportKeys, MySupportKeysEnum.ALT)) m_tmpBuilder.Append(ALT).Append(PLUS);
            m_tmpBuilder.Append(controlName);
            return m_tmpBuilder.ToString();
        }

        private bool HasSupportKey(MySupportKeysEnum collection, MySupportKeysEnum key)
        {
            return (collection & key) == key;
        }
    }
}
