using System.Collections.Generic;
using VRage.Utils;

namespace VRage.Input
{
    public enum MyControlStateType
    {
        NEW_PRESSED,
        PRESSED,
        NEW_RELEASED,
    }

    public enum MyControlType
    {
        JoystickAxis,
        JoystickButton,
    }

    public static class MyControllerHelper
    {
        #region CODES
        private static readonly Dictionary<MyJoystickAxesEnum, char> XBOX_AXES_CODES = new Dictionary<MyJoystickAxesEnum, char>()
        {
            { MyJoystickAxesEnum.Xneg, '\xe016' },
            { MyJoystickAxesEnum.Xpos, '\xe015' },
            { MyJoystickAxesEnum.Ypos, '\xe014' },
            { MyJoystickAxesEnum.Yneg, '\xe017' },
            { MyJoystickAxesEnum.RotationXneg, '\xe020' },
            { MyJoystickAxesEnum.RotationXpos, '\xe019' },
            { MyJoystickAxesEnum.RotationYneg, '\xe021' },
            { MyJoystickAxesEnum.RotationYpos, '\xe018' },
            { MyJoystickAxesEnum.Zneg, '\xe007' },
            { MyJoystickAxesEnum.Zpos, '\xe008' },
        };

        private static readonly Dictionary<MyJoystickButtonsEnum, char> XBOX_BUTTONS_CODES = new Dictionary<MyJoystickButtonsEnum, char>()
        {
            { MyJoystickButtonsEnum.J01, '\xe001' },
            { MyJoystickButtonsEnum.J02, '\xe003' },
            { MyJoystickButtonsEnum.J03, '\xe002' },
            { MyJoystickButtonsEnum.J04, '\xe004' },
            { MyJoystickButtonsEnum.J05, '\xe005' },
            { MyJoystickButtonsEnum.J06, '\xe006' },
            { MyJoystickButtonsEnum.J07, '\xe00d' },
            { MyJoystickButtonsEnum.J08, '\xe00e' },
            { MyJoystickButtonsEnum.J09, '\xe00b' },
            { MyJoystickButtonsEnum.J10, '\xe00c' },
            { MyJoystickButtonsEnum.JDLeft, '\xe010' },
            { MyJoystickButtonsEnum.JDUp, '\xe011' },
            { MyJoystickButtonsEnum.JDRight, '\xe012' },
            { MyJoystickButtonsEnum.JDDown, '\xe013' },
        };
        #endregion

        public static readonly MyStringId CX_BASE = MyStringId.GetOrCompute("BASE");
        public static readonly MyStringId CX_GUI = MyStringId.GetOrCompute("GUI");
        public static readonly MyStringId CX_CHARACTER = MyStringId.GetOrCompute("CHARACTER");

        private interface IControl
        {
            byte Code { get; }
            bool IsNewPressed();
            bool IsPressed();
            bool IsNewReleased();
            float AnalogValue();
            char ControlCode();
        }

        private class Context
        {
            public Context ParentContext;
            public Dictionary<MyStringId, IControl> Bindings;

            public IControl this[MyStringId id]
            {
                get
                {
                    if (Bindings.ContainsKey(id))
                        return Bindings[id];
                    else if (ParentContext != null)
                        return ParentContext[id];
                    else
                        return m_nullControl;
                }
                set
                {
                    Bindings[id] = value;
                }
            }

            public Context()
            {
                Bindings = new Dictionary<MyStringId, IControl>(MyStringId.Comparer);
            }
        }

        private class EmptyControl : IControl
        {
            public byte Code { get { return 0; } }

            public bool IsNewPressed()
            {
                return false;
            }

            public bool IsPressed()
            {
                return false;
            }

            public bool IsNewReleased()
            {
                return false;
            }

            public float AnalogValue()
            {
                return 0;
            }

            public char ControlCode()
            {
                return ' ';
            }
        }

        private class JoystickAxis : IControl
        {
            public MyJoystickAxesEnum Axis;

            public byte Code { get { return (byte)Axis; } }

            public JoystickAxis(MyJoystickAxesEnum axis) { Axis = axis; }

            public bool IsNewPressed()
            {
                return MyInput.Static.IsJoystickAxisNewPressed(Axis);
            }

            public bool IsPressed()
            {
                return MyInput.Static.IsJoystickAxisPressed(Axis);
            }

            public bool IsNewReleased()
            {
                return MyInput.Static.IsNewJoystickAxisReleased(Axis);
            }

            public float AnalogValue()
            {
                return MyInput.Static.GetJoystickAxisStateForGameplay(Axis);
            }

            public char ControlCode()
            {
                return XBOX_AXES_CODES[Axis];
            }
        }

        private class JoystickButton : IControl
        {
            public MyJoystickButtonsEnum Button;

            public byte Code { get { return (byte)Button; } }

            public JoystickButton(MyJoystickButtonsEnum button) { Button = button; }

            public bool IsNewPressed()
            {
                return MyInput.Static.IsJoystickButtonNewPressed(Button);
            }

            public bool IsPressed()
            {
                return MyInput.Static.IsJoystickButtonPressed(Button);
            }

            public bool IsNewReleased()
            {
                return MyInput.Static.IsNewJoystickButtonReleased(Button);
            }

            public float AnalogValue()
            {
                return IsPressed() ? 1 : 0;
            }

            public char ControlCode()
            {
                return XBOX_BUTTONS_CODES[Button];
            }
        }

        private static EmptyControl m_nullControl = new EmptyControl();
        private static Dictionary<MyStringId, Context> m_bindings = new Dictionary<MyStringId, Context>(MyStringId.Comparer);

        static MyControllerHelper()
        {
            m_bindings.Add(MyStringId.NullOrEmpty, new Context());
        }

        public static void AddContext(MyStringId context, MyStringId? parent = null)
        {
            if (!m_bindings.ContainsKey(context))
            {
                var contextObj = new Context();
                m_bindings.Add(context, contextObj);
                if (parent.HasValue && m_bindings.ContainsKey(parent.Value))
                {
                    contextObj.ParentContext = m_bindings[parent.Value];
                }
            }
        }

        public static void AddControl(MyStringId context, MyStringId stringId, MyJoystickAxesEnum axis)
        {
            m_bindings[context][stringId] = new JoystickAxis(axis);
        }

        public static void AddControl(MyStringId context, MyStringId stringId, MyJoystickButtonsEnum button)
        {
            m_bindings[context][stringId] = new JoystickButton(button);
        }

        public static void NullControl(MyStringId context, MyStringId stringId)
        {
            m_bindings[context][stringId] = m_nullControl;
        }

        public static void NullControl(MyStringId context, MyJoystickAxesEnum axis)
        {
            MyStringId key = MyStringId.NullOrEmpty;
            foreach (var pair in m_bindings[context].Bindings)
            {
                if (pair.Value is JoystickAxis && pair.Value.Code == (byte)axis)
                {
                    key = pair.Key;
                    break;
                }
            }
            if (key != MyStringId.NullOrEmpty)
                m_bindings[context][key] = m_nullControl;
        }

        public static void NullControl(MyStringId context, MyJoystickButtonsEnum button)
        {
            MyStringId key = MyStringId.NullOrEmpty;
            foreach (var pair in m_bindings[context].Bindings)
            {
                if (pair.Value is JoystickButton && pair.Value.Code == (byte)button)
                {
                    key = pair.Key;
                    break;
                }
            }
            if (key != MyStringId.NullOrEmpty)
                m_bindings[context][key] = m_nullControl;
        }

        public static bool IsControl(MyStringId context, MyStringId stringId, MyControlStateType type = MyControlStateType.NEW_PRESSED)
        {
            switch (type)
            { // temporary included cuz bindings support only joystick
                case MyControlStateType.NEW_PRESSED:
                    return MyInput.Static.IsNewGameControlPressed(stringId) || m_bindings[context][stringId].IsNewPressed();
                case MyControlStateType.NEW_RELEASED:
                    return MyInput.Static.IsNewGameControlReleased(stringId) || m_bindings[context][stringId].IsNewReleased();
                case MyControlStateType.PRESSED:
                    return MyInput.Static.IsGameControlPressed(stringId) || m_bindings[context][stringId].IsPressed();
            }

            return false;
        }

        public static float IsControlAnalog(MyStringId context, MyStringId stringId)
        {
            return MyInput.Static.GetGameControlAnalogState(stringId) + m_bindings[context][stringId].AnalogValue();
        }

        public static char GetCodeForControl(MyStringId context, MyStringId stringId)
        {
            return m_bindings[context][stringId].ControlCode();
        }
    }
}
