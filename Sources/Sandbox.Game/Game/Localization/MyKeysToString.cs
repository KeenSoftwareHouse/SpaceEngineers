using System;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game.Localization
{
    abstract class MyUtilKeyToString
    {
        public MyKeys Key;
        public abstract string Name { get; } // Name of this Key

        public MyUtilKeyToString(MyKeys key)
        {
            Key = key;
        }
    }

    class MyUtilKeyToStringSimple : MyUtilKeyToString
    {
        private string m_name;
        public override string Name
        {
            get { return m_name; }
        }

        public MyUtilKeyToStringSimple(MyKeys key, string name) :
            base(key)
        {
            m_name = name;
        }
    }

    class MyUtilKeyToStringLocalized : MyUtilKeyToString
    {
        private MyStringId m_name;
        public override string Name
        {
            get { return MyTexts.GetString(m_name); }
        }

        public MyUtilKeyToStringLocalized(MyKeys key, MyStringId name) :
            base(key)
        {
            m_name = name;
        }
    }

    public class MyKeysToString : IMyControlNameLookup
    {
        private readonly String[] m_systemKeyNamesLower = new String[256];
        private readonly String[] m_systemKeyNamesUpper = new String[256];

        private readonly MyUtilKeyToString[] m_keyToString = new MyUtilKeyToString[]
        {
            new MyUtilKeyToStringLocalized(MyKeys.Left, MySpaceTexts.KeysLeft),
            new MyUtilKeyToStringLocalized(MyKeys.Right, MySpaceTexts.KeysRight),
            new MyUtilKeyToStringLocalized(MyKeys.Up, MySpaceTexts.KeysUp),
            new MyUtilKeyToStringLocalized(MyKeys.Down, MySpaceTexts.KeysDown),
            new MyUtilKeyToStringLocalized(MyKeys.Home, MySpaceTexts.KeysHome),
            new MyUtilKeyToStringLocalized(MyKeys.End, MySpaceTexts.KeysEnd),
            new MyUtilKeyToStringLocalized(MyKeys.Delete, MySpaceTexts.KeysDelete),
            new MyUtilKeyToStringLocalized(MyKeys.Back, MySpaceTexts.KeysBackspace),
            new MyUtilKeyToStringLocalized(MyKeys.Insert, MySpaceTexts.KeysInsert),
            new MyUtilKeyToStringLocalized(MyKeys.PageDown, MySpaceTexts.KeysPageDown),
            new MyUtilKeyToStringLocalized(MyKeys.PageUp, MySpaceTexts.KeysPageUp),
            new MyUtilKeyToStringLocalized(MyKeys.LeftAlt, MySpaceTexts.KeysLeftAlt),
            new MyUtilKeyToStringLocalized(MyKeys.LeftControl, MySpaceTexts.KeysLeftControl),
            new MyUtilKeyToStringLocalized(MyKeys.LeftShift, MySpaceTexts.KeysLeftShift),
            new MyUtilKeyToStringLocalized(MyKeys.RightAlt, MySpaceTexts.KeysRightAlt),
            new MyUtilKeyToStringLocalized(MyKeys.RightControl, MySpaceTexts.KeysRightControl),
            new MyUtilKeyToStringLocalized(MyKeys.RightShift, MySpaceTexts.KeysRightShift),
            new MyUtilKeyToStringLocalized(MyKeys.CapsLock, MySpaceTexts.KeysCapsLock),
            new MyUtilKeyToStringLocalized(MyKeys.Enter, MySpaceTexts.KeysEnter),
            new MyUtilKeyToStringLocalized(MyKeys.Tab, MySpaceTexts.KeysTab),
            new MyUtilKeyToStringLocalized(MyKeys.OemOpenBrackets, MySpaceTexts.KeysOpenBracket),
            new MyUtilKeyToStringLocalized(MyKeys.OemCloseBrackets, MySpaceTexts.KeysCloseBracket),
            new MyUtilKeyToStringLocalized(MyKeys.Multiply, MySpaceTexts.KeysMultiply),
            new MyUtilKeyToStringLocalized(MyKeys.Subtract, MySpaceTexts.KeysSubtract),
            new MyUtilKeyToStringLocalized(MyKeys.Add, MySpaceTexts.KeysAdd),
            new MyUtilKeyToStringLocalized(MyKeys.Divide, MySpaceTexts.KeysDivide),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad0, MySpaceTexts.KeysNumPad0),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad1, MySpaceTexts.KeysNumPad1),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad2, MySpaceTexts.KeysNumPad2),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad3, MySpaceTexts.KeysNumPad3),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad4, MySpaceTexts.KeysNumPad4),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad5, MySpaceTexts.KeysNumPad5),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad6, MySpaceTexts.KeysNumPad6),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad7, MySpaceTexts.KeysNumPad7),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad8, MySpaceTexts.KeysNumPad8),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad9, MySpaceTexts.KeysNumPad9),
            new MyUtilKeyToStringLocalized(MyKeys.Decimal, MySpaceTexts.KeysDecimal),
            new MyUtilKeyToStringLocalized(MyKeys.OemBackslash, MySpaceTexts.KeysBackslash),
            new MyUtilKeyToStringLocalized(MyKeys.OemComma, MySpaceTexts.KeysComma),
            new MyUtilKeyToStringLocalized(MyKeys.OemMinus, MySpaceTexts.KeysMinus),
            new MyUtilKeyToStringLocalized(MyKeys.OemPeriod, MySpaceTexts.KeysPeriod),
            new MyUtilKeyToStringLocalized(MyKeys.OemPipe, MySpaceTexts.KeysPipe),
            new MyUtilKeyToStringLocalized(MyKeys.OemPlus, MySpaceTexts.KeysPlus),
            new MyUtilKeyToStringLocalized(MyKeys.OemQuestion, MySpaceTexts.KeysQuestion),
            new MyUtilKeyToStringLocalized(MyKeys.OemQuotes, MySpaceTexts.KeysQuotes),
            new MyUtilKeyToStringLocalized(MyKeys.OemSemicolon, MySpaceTexts.KeysSemicolon),
            new MyUtilKeyToStringLocalized(MyKeys.OemTilde, MySpaceTexts.KeysTilde),
            new MyUtilKeyToStringLocalized(MyKeys.Space, MySpaceTexts.KeysSpace),
            new MyUtilKeyToStringLocalized(MyKeys.Pause, MySpaceTexts.KeysPause),

            new MyUtilKeyToStringSimple(MyKeys.D0, "0"),
            new MyUtilKeyToStringSimple(MyKeys.D1, "1"),
            new MyUtilKeyToStringSimple(MyKeys.D2, "2"),
            new MyUtilKeyToStringSimple(MyKeys.D3, "3"),
            new MyUtilKeyToStringSimple(MyKeys.D4, "4"),
            new MyUtilKeyToStringSimple(MyKeys.D5, "5"),
            new MyUtilKeyToStringSimple(MyKeys.D6, "6"),
            new MyUtilKeyToStringSimple(MyKeys.D7, "7"),
            new MyUtilKeyToStringSimple(MyKeys.D8, "8"),
            new MyUtilKeyToStringSimple(MyKeys.D9, "9"),
            new MyUtilKeyToStringSimple(MyKeys.F1, "F1"),
            new MyUtilKeyToStringSimple(MyKeys.F2, "F2"),
            new MyUtilKeyToStringSimple(MyKeys.F3, "F3"),
            new MyUtilKeyToStringSimple(MyKeys.F4, "F4"),
            new MyUtilKeyToStringSimple(MyKeys.F5, "F5"),
            new MyUtilKeyToStringSimple(MyKeys.F6, "F6"),
            new MyUtilKeyToStringSimple(MyKeys.F7, "F7"),
            new MyUtilKeyToStringSimple(MyKeys.F8, "F8"),
            new MyUtilKeyToStringSimple(MyKeys.F9, "F9"),
            new MyUtilKeyToStringSimple(MyKeys.F10, "F10"),
            new MyUtilKeyToStringSimple(MyKeys.F11, "F11"),
            new MyUtilKeyToStringSimple(MyKeys.F12, "F12"),
            new MyUtilKeyToStringSimple(MyKeys.F13, "F13"),
            new MyUtilKeyToStringSimple(MyKeys.F14, "F14"),
            new MyUtilKeyToStringSimple(MyKeys.F15, "F15"),
            new MyUtilKeyToStringSimple(MyKeys.F16, "F16"),
            new MyUtilKeyToStringSimple(MyKeys.F17, "F17"),
            new MyUtilKeyToStringSimple(MyKeys.F18, "F18"),
            new MyUtilKeyToStringSimple(MyKeys.F19, "F19"),
            new MyUtilKeyToStringSimple(MyKeys.F20, "F20"),
            new MyUtilKeyToStringSimple(MyKeys.F21, "F21"),
            new MyUtilKeyToStringSimple(MyKeys.F22, "F22"),
            new MyUtilKeyToStringSimple(MyKeys.F23, "F23"),
            new MyUtilKeyToStringSimple(MyKeys.F24, "F24"),
        };

        public MyKeysToString()
        {
            for (int i = 0; i < m_systemKeyNamesLower.Length; i++)
            {
                m_systemKeyNamesLower[i] = ((char)i).ToString().ToLower();
                m_systemKeyNamesUpper[i] = ((char)i).ToString().ToUpper();
            }
        }

        string IMyControlNameLookup.UnassignedText
        {
            get { return MyTexts.GetString(MySpaceTexts.UnknownControl_Unassigned); }
        }

        string IMyControlNameLookup.GetKeyName(MyKeys key)
        {
            if ((int)key >= m_systemKeyNamesUpper.Length)
                return null;

            String retVal = m_systemKeyNamesUpper[(int)key];
            for (int j = 0; j < m_keyToString.Length; j++)
            {
                if (m_keyToString[j].Key == key)
                {
                    retVal = m_keyToString[j].Name;
                    break;
                }
            }
            return retVal;
        }

        string IMyControlNameLookup.GetName(MyMouseButtonsEnum button)
        {
            switch (button)
            {
                case MyMouseButtonsEnum.Left: return MyTexts.GetString(MySpaceTexts.LeftMouseButton);
                case MyMouseButtonsEnum.Middle: return MyTexts.GetString(MySpaceTexts.MiddleMouseButton);
                case MyMouseButtonsEnum.Right: return MyTexts.GetString(MySpaceTexts.RightMouseButton);
                case MyMouseButtonsEnum.XButton1: return MyTexts.GetString(MySpaceTexts.MouseXButton1);
                case MyMouseButtonsEnum.XButton2: return MyTexts.GetString(MySpaceTexts.MouseXButton2);
            }
            return MyTexts.GetString(MySpaceTexts.Blank);
        }

        string IMyControlNameLookup.GetName(MyJoystickButtonsEnum joystickButton)
        {
            if (joystickButton == MyJoystickButtonsEnum.None)
                return "";

            switch (joystickButton)
            {
                case MyJoystickButtonsEnum.JDLeft:  return MyTexts.GetString(MySpaceTexts.JoystickButtonLeft);
                case MyJoystickButtonsEnum.JDRight: return MyTexts.GetString(MySpaceTexts.JoystickButtonRight);
                case MyJoystickButtonsEnum.JDUp:    return MyTexts.GetString(MySpaceTexts.JoystickButtonUp);
                case MyJoystickButtonsEnum.JDDown:  return MyTexts.GetString(MySpaceTexts.JoystickButtonDown);

                default:
                    return "JB" + ((int)joystickButton - 4);
            }
        }

        string IMyControlNameLookup.GetName(MyJoystickAxesEnum joystickAxis)
        {
            switch (joystickAxis)
            {
                case MyJoystickAxesEnum.Xpos: return "JX+";
                case MyJoystickAxesEnum.Xneg: return "JX-";
                case MyJoystickAxesEnum.Ypos: return "JY+";
                case MyJoystickAxesEnum.Yneg: return "JY-";
                case MyJoystickAxesEnum.Zpos: return "JZ+";
                case MyJoystickAxesEnum.Zneg: return "JZ-";
                case MyJoystickAxesEnum.RotationXpos: return MyTexts.GetString(MySpaceTexts.JoystickRotationXpos);
                case MyJoystickAxesEnum.RotationXneg: return MyTexts.GetString(MySpaceTexts.JoystickRotationXneg);
                case MyJoystickAxesEnum.RotationYpos: return MyTexts.GetString(MySpaceTexts.JoystickRotationYpos);
                case MyJoystickAxesEnum.RotationYneg: return MyTexts.GetString(MySpaceTexts.JoystickRotationYneg);
                case MyJoystickAxesEnum.RotationZpos: return MyTexts.GetString(MySpaceTexts.JoystickRotationZpos);
                case MyJoystickAxesEnum.RotationZneg: return MyTexts.GetString(MySpaceTexts.JoystickRotationZneg);
                case MyJoystickAxesEnum.Slider1pos: return MyTexts.GetString(MySpaceTexts.JoystickSlider1pos);
                case MyJoystickAxesEnum.Slider1neg: return MyTexts.GetString(MySpaceTexts.JoystickSlider1neg);
                case MyJoystickAxesEnum.Slider2pos: return MyTexts.GetString(MySpaceTexts.JoystickSlider2pos);
                case MyJoystickAxesEnum.Slider2neg: return MyTexts.GetString(MySpaceTexts.JoystickSlider2neg);
            }

            return "";
        }
    }
}
