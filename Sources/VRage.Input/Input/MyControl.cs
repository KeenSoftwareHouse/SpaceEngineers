using System;
using System.Diagnostics;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRage.Input
{
    public class MyControl : ModAPI.IMyControl
    {
        private const int DEFAULT_CAPACITY = 16;
        private static StringBuilder m_toStringCache = new StringBuilder(DEFAULT_CAPACITY);

        struct Data
        {
            public MyStringId Name;
            public MyStringId ControlId;
            public MyGuiControlTypeEnum ControlType;
            public MyKeys KeyboardKey;
            public MyKeys KeyboardKey2;
            public MyMouseButtonsEnum MouseButton;
            public MyStringId? Description;
        }

        private Data m_data;

        private MyStringId m_name
        {
            get { return m_data.Name; }
            set { m_data.Name = value; }
        }
        private MyStringId m_controlId
        {
            get { return m_data.ControlId; }
            set { m_data.ControlId = value; }
        }
        private MyGuiControlTypeEnum m_controlType
        {
            get { return m_data.ControlType; }
            set { m_data.ControlType = value; }
        }
        private MyKeys m_keyboardKey
        {
            get { return m_data.KeyboardKey; }
            set { m_data.KeyboardKey = value; }
        }
        private MyKeys m_KeyboardKey2
        {
            get { return m_data.KeyboardKey2; }
            set { m_data.KeyboardKey2 = value; }
        }
        private MyMouseButtonsEnum m_mouseButton
        {
            get { return m_data.MouseButton; }
            set { m_data.MouseButton = value; }
        }

        public MyControl(MyStringId controlId,
            MyStringId name,
            MyGuiControlTypeEnum controlType,
            MyMouseButtonsEnum? defaultControlMouse,
            MyKeys? defaultControlKey,
            MyStringId? helpText = null,
            MyKeys? defaultControlKey2 = null,
            MyStringId? description = null)
        {
            m_controlId = controlId;
            m_name = name;
            m_controlType = controlType;
            m_mouseButton = defaultControlMouse ?? MyMouseButtonsEnum.None;
            m_keyboardKey = defaultControlKey ?? MyKeys.None;
            m_KeyboardKey2 = defaultControlKey2 ?? MyKeys.None;
            m_data.Description = description;
        }

        public MyControl(MyControl other)
        {
            this.CopyFrom(other);
        }

        public void SetControl(MyGuiInputDeviceEnum device, MyKeys key)
        {
            Debug.Assert(device == MyGuiInputDeviceEnum.Keyboard ||
                         device == MyGuiInputDeviceEnum.KeyboardSecond);

            if (device == MyGuiInputDeviceEnum.Keyboard)
                m_keyboardKey = key;
            else if (device == MyGuiInputDeviceEnum.KeyboardSecond)
                m_KeyboardKey2 = key;
            else
                MyLog.Default.WriteLine("ERROR: Setting non-keyboard device to keyboard control.");
        }

        public void SetControl(MyMouseButtonsEnum mouseButton)
        {
            m_mouseButton = mouseButton;
        }

        public void SetNoControl()
        {
            m_mouseButton = MyMouseButtonsEnum.None;
            m_keyboardKey = MyKeys.None;
            m_KeyboardKey2 = MyKeys.None;
        }

        public MyKeys GetKeyboardControl()
        {
            return m_keyboardKey;
        }

        public MyKeys GetSecondKeyboardControl()
        {
            return m_KeyboardKey2;
        }

        public MyMouseButtonsEnum GetMouseControl()
        {
            return m_mouseButton;
        }

        public bool IsPressed()
        {
            bool pressed = false;

            if (m_keyboardKey != MyKeys.None)
            {
                pressed = MyInput.Static.IsKeyPress(m_keyboardKey);
            }

            if (m_KeyboardKey2 != MyKeys.None && pressed == false)
            {
                pressed = MyInput.Static.IsKeyPress(m_KeyboardKey2);
            }

            if (m_mouseButton != MyMouseButtonsEnum.None && pressed == false)
            {
                pressed = MyInput.Static.IsMousePressed(m_mouseButton);
            }

            return pressed;
        }

        public bool IsNewPressed()
        {
            bool pressed = false;

            if (m_keyboardKey != MyKeys.None)
            {
                pressed = MyInput.Static.IsNewKeyPressed(m_keyboardKey);
            }

            if (m_KeyboardKey2 != MyKeys.None && pressed == false)
            {
                pressed = MyInput.Static.IsNewKeyPressed(m_KeyboardKey2);
            }

            if (m_mouseButton != MyMouseButtonsEnum.None && pressed == false)
            {
                pressed = MyInput.Static.IsNewMousePressed(m_mouseButton);
            }

            return pressed;
        }

        public bool IsNewReleased()
        {
            bool released = false;

            if (m_keyboardKey != MyKeys.None)
            {
                released = MyInput.Static.IsNewKeyReleased(m_keyboardKey);
            }

            if (m_KeyboardKey2 != MyKeys.None && released == false)
            {
                released = MyInput.Static.IsNewKeyReleased(m_KeyboardKey2);
            }

            if (m_mouseButton != MyMouseButtonsEnum.None && released == false)
            {
                switch (m_mouseButton)
                {
                    case MyMouseButtonsEnum.Left:
                        released = MyInput.Static.IsNewLeftMouseReleased();
                        break;
                    case MyMouseButtonsEnum.Middle:
                        released = MyInput.Static.IsNewMiddleMouseReleased();
                        break;
                    case MyMouseButtonsEnum.Right:
                        released = MyInput.Static.IsNewRightMouseReleased();
                        break;
                    case MyMouseButtonsEnum.XButton1:
                        released = MyInput.Static.IsNewXButton1MouseReleased();
                        break;
                    case MyMouseButtonsEnum.XButton2:
                        released = MyInput.Static.IsNewXButton2MouseReleased();
                        break;
                }
            }

            return released;
        }

        public bool IsJoystickPressed()
        {
            bool pressed = false;

            return pressed;
        }

        public bool IsNewJoystickPressed()
        {
            bool pressed = false;

            //if (m_joystickButton != MyJoystickButtonsEnum.None && pressed == false)
            //{
            //    pressed = MyInput.Static.IsJoystickButtonNewPressed(m_joystickButton);
            //}

            //if (m_joystickAxis != MyJoystickAxesEnum.None && pressed == false)
            //{
            //    pressed = MyInput.Static.IsJoystickAxisNewPressed(m_joystickAxis);
            //}

            return pressed;
        }

        public bool IsNewJoystickReleased()
        {
            bool released = false;

            //if (m_joystickButton != MyJoystickButtonsEnum.None && released == false)
            //{
            //    released = MyInput.Static.IsNewJoystickButtonReleased(m_joystickButton);
            //}
            //if (m_joystickAxis != MyJoystickAxesEnum.None && released == false)
            //{
            //    released = MyInput.Static.IsNewJoystickAxisReleased(m_joystickAxis);
            //}

            return released;
        }

        /// <summary>
        /// Return the analog state between 0 (not pressed at all) and 1 (fully pressed).
        /// If a digital button is mapped to an analog control, it can return only 0 or 1.
        /// </summary>
        public float GetAnalogState()
        {
            bool pressed = false;

            if (m_keyboardKey != MyKeys.None)
            {
                pressed = MyInput.Static.IsKeyPress(m_keyboardKey);
            }

            if (m_KeyboardKey2 != MyKeys.None && pressed == false)
            {
                pressed = MyInput.Static.IsKeyPress(m_KeyboardKey2);
            }

            if (m_mouseButton != MyMouseButtonsEnum.None && pressed == false)
            {
                switch (m_mouseButton)
                {
                    case MyMouseButtonsEnum.Left:
                        pressed = MyInput.Static.IsLeftMousePressed();
                        break;
                    case MyMouseButtonsEnum.Middle:
                        pressed = MyInput.Static.IsMiddleMousePressed();
                        break;
                    case MyMouseButtonsEnum.Right:
                        pressed = MyInput.Static.IsRightMousePressed();
                        break;
                    case MyMouseButtonsEnum.XButton1:
                        pressed = MyInput.Static.IsXButton1MousePressed();
                        break;
                    case MyMouseButtonsEnum.XButton2:
                        pressed = MyInput.Static.IsXButton2MousePressed();
                        break;
                }
            }

            //if (m_joystickButton != MyJoystickButtonsEnum.None && pressed == false)
            //{
            //    pressed = MyInput.Static.IsJoystickButtonPressed(m_joystickButton);
            //}

            if (pressed) return 1;

            //if (m_joystickAxis != MyJoystickAxesEnum.None)
            //{
            //    return MyInput.Static.GetJoystickAxisStateForGameplay(m_joystickAxis);
            //}
            return 0;
        }

        public MyStringId GetControlName()
        {
            return m_name;
        }

        public MyStringId? GetControlDescription()
        {
            return m_data.Description;
        }

        public MyGuiControlTypeEnum GetControlTypeEnum()
        {
            return m_controlType;
        }

        public MyStringId GetGameControlEnum()
        {
            return m_controlId;
        }

        public bool IsControlAssigned()
        {
            return (m_keyboardKey != MyKeys.None) ||
                (m_mouseButton != MyMouseButtonsEnum.None);
        }

        public bool IsControlAssigned(MyGuiInputDeviceEnum deviceType)
        {
            bool isAssigned = false;
            switch (deviceType)
            {
                case MyGuiInputDeviceEnum.Keyboard:
                    isAssigned = m_keyboardKey != MyKeys.None;
                    break;
                case MyGuiInputDeviceEnum.Mouse:
                    isAssigned = m_mouseButton != MyMouseButtonsEnum.None;
                    break;
            }
            return isAssigned;
        }

        public void CopyFrom(MyControl other)
        {
            m_data = other.m_data;
        }

        #region Control to string and StringBuilder conversions
        /// <summary>
        /// Causes allocation. Creates single string with list of assigned controls.
        /// </summary>
        public override string ToString()
        {
            return ButtonNames;
        }

        public string ButtonNames
        {
            get
            {
                m_toStringCache.Clear();
                AppendBoundButtonNames(ref m_toStringCache, unassignedText: MyInput.Static.GetUnassignedName());
                return m_toStringCache.ToString();
            }
        }

        public string ButtonNamesIgnoreSecondary
        {
            get
            {
                m_toStringCache.Clear();
                AppendBoundButtonNames(ref m_toStringCache, unassignedText: null, includeSecondary: false);
                return m_toStringCache.ToString();
            }
        }

        /// <summary>
        /// Causes allocation. Creates single StringBuilder with list of assigned controls. Caller
        /// takes ownership of returned StringBuilder (it is not stored internally).
        /// </summary>
        public StringBuilder ToStringBuilder(string unassignedText)
        {
            m_toStringCache.Clear();
            AppendBoundButtonNames(ref m_toStringCache, unassignedText: unassignedText);
            return new StringBuilder(m_toStringCache.Length).AppendStringBuilder(m_toStringCache);
        }

        public string GetControlButtonName(MyGuiInputDeviceEnum deviceType)
        {
            m_toStringCache.Clear();
            AppendBoundButtonNames(ref m_toStringCache, deviceType);
            return m_toStringCache.ToString();
        }

        public void AppendBoundKeyJustOne(ref StringBuilder output)
        {
            EnsureExists(ref output);
            if (m_keyboardKey != MyKeys.None)
                AppendName(ref output, m_keyboardKey);
            else
                AppendName(ref output, m_KeyboardKey2);
        }

        public void AppendBoundButtonNames(ref StringBuilder output, MyGuiInputDeviceEnum device, string separator = null)
        {
            EnsureExists(ref output);

            switch (device)
            {
                case MyGuiInputDeviceEnum.Keyboard:
                    if (separator == null)
                        AppendName(ref output, m_keyboardKey);
                    else
                        AppendName(ref output, m_keyboardKey, m_KeyboardKey2, separator);
                    break;

                case MyGuiInputDeviceEnum.KeyboardSecond:
                    if (separator == null)
                        AppendName(ref output, m_KeyboardKey2);
                    else
                        AppendName(ref output, m_keyboardKey, m_KeyboardKey2, separator);
                    break;

                case MyGuiInputDeviceEnum.Mouse:
                    AppendName(ref output, m_mouseButton);
                    break;
            }
        }

        public void AppendBoundButtonNames(ref StringBuilder output, string separator = ", ", string unassignedText = null, bool includeSecondary = true)
        {
            EnsureExists(ref output);

            // Uncomment to enable hidden devices once we support them again.
            //foreach (MyGuiInputDeviceEnum value in Enum.GetValues(typeof(MyGuiInputDeviceEnum)))
            MyGuiInputDeviceEnum[] devices = { MyGuiInputDeviceEnum.Keyboard, MyGuiInputDeviceEnum.Mouse };

            int appendCount = 0;
            foreach (MyGuiInputDeviceEnum device in devices)
            {
                if (!IsControlAssigned(device))
                    continue;

                if (appendCount > 0)
                    output.Append(separator);
                AppendBoundButtonNames(ref output, device, includeSecondary ? separator : null);
                ++appendCount;
            }

            if (appendCount == 0 && unassignedText != null)
                output.Append(unassignedText);
        }

        public static void AppendName(ref StringBuilder output, MyKeys key)
        {
            EnsureExists(ref output);
            if (key != MyKeys.None)
                output.Append(MyInput.Static.GetKeyName(key));
        }

        public static void AppendName(ref StringBuilder output, MyKeys key1, MyKeys key2, string separator)
        {
            EnsureExists(ref output);

            string key1str = null;
            string key2str = null;
            if (key1 != MyKeys.None)
                key1str = MyInput.Static.GetKeyName(key1);

            if (key2 != MyKeys.None)
                key2str = MyInput.Static.GetKeyName(key2);

            if (key1str != null && key2str != null)
                output.Append(key1str).Append(separator).Append(key2str);
            else if (key1str != null)
                output.Append(key1str);
            else if (key2str != null)
                output.Append(key2str);
        }

        public static void AppendName(ref StringBuilder output, MyMouseButtonsEnum mouseButton)
        {
            EnsureExists(ref output);
            output.Append(MyInput.Static.GetName(mouseButton));
        }

        public static void AppendName(ref StringBuilder output, MyJoystickButtonsEnum joystickButton)
        {
            EnsureExists(ref output);
            output.Append(MyInput.Static.GetName(joystickButton));
        }

        public static void AppendName(ref StringBuilder output, MyJoystickAxesEnum joystickAxis)
        {
            EnsureExists(ref output);
            output.Append(MyInput.Static.GetName(joystickAxis));
        }

        public static void AppendUnknownTextIfNeeded(ref StringBuilder output, string unassignedText)
        {
            EnsureExists(ref output);
            if (output.Length == 0)
                output.Append(unassignedText);
        }

        private static void EnsureExists(ref StringBuilder output)
        {
            if (output == null)
                output = new StringBuilder(DEFAULT_CAPACITY);
        }
        #endregion

    }
}
