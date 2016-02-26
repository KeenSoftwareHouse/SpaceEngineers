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
            new MyUtilKeyToStringLocalized(MyKeys.Left, MyCommonTexts.KeysLeft),
            new MyUtilKeyToStringLocalized(MyKeys.Right, MyCommonTexts.KeysRight),
            new MyUtilKeyToStringLocalized(MyKeys.Up, MyCommonTexts.KeysUp),
            new MyUtilKeyToStringLocalized(MyKeys.Down, MyCommonTexts.KeysDown),
            new MyUtilKeyToStringLocalized(MyKeys.Home, MyCommonTexts.KeysHome),
            new MyUtilKeyToStringLocalized(MyKeys.End, MyCommonTexts.KeysEnd),
            new MyUtilKeyToStringLocalized(MyKeys.Delete, MyCommonTexts.KeysDelete),
            new MyUtilKeyToStringLocalized(MyKeys.Back, MyCommonTexts.KeysBackspace),
            new MyUtilKeyToStringLocalized(MyKeys.Insert, MyCommonTexts.KeysInsert),
            new MyUtilKeyToStringLocalized(MyKeys.PageDown, MyCommonTexts.KeysPageDown),
            new MyUtilKeyToStringLocalized(MyKeys.PageUp, MyCommonTexts.KeysPageUp),
            new MyUtilKeyToStringLocalized(MyKeys.LeftAlt, MyCommonTexts.KeysLeftAlt),
            new MyUtilKeyToStringLocalized(MyKeys.LeftControl, MyCommonTexts.KeysLeftControl),
            new MyUtilKeyToStringLocalized(MyKeys.LeftShift, MyCommonTexts.KeysLeftShift),
            new MyUtilKeyToStringLocalized(MyKeys.RightAlt, MyCommonTexts.KeysRightAlt),
            new MyUtilKeyToStringLocalized(MyKeys.RightControl, MyCommonTexts.KeysRightControl),
            new MyUtilKeyToStringLocalized(MyKeys.RightShift, MyCommonTexts.KeysRightShift),
            new MyUtilKeyToStringLocalized(MyKeys.CapsLock, MyCommonTexts.KeysCapsLock),
            new MyUtilKeyToStringLocalized(MyKeys.Enter, MyCommonTexts.KeysEnter),
            new MyUtilKeyToStringLocalized(MyKeys.Tab, MyCommonTexts.KeysTab),
            new MyUtilKeyToStringLocalized(MyKeys.OemOpenBrackets, MyCommonTexts.KeysOpenBracket),
            new MyUtilKeyToStringLocalized(MyKeys.OemCloseBrackets, MyCommonTexts.KeysCloseBracket),
            new MyUtilKeyToStringLocalized(MyKeys.Multiply, MyCommonTexts.KeysMultiply),
            new MyUtilKeyToStringLocalized(MyKeys.Subtract, MyCommonTexts.KeysSubtract),
            new MyUtilKeyToStringLocalized(MyKeys.Add, MyCommonTexts.KeysAdd),
            new MyUtilKeyToStringLocalized(MyKeys.Divide, MyCommonTexts.KeysDivide),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad0, MyCommonTexts.KeysNumPad0),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad1, MyCommonTexts.KeysNumPad1),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad2, MyCommonTexts.KeysNumPad2),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad3, MyCommonTexts.KeysNumPad3),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad4, MyCommonTexts.KeysNumPad4),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad5, MyCommonTexts.KeysNumPad5),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad6, MyCommonTexts.KeysNumPad6),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad7, MyCommonTexts.KeysNumPad7),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad8, MyCommonTexts.KeysNumPad8),
            new MyUtilKeyToStringLocalized(MyKeys.NumPad9, MyCommonTexts.KeysNumPad9),
            new MyUtilKeyToStringLocalized(MyKeys.Decimal, MyCommonTexts.KeysDecimal),
            new MyUtilKeyToStringLocalized(MyKeys.OemBackslash, MyCommonTexts.KeysBackslash),
            new MyUtilKeyToStringLocalized(MyKeys.OemComma, MyCommonTexts.KeysComma),
            new MyUtilKeyToStringLocalized(MyKeys.OemMinus, MyCommonTexts.KeysMinus),
            new MyUtilKeyToStringLocalized(MyKeys.OemPeriod, MyCommonTexts.KeysPeriod),
            new MyUtilKeyToStringLocalized(MyKeys.OemPipe, MyCommonTexts.KeysPipe),
            new MyUtilKeyToStringLocalized(MyKeys.OemPlus, MyCommonTexts.KeysPlus),
            new MyUtilKeyToStringLocalized(MyKeys.OemQuestion, MyCommonTexts.KeysQuestion),
            new MyUtilKeyToStringLocalized(MyKeys.OemQuotes, MyCommonTexts.KeysQuotes),
            new MyUtilKeyToStringLocalized(MyKeys.OemSemicolon, MyCommonTexts.KeysSemicolon),
            new MyUtilKeyToStringLocalized(MyKeys.OemTilde, MyCommonTexts.KeysTilde),
            new MyUtilKeyToStringLocalized(MyKeys.Space, MyCommonTexts.KeysSpace),
            new MyUtilKeyToStringLocalized(MyKeys.Pause, MyCommonTexts.KeysPause),

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
            get { return MyTexts.GetString(MyCommonTexts.UnknownControl_Unassigned); }
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
                case MyMouseButtonsEnum.Left: return MyTexts.GetString(MyCommonTexts.LeftMouseButton);
                case MyMouseButtonsEnum.Middle: return MyTexts.GetString(MyCommonTexts.MiddleMouseButton);
                case MyMouseButtonsEnum.Right: return MyTexts.GetString(MyCommonTexts.RightMouseButton);
                case MyMouseButtonsEnum.XButton1: return MyTexts.GetString(MyCommonTexts.MouseXButton1);
                case MyMouseButtonsEnum.XButton2: return MyTexts.GetString(MyCommonTexts.MouseXButton2);
            }
            return MyTexts.GetString(MySpaceTexts.Blank);
        }

        string IMyControlNameLookup.GetName(MyJoystickButtonsEnum joystickButton)
        {
            if (joystickButton == MyJoystickButtonsEnum.None)
                return "";

            switch (joystickButton)
            {
                case MyJoystickButtonsEnum.JDLeft:  return MyTexts.GetString(MyCommonTexts.JoystickButtonLeft);
                case MyJoystickButtonsEnum.JDRight: return MyTexts.GetString(MyCommonTexts.JoystickButtonRight);
                case MyJoystickButtonsEnum.JDUp:    return MyTexts.GetString(MyCommonTexts.JoystickButtonUp);
                case MyJoystickButtonsEnum.JDDown:  return MyTexts.GetString(MyCommonTexts.JoystickButtonDown);

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
                case MyJoystickAxesEnum.RotationXpos: return MyTexts.GetString(MyCommonTexts.JoystickRotationXpos);
                case MyJoystickAxesEnum.RotationXneg: return MyTexts.GetString(MyCommonTexts.JoystickRotationXneg);
                case MyJoystickAxesEnum.RotationYpos: return MyTexts.GetString(MyCommonTexts.JoystickRotationYpos);
                case MyJoystickAxesEnum.RotationYneg: return MyTexts.GetString(MyCommonTexts.JoystickRotationYneg);
                case MyJoystickAxesEnum.RotationZpos: return MyTexts.GetString(MyCommonTexts.JoystickRotationZpos);
                case MyJoystickAxesEnum.RotationZneg: return MyTexts.GetString(MyCommonTexts.JoystickRotationZneg);
                case MyJoystickAxesEnum.Slider1pos: return MyTexts.GetString(MyCommonTexts.JoystickSlider1pos);
                case MyJoystickAxesEnum.Slider1neg: return MyTexts.GetString(MyCommonTexts.JoystickSlider1neg);
                case MyJoystickAxesEnum.Slider2pos: return MyTexts.GetString(MyCommonTexts.JoystickSlider2pos);
                case MyJoystickAxesEnum.Slider2neg: return MyTexts.GetString(MyCommonTexts.JoystickSlider2neg);
            }

            return "";
        }
    }
}
