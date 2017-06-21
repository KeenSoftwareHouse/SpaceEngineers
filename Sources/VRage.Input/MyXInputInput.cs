#if XB1

#region Using

using XB1Interface;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
// using System.Windows.Forms;

using VRage.Collections;
using VRage.Library.Utils;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;


#endregion


namespace VRage.Input
{


    public partial class MyXInputInput : IMyInput
    {
        
        private class JoystickHelper
        {

            internal enum XInputGamepadButtonFlags
            {
                // Summary:
                //     No documentation.
                Y = -32768,
                //
                // Summary:
                //     None.
                None = 0,
                //
                // Summary:
                //      Bitmask of the device digital buttons, as follows. A set bit indicates that
                //     the corresponding button is pressed. Device buttonBitmask SharpDX.XInput.GamepadButtonFlags.DPadUp
                //     0x0001 SharpDX.XInput.GamepadButtonFlags.DPadDown 0x0002 SharpDX.XInput.GamepadButtonFlags.DPadLeft
                //     0x0004 SharpDX.XInput.GamepadButtonFlags.DPadRight 0x0008 SharpDX.XInput.GamepadButtonFlags.Start
                //     0x0010 SharpDX.XInput.GamepadButtonFlags.Back 0x0020 SharpDX.XInput.GamepadButtonFlags.LeftThumb
                //     0x0040 SharpDX.XInput.GamepadButtonFlags.RightThumb 0x0080 SharpDX.XInput.GamepadButtonFlags.LeftShoulder
                //     0x0100 SharpDX.XInput.GamepadButtonFlags.RightShoulder 0x0200 SharpDX.XInput.GamepadButtonFlags.A
                //     0x1000 SharpDX.XInput.GamepadButtonFlags.B 0x2000 SharpDX.XInput.GamepadButtonFlags.X
                //     0x4000 SharpDX.XInput.GamepadButtonFlags.Y 0x8000 ? Bits that are set but
                //     not defined above are reserved, and their state is undefined.
                DPadUp = 1,
                //
                // Summary:
                //      The current value of the left trigger analog control. The value is between
                //     0 and 255.
                DPadDown = 2,
                //
                // Summary:
                //      The current value of the right trigger analog control. The value is between
                //     0 and 255.
                DPadLeft = 4,
                //
                // Summary:
                //      Left thumbstick x-axis value. Each of the thumbstick axis members is a signed
                //     value between -32768 and 32767 describing the position of the thumbstick.
                //     A value of 0 is centered. Negative values signify down or to the left. Positive
                //     values signify up or to the right. The constants SharpDX.XInput.Gamepad.LeftThumbDeadZone
                //     or SharpDX.XInput.Gamepad.RightThumbDeadZone can be used as a positive and
                //     negative value to filter a thumbstick input.
                DPadRight = 8,
                //
                // Summary:
                //      Left thumbstick y-axis value. The value is between -32768 and 32767.
                Start = 16,
                //
                // Summary:
                //      Right thumbstick x-axis value. The value is between -32768 and 32767.
                Back = 32,
                //
                // Summary:
                //      Right thumbstick y-axis value. The value is between -32768 and 32767.
                LeftThumb = 64,
                //
                // Summary:
                //     No documentation.
                RightThumb = 128,
                //
                // Summary:
                //     No documentation.
                LeftShoulder = 256,
                //
                // Summary:
                //     No documentation.
                RightShoulder = 512,
                //
                // Summary:
                //     No documentation.
                A = 4096,
                //
                // Summary:
                //     No documentation.
                B = 8192,
                //
                // Summary:
                //     No documentation.
                X = 16384,
            }

            internal static XInputGamepadButtonFlags[] kArrVRageToXInput = new XInputGamepadButtonFlags[]
            {
                XInputGamepadButtonFlags.None,
                XInputGamepadButtonFlags.DPadLeft,  // Directional pad buttons
                XInputGamepadButtonFlags.DPadRight,
                XInputGamepadButtonFlags.DPadUp,
                XInputGamepadButtonFlags.DPadDown,
                XInputGamepadButtonFlags.A, // J01 = 5,  // Regular buttons (up to 16)
                XInputGamepadButtonFlags.B, // J02 = 6,  // If you add new button here, dont forget to change value in MyJoystickConstants
                XInputGamepadButtonFlags.X, // J03 = 7,
                XInputGamepadButtonFlags.Y, // J04 = 8,
                XInputGamepadButtonFlags.LeftShoulder, // J05 = 9,
                XInputGamepadButtonFlags.RightShoulder, // J06 = 10,
                XInputGamepadButtonFlags.Back, // J07 = 11,
                XInputGamepadButtonFlags.Start, // J08 = 12,
                XInputGamepadButtonFlags.LeftThumb, // J09 = 13,
                XInputGamepadButtonFlags.RightThumb, // J10 = 14,
                XInputGamepadButtonFlags.None, // J11 = 15,
                XInputGamepadButtonFlags.None, // J12 = 16,
                XInputGamepadButtonFlags.None, // J13 = 17,
                XInputGamepadButtonFlags.None, // J14 = 18,
                XInputGamepadButtonFlags.None, // J15 = 19,
                XInputGamepadButtonFlags.None, // J16 = 20
            };

            static public uint VRageToXInput(MyJoystickButtonsEnum button)
            {
                return (uint)(kArrVRageToXInput[(byte)button]);
            }

        }
        




        public void AddDefaultControl(MyStringId stringId, MyControl control)
        {
#if !XB1_SKIPASSERTFORNOW
            Debug.Assert(false, "Not implemented!");
#endif // !XB1_SKIPASSERTFORNOW
        }




        public bool IsCapsLock { get { return false; } }
        public bool IsNumLock { get { return false; } }
        public bool IsScrollLock { get { return false; } }

        Vector2 m_absoluteMousePosition;

        //  State Variables
        MyMouseState m_previousMouseState;



        XB1Interface.XB1Interface.GamepadState m_previousJoystickState;
        XB1Interface.XB1Interface.GamepadState m_actualJoystickState;

        MyMouseState m_actualMouseState;
        MyMouseState m_actualMouseStateRaw;
               

        //  Control properties
        bool m_mouseXIsInverted;
        bool m_mouseYIsInverted;
        float m_mouseSensitivity;
        private string m_joystickInstanceName;
        public string JoystickInstanceName
        {
            get { return m_joystickInstanceName; }
            set
            {
                if (m_joystickInstanceName != value)
                {
                    m_joystickInstanceName = value;

                    //MySandboxGame.Static.Invoke(() => { InitializeJoystickIfPossible(); });
                    InitializeJoystickIfPossible();
                }
            }
        }
        float m_joystickSensitivity;
        float m_joystickDeadzone;
        float m_joystickExponent;
        public bool IsMouseXInvertedDefault { get { return false; } }
        public bool IsMouseYInvertedDefault { get { return false; } }
        public float MouseSensitivityDefault { get { return 1.655f; } }
        public string JoystickInstanceNameDefault { get { return null; } }
        public float JoystickSensitivityDefault { get { return 2.0f; } }
        public float JoystickExponentDefault { get { return 2.0f; } }
        public float JoystickDeadzoneDefault { get { return 0.2f; } }

        string m_joystickInstanceNameSnapshot;
        
        //  Control lists
        Dictionary<MyStringId, MyControl> m_defaultGameControlsList;
        Dictionary<MyStringId, MyControl> m_gameControlsList = new Dictionary<MyStringId, MyControl>(MyStringId.Comparer);
        Dictionary<MyStringId, MyControl> m_gameControlsSnapshot = new Dictionary<MyStringId, MyControl>(MyStringId.Comparer);


        //  Lists of valid keys and buttons
        List<MyJoystickButtonsEnum> m_validJoystickButtons = new List<MyJoystickButtonsEnum>();
        List<MyJoystickAxesEnum> m_validJoystickAxes = new List<MyJoystickAxesEnum>();
        List<MyMouseButtonsEnum> m_validMouseButtons = new List<MyMouseButtonsEnum>();

        //  Joystick variables
        SharpDX.XInput.Controller m_joystick;


        IMyBufferedInputSource m_bufferedInputSource;
        IMyControlNameLookup m_nameLookup;
        List<char> m_currentTextInput = new List<char>();
        List<MyKeys> m_tmpPressedKeys = new List<MyKeys>(10);


        public MyXInputInput(
            IMyBufferedInputSource textInputBuffer,
            IMyControlNameLookup nameLookup,
            Dictionary<MyStringId, MyControl> gameControls,
            bool enableDevKeys)
        {
            m_bufferedInputSource = textInputBuffer;
            m_nameLookup = nameLookup;
            m_defaultGameControlsList = gameControls;
            m_gameControlsList = new Dictionary<MyStringId, MyControl>(MyStringId.Comparer);
            m_gameControlsSnapshot = new Dictionary<MyStringId, MyControl>(MyStringId.Comparer);
            CloneControls(m_defaultGameControlsList, m_gameControlsList);
            ENABLE_DEVELOPER_KEYS = enableDevKeys;
        }

        public void LoadData(SerializableDictionary<string, object> controlsGeneral, SerializableDictionary<string, object> controlsButtons)
        {
            m_mouseXIsInverted = IsMouseXInvertedDefault;
            m_mouseYIsInverted = IsMouseYInvertedDefault;
            m_mouseSensitivity = MouseSensitivityDefault;
            m_joystickInstanceName = JoystickInstanceNameDefault;
            m_joystickSensitivity = JoystickSensitivityDefault;
            m_joystickDeadzone = JoystickDeadzoneDefault;
            m_joystickExponent = JoystickExponentDefault;   

            //  List of assignable joystick buttons
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J01);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J02);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J03);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J04);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J05);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J06);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J07);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J08);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J09);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J10);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J11);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J12);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J13);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J14);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J15);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.J16);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.JDLeft);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.JDRight);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.JDUp);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.JDDown);
            m_validJoystickButtons.Add(MyJoystickButtonsEnum.None);

            //  List of assignable joystick axes
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Xpos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Xneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Ypos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Yneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Zpos);
            /*m_validJoystickAxes.Add(MyJoystickAxesEnum.Zneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationXpos);*/
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationXneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationYpos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationYneg);
            /*m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationZpos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationZneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Slider1pos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Slider1neg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Slider2pos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Slider2neg);*/
            m_validJoystickAxes.Add(MyJoystickAxesEnum.None);

            LoadControls(controlsGeneral, controlsButtons);
            TakeSnapshot();
        }

#if !XB1
        IntPtr m_windowHandle;
        public void LoadContent(IntPtr windowHandle)
#else // XB1
        public void LoadContent()
#endif // XB1
        {
#if !XB1
            m_windowHandle = windowHandle;
#endif // !XB1
            

            if (ENABLE_DEVELOPER_KEYS)
            {
                MyLog.Default.WriteLine("DEVELOPER KEYS ENABLED");
            }


            //  Make sure that DirectInput has been initialized
            InitializeJoystickIfPossible();

        }

#if !XB1
        public IntPtr WindowHandle
        {
            get { return m_windowHandle; }
        }
#endif // !XB1

        public ListReader<char> TextInput
        {
            get { return new ListReader<char>(m_currentTextInput); }
        }


        public void UnloadData()
        {
            //MyDirectInput.Close();
        }

        void CheckValidControls(Dictionary<MyStringId, MyControl> controls)
        {
            foreach (MyControl control in controls.Values)
            {
                MyDebug.AssertDebug(IsKeyValid(control.GetKeyboardControl()));
                MyDebug.AssertDebug(IsMouseButtonValid(control.GetMouseControl()));
            }
        }

        public List<string> EnumerateJoystickNames()
        {
            var results = new List<string>();
            
            results.Add("User_" + m_joystick.UserIndex);

            return results;
        }


        //call this on call back when something is beeing plugged in or unplugged
        public void InitializeJoystickIfPossible()
        {
            m_joystick = new SharpDX.XInput.Controller(SharpDX.XInput.UserIndex.One);
            if (JoystickConnected != null)
            {
                JoystickConnected(m_joystick.IsConnected);
            }
            
        }
        

        public void ClearStates()
        {
            m_previousMouseState = m_actualMouseState;
            m_actualMouseState = new MyMouseState();
            m_actualMouseStateRaw.ClearPosition();
        }


        private Vector2 m_mouseMinsBound = new Vector2(0.0f, 0.0f);
        private Vector2 m_mouseMaxsBound = new Vector2(4000.0f, 4000.0f);
        private bool m_mouseLimitsInit = false;
        public void SetMouseLimits(Vector2 mins, Vector2 maxs)
        {
            m_mouseMinsBound = mins;
            m_mouseMaxsBound = maxs;

            if( !m_mouseLimitsInit )
            {
                m_mouseLimitsInit = true;
                m_absoluteMousePosition = (m_mouseMinsBound + m_mouseMaxsBound) * 0.5f;
            }
        }


        void UpdateStates()
        {
            ProfilerShort.Begin("MyXInputInput::UpdateStates");
            m_previousMouseState = m_actualMouseState;
            

            if (IsJoystickConnected())
            {
                m_previousJoystickState = m_actualJoystickState;
                m_actualJoystickState = XB1Interface.XB1Interface.GetGamepadState();

                // Always emulating mouse (only controllers here!)
                {
                    float lx = m_actualJoystickState.lx;
                    float ly = m_actualJoystickState.ly;

                    float magnitude = (float)Math.Sqrt(lx*lx + ly*ly);

                    // determine the direction the controller is pushed
                    float normalizedLX = lx / magnitude;
                    float normalizedLY = ly / magnitude;

                    float normalizedMagnitude = 0.0f;

                    // check if the controller is outside a circular dead zone
                    if (magnitude > m_joystickDeadzone)
                    {
                        if (magnitude > 1.0f) magnitude = 1.0f;
                        magnitude -= m_joystickDeadzone;
                        normalizedMagnitude = magnitude / (1.0f - m_joystickDeadzone);

                        // Use cubic magnitude to achieve better precision near the dead zone
                        float mult = normalizedMagnitude * normalizedMagnitude * normalizedMagnitude *
                        MyJoystickConstants.JOYSTICK_AS_MOUSE_MULTIPLIER * 2.0f;
                        m_absoluteMousePosition.X += mult * normalizedLX;
                        m_absoluteMousePosition.Y -= mult * normalizedLY;
                    }

                    m_absoluteMousePosition = Vector2.Clamp(m_absoluteMousePosition, m_mouseMinsBound, m_mouseMaxsBound);

                    
                    
                    
                }
            }

            if (IsJoystickLastUsed)
            {
                /*
                if (IsAnyMousePressed() || IsAnyKeyPress())
                    IsJoystickLastUsed = false;
                 * */
            }
            else
            {
                if (IsAnyJoystickButtonPressed() || IsAnyJoystickAxisPressed())
                    IsJoystickLastUsed = true;
            }

            
            ProfilerShort.End();
        }


        bool m_gameWasFocused = false;

        //  Update keyboard/mouse input and return true if application has focus (is active). Otherwise false.
        public bool Update(bool gameFocused)
        {
            ProfilerShort.Begin("MyXInputInput::Update");
            bool ret;

            if (!m_gameWasFocused && gameFocused)
            {
                //We call 2x Update states to not receive NewKeyPressed if user keeps pressed key while AltTabbing
                UpdateStates();
            }

            m_gameWasFocused = gameFocused;

            if (!gameFocused)
            {
                ClearStates();
                ProfilerShort.End();
                return false;
            }
            ProfilerShort.BeginNextBlock("MyXInputInput::Update2");

            UpdateStates();

            ret = true;
            ProfilerShort.BeginNextBlock("MyXInputInput::Update3");

            if (m_bufferedInputSource != null)
            {
                m_bufferedInputSource.SwapBufferedTextInput(ref m_currentTextInput);
            }
            
            ProfilerShort.End();
            return ret;
        }

        //  Return true if ANY key IS pressed, that means that the key was pressed now. During previous Update it wasn't pressed at all.
        public bool IsAnyKeyPress()
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        //  Return true if ANY NEW key IS pressed, that means that the key was pressed now. During previous Update it wasn't pressed at all.
        public bool IsAnyNewKeyPress()
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        //  Return true if ANY mouse key IS pressed.
        public bool IsAnyMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Not mouse support!");
#endif
            return false;
        }

        public bool IsAnyNewMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Not mouse support!");
#endif
            return false;
        }

        //  Check to see if any button is currently pressed on the joystick
        public bool IsAnyJoystickButtonPressed()
        {
            if (IsJoystickConnected())
            {
                bool dpad = IsGamepadKeyDownPressed() || IsGamepadKeyLeftPressed() || IsGamepadKeyRightPressed() || IsGamepadKeyUpPressed();
                if (dpad)
                    return true;

                if (m_actualJoystickState.Buttons != 0x0)
                    return true;
            }
            return false;
        }

        public bool IsAnyNewJoystickButtonPressed()
        {
            if (IsJoystickConnected())
            {
                if ( (m_actualJoystickState.Buttons != 0x0) &&
                    (m_actualJoystickState.Buttons != m_previousJoystickState.Buttons))
                    return true;
            }
            return false;
        }

        public bool IsNewGameControlJoystickOnlyPressed(MyStringId controlId)
        {
            MyControl gameControl;
            if (m_gameControlsList.TryGetValue(controlId, out gameControl))
            {
                return gameControl.IsNewJoystickPressed();
            }
            else
            {
                return false;
            }
        }

        public bool IsGameControlJoystickOnlyPressed(MyStringId controlId)
        {
            MyControl gameControl;
            if (m_gameControlsList.TryGetValue(controlId, out gameControl))
            {
                return gameControl.IsJoystickPressed();
            }
            else
            {
                return false;
            }
        }

        public bool IsNewGameControlJoystickOnlyReleased(MyStringId controlId)
        {
            MyControl gameControl;
            if (m_gameControlsList.TryGetValue(controlId, out gameControl))
            {
                return gameControl.IsNewJoystickReleased();
            }
            else
            {
                return false;
            }
        }

        private bool IsAnyJoystickAxisPressed()
        {
            if (IsJoystickConnected())
            {
                foreach (var axis in m_validJoystickAxes)
                {
                    if (axis != MyJoystickAxesEnum.None && IsJoystickAxisPressed(axis))
                        return true;
                }
            }
            return false;
        }

        public bool IsAnyMouseOrJoystickPressed()
        {
            return IsAnyJoystickButtonPressed();
        }

        public bool IsAnyNewMouseOrJoystickPressed()
        {
            return IsAnyNewJoystickButtonPressed();
        }

        public bool IsNewPrimaryButtonPressed()
        {
            return IsJoystickButtonNewPressed(MyJoystickButtonsEnum.J01);
        }

        public bool IsNewSecondaryButtonPressed()
        {
            return IsJoystickButtonNewPressed(MyJoystickButtonsEnum.J02);
        }

        public bool IsNewPrimaryButtonReleased()
        {
            return IsNewJoystickButtonReleased(MyJoystickButtonsEnum.J01);
        }

        public bool IsNewSecondaryButtonReleased()
        {
            return IsNewJoystickButtonReleased(MyJoystickButtonsEnum.J02);
        }

        public bool IsPrimaryButtonReleased()
        {
            return IsJoystickButtonReleased(MyJoystickButtonsEnum.J01);
        }

        public bool IsSecondaryButtonReleased()
        {
            return IsJoystickButtonReleased(MyJoystickButtonsEnum.J02);
        }

        public bool IsPrimaryButtonPressed()
        {
            return IsJoystickButtonPressed(MyJoystickButtonsEnum.J01);
        }

        public bool IsSecondaryButtonPressed()
        {
            return IsJoystickButtonPressed(MyJoystickButtonsEnum.J02);
        }

        public bool IsNewButtonPressed(MySharedButtonsEnum button)
        {
            switch (button)
            {
                case MySharedButtonsEnum.Primary:
                    return IsNewPrimaryButtonPressed();
                case MySharedButtonsEnum.Secondary:
                    return IsNewSecondaryButtonPressed();
            }
            return false;
        }

        public bool IsButtonPressed(MySharedButtonsEnum button)
        {
            switch (button)
            {
                case MySharedButtonsEnum.Primary:
                    return IsPrimaryButtonPressed();
                case MySharedButtonsEnum.Secondary:
                    return IsSecondaryButtonPressed();
            }
            return false;
        }

        public bool IsNewButtonReleased(MySharedButtonsEnum button)
        {
            switch (button)
            {
                case MySharedButtonsEnum.Primary:
                    return IsNewPrimaryButtonReleased();
                case MySharedButtonsEnum.Secondary:
                    return IsNewSecondaryButtonReleased();
            }
            return false;
        }

        public bool IsButtonReleased(MySharedButtonsEnum button)
        {
            switch (button)
            {
                case MySharedButtonsEnum.Primary:
                    return IsPrimaryButtonReleased();
                case MySharedButtonsEnum.Secondary:
                    return IsSecondaryButtonReleased();
            }
            return false;
        }




        public bool IsAnyWinKeyPressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        //True if any SHIFT key is pressed
        public bool IsAnyShiftKeyPressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        //True if any ALT key is pressed
        public bool IsAnyAltKeyPressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        //True if any CTRL key is pressed
        public bool IsAnyCtrlKeyPressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        //  Gets an array of values that correspond to the keyboard keys that are currently
        //  being pressed. Reference page contains links to related code samples.
        public void GetPressedKeys(List<MyKeys> keys)
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
        }

        #region Key Button States

        //  Return true if new key pressed right now. Don't care if it was pressed in previous update too.
        public bool IsKeyPress(MyKeys key)
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        //  Return true if new key was pressed, that means this key was pressed now. During previous Update it wasn't pressed at all.
        public bool IsNewKeyPressed(MyKeys key)
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        //  Return true if key was pressed in previous update and now it is not.
        public bool IsNewKeyReleased(MyKeys key)
        {
#if UNSHARPER
            Debug.Assert(false, "Not keyboard support!");
#endif
            return false;
        }

        #endregion

        #region Mouse Button States
        public bool IsMousePressed(MyMouseButtonsEnum button)
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool IsMouseReleased(MyMouseButtonsEnum button)
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasMousePressed(MyMouseButtonsEnum button)
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasMouseReleased(MyMouseButtonsEnum button)
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool IsNewMousePressed(MyMouseButtonsEnum button)
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool IsNewMouseReleased(MyMouseButtonsEnum button)
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        #endregion

        #region Left Mouse Button States

        //  True if LEFT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewLeftMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if LEFT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        public bool IsNewLeftMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if LEFT mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsLeftMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if LEFT mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsLeftMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasLeftMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if LEFT mouse was pressed in previous update.
        public bool WasLeftMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        #endregion

        #region Right Mouse Button states

        //  True if RIGHT mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsRightMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if RIGHT mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsRightMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if RIGHT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewRightMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if RIGHT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        public bool IsNewRightMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasRightMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasRightMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        #endregion

        #region Middle Mouse Button States

        //  True if MIDDLE mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsMiddleMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if MIDDLE mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsMiddleMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewMiddleMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewMiddleMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasMiddleMousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasMiddleMouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        #endregion

        #region XButton1 Mouse Button States

        //  True if XButton1 mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsXButton1MousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if XButton1 mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsXButton1MouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if XButton1 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewXButton1MousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool IsNewXButton1MouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasXButton1MousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasXButton1MouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        #endregion

        #region XButton2 Mouse Button States

        //  True if XButton2 mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsXButton2MousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  //True if XButton2 mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsXButton2MouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        //  True if XButton2 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewXButton2MousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool IsNewXButton2MouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasXButton2MousePressed()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        public bool WasXButton2MouseReleased()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return false;
        }

        #endregion

        #region Joystick button States
        
        //  Check to see if a specific button on the joystick is pressed.
        public bool IsJoystickButtonPressed(MyJoystickButtonsEnum button)
        {
            bool isPressed = false;
            if (IsJoystickConnected() && button != MyJoystickButtonsEnum.None)
            {
                switch (button)
                {
                    case MyJoystickButtonsEnum.J11:
                        isPressed = IsGamepadLTriggerPressed(m_actualJoystickState);
                        break;
                    case MyJoystickButtonsEnum.J12:
                        isPressed = IsGamepadRTriggerPressed(m_actualJoystickState);
                        break;
                    default: isPressed = (m_actualJoystickState.Buttons & JoystickHelper.VRageToXInput(button)) != 0x0; break;
                }
            }
            if (!isPressed && button == MyJoystickButtonsEnum.None)
            {
                return true;
            }
            return isPressed;
        }

        //  Check to see if a specific button on the joystick is currently pressed and was not pressed during the last update. 
        public bool IsJoystickButtonNewPressed(MyJoystickButtonsEnum button)
        {
            bool isNewPressed = false;
            //if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_actualJoystickState != null && m_previousJoystickState != null)
            if (IsJoystickConnected() && button != MyJoystickButtonsEnum.None) // && m_actualJoystickState != null && m_previousJoystickState != null)
            {
                uint flags = JoystickHelper.VRageToXInput(button);
                switch (button)
                {
                    case MyJoystickButtonsEnum.J11:
                        isNewPressed = IsGamepadLTriggerPressed(m_actualJoystickState) &&
                            (!IsGamepadLTriggerPressed(m_previousJoystickState));
                        break;
                    case MyJoystickButtonsEnum.J12:
                        isNewPressed = IsGamepadRTriggerPressed(m_actualJoystickState) &&
                            (!IsGamepadRTriggerPressed(m_previousJoystickState));
                        break;
                    default: isNewPressed = ((m_actualJoystickState.Buttons & flags) != 0x0) &&
                        ((m_previousJoystickState.Buttons & flags) == 0x0); break;
                }
            }
            if (!isNewPressed && button == MyJoystickButtonsEnum.None)
            {
                return true;
            }
            return isNewPressed;
        }

        public bool IsNewJoystickButtonReleased(MyJoystickButtonsEnum button)
        {
            bool isReleased = false;
            //if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_actualJoystickState != null && m_previousJoystickState != null)
            if (IsJoystickConnected() && button != MyJoystickButtonsEnum.None) // && m_actualJoystickState != null && m_previousJoystickState != null)
            {
                uint flags = JoystickHelper.VRageToXInput(button);
                switch (button)
                {
                    case MyJoystickButtonsEnum.J11:
                        isReleased = (!IsGamepadLTriggerPressed(m_actualJoystickState)) &&
                            IsGamepadLTriggerPressed(m_previousJoystickState);
                        break;
                    case MyJoystickButtonsEnum.J12:
                        isReleased = (!IsGamepadRTriggerPressed(m_actualJoystickState)) &&
                            IsGamepadRTriggerPressed(m_previousJoystickState);
                        break;
                    //default: isReleased = ((m_actualJoystickState.IsReleased((int)button - 5)) && (m_previousJoystickState.IsPressed((int)button - 5))); break;
                    default: isReleased = ((m_actualJoystickState.Buttons & flags) == 0x0) &&
                        ((m_previousJoystickState.Buttons & flags) != 0x0); break;
                }
            }
            if (!isReleased && button == MyJoystickButtonsEnum.None)
            {
                return true;
            }
            return isReleased;
        }

        public bool IsJoystickButtonReleased(MyJoystickButtonsEnum button)
        {
            bool isReleased = false;
            //if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_actualJoystickState != null)
            if (IsJoystickConnected() && button != MyJoystickButtonsEnum.None) // && m_actualJoystickState != null && m_previousJoystickState != null)
            {
                uint flags = JoystickHelper.VRageToXInput(button);
                switch (button)
                {
                    case MyJoystickButtonsEnum.J11:
                        isReleased = !IsGamepadLTriggerPressed(m_actualJoystickState);
                        break;
                    case MyJoystickButtonsEnum.J12:
                        isReleased = !IsGamepadRTriggerPressed(m_actualJoystickState);
                        break;
                    //default: isReleased = m_actualJoystickState.IsReleased((int)button - 5); break;
                    default: isReleased = (m_actualJoystickState.Buttons & flags) == 0x0; break;
                }
            }
            if (!isReleased && button == MyJoystickButtonsEnum.None)
            {
                return true;
            }
            return isReleased;
        }

        //  Check to see if a specific button on the joystick was pressed.
        public bool WasJoystickButtonPressed(MyJoystickButtonsEnum button)
        {
            bool wasPressed = false;
            //if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_previousJoystickState != null)
            if (IsJoystickConnected() && button != MyJoystickButtonsEnum.None)
            {
                uint flags = JoystickHelper.VRageToXInput(button);
                switch (button)
                {
                    case MyJoystickButtonsEnum.J11:
                        wasPressed = IsGamepadLTriggerPressed(m_previousJoystickState);
                        break;
                    case MyJoystickButtonsEnum.J12:
                        wasPressed = IsGamepadRTriggerPressed(m_previousJoystickState);
                        break;
                    //default: wasPressed = (m_previousJoystickState.Buttons[(int)button - 5]); break;
                    default: wasPressed = (m_previousJoystickState.Buttons & flags) != 0x0; break;
                }
            }
            if (!wasPressed && button == MyJoystickButtonsEnum.None)
            {
                return true;
            }
            return wasPressed;
        }

        //  Check to see if a specific button on the joystick was released.
        public bool WasJoystickButtonReleased(MyJoystickButtonsEnum button)
        {
            bool wasReleased = false;
            //if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_previousJoystickState != null)
            if (IsJoystickConnected() && button != MyJoystickButtonsEnum.None)
            {
                uint flags = JoystickHelper.VRageToXInput(button);
                switch (button)
                {
                    case MyJoystickButtonsEnum.J11:
                        wasReleased = !IsGamepadLTriggerPressed(m_previousJoystickState);
                        break;
                    case MyJoystickButtonsEnum.J12:
                        wasReleased = !IsGamepadRTriggerPressed(m_previousJoystickState);
                        break;
                    //default: wasReleased = (m_previousJoystickState.IsReleased((int)button - 5)); break;
                    default: wasReleased = (m_previousJoystickState.Buttons & flags) == 0x0; break;
                }
            }
            if (!wasReleased && button == MyJoystickButtonsEnum.None)
            {
                return true;
            }
            return wasReleased;
        }

        #endregion

        #region Joystick axis States

        private float ComputeJoystickAxisState(XB1Interface.XB1Interface.GamepadState padState, MyJoystickAxesEnum axis)
        {
            float value = 0.0f;
            if (IsJoystickConnected() && IsJoystickAxisSupported(axis))
            {
                
                switch (axis)
                {
                        
                    case MyJoystickAxesEnum.RotationXpos:
                    case MyJoystickAxesEnum.RotationXneg: value = padState.rx; break;
                    case MyJoystickAxesEnum.RotationYpos:
                    case MyJoystickAxesEnum.RotationYneg: value = -padState.ry; break;
                    case MyJoystickAxesEnum.Xpos:
                    case MyJoystickAxesEnum.Xneg: value = padState.lx; break;
                    case MyJoystickAxesEnum.Ypos:
                    case MyJoystickAxesEnum.Yneg: value = -padState.ly; break;

                }
                 
            }
            return value;
        }


        private float ComputeJoystickAxisStateForGameplay(XB1Interface.XB1Interface.GamepadState padState, MyJoystickAxesEnum axis)
        {
            if (IsJoystickConnected() && IsJoystickAxisSupported(axis))
            {
                // Input position scaled to (-1..1).
                float position = ComputeJoystickAxisState(padState, axis);
                float distance = (((int)axis) % 2 != 0) ? position : -position; // even -> neg axis
                if (distance > m_joystickDeadzone)
                {
                    distance = (distance - m_joystickDeadzone) / (1 - m_joystickDeadzone);  // Rescale distance to (0..1) outside the deadzone.
                    return m_joystickSensitivity * (float)Math.Pow(distance, m_joystickExponent);
                }
            }

            return 0.0f;
        }

        
        //  Find out how much a specific joystick half-axis is pressed.
        //  Return a number between 0 and 1 (taking deadzone, sensitivity and non-linearity into account).
        public float GetJoystickAxisStateForGameplay(MyJoystickAxesEnum axis)
        {
            return ComputeJoystickAxisStateForGameplay(m_actualJoystickState, axis);
        }

        //  Find out how much a specific joystick half-axis is pressed.
        //  Return a number between 0 and 1 (taking deadzone, sensitivity and non-linearity into account).
        public float GetPreviousJoystickAxisStateForGameplay(MyJoystickAxesEnum axis)
        {
            return ComputeJoystickAxisStateForGameplay(m_previousJoystickState, axis);
        }




        #region Joystick analog axes used for digital controls

        public bool IsJoystickAxisPressed(MyJoystickAxesEnum axis)
        {
            bool isPressed = false;
            //if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null)
            if (IsJoystickConnected() && axis != MyJoystickAxesEnum.None)
            {
                isPressed = GetJoystickAxisStateForGameplay(axis) > MyJoystickConstants.ANALOG_PRESSED_THRESHOLD;
            }
            if (!isPressed && axis == MyJoystickAxesEnum.None)
            {
                return true;
            }
            if (!IsJoystickAxisSupported(axis)) return false;
            return isPressed;
        }

        //  Check to see if a specific button on the joystick is currently pressed and was not pressed during the last update. 
        public bool IsJoystickAxisNewPressed(MyJoystickAxesEnum axis)
        {
            bool isNewPressed = false;
            //if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null && m_previousJoystickState != null)
            if( IsJoystickConnected() && axis != MyJoystickAxesEnum.None  )
            {
                float newState = GetJoystickAxisStateForGameplay(axis);
                float oldState = GetPreviousJoystickAxisStateForGameplay(axis);
                isNewPressed = newState > MyJoystickConstants.ANALOG_PRESSED_THRESHOLD && oldState <= MyJoystickConstants.ANALOG_PRESSED_THRESHOLD;
            }
            if (!isNewPressed && axis == MyJoystickAxesEnum.None)
            {
                return true;
            }
            if (!IsJoystickAxisSupported(axis)) return false;
            return isNewPressed;
        }

        public bool IsNewJoystickAxisReleased(MyJoystickAxesEnum axis)
        {
            bool isNewPressed = false;
            //if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null && m_previousJoystickState != null)
            if (IsJoystickConnected() && axis != MyJoystickAxesEnum.None)
            {
                isNewPressed = GetJoystickAxisStateForGameplay(axis) <= MyJoystickConstants.ANALOG_PRESSED_THRESHOLD && GetPreviousJoystickAxisStateForGameplay(axis) > MyJoystickConstants.ANALOG_PRESSED_THRESHOLD;
            }
            if (!isNewPressed && axis == MyJoystickAxesEnum.None)
            {
                return true;
            }
            if (!IsJoystickAxisSupported(axis)) return false;
            return isNewPressed;
        }

        public bool IsJoystickAxisReleased(MyJoystickAxesEnum axis)
        {
            bool isPressed = false;
            //if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null)
            if (IsJoystickConnected() && axis != MyJoystickAxesEnum.None)
            {
                isPressed = GetJoystickAxisStateForGameplay(axis) <= MyJoystickConstants.ANALOG_PRESSED_THRESHOLD;
            }
            if (!isPressed && axis == MyJoystickAxesEnum.None)
            {
                return true;
            }
            if (!IsJoystickAxisSupported(axis)) return false;
            return isPressed;
        }

        //  Check to see if a specific button on the joystick was pressed.
        public bool WasJoystickAxisPressed(MyJoystickAxesEnum axis)
        {
            bool isPressed = false;
            //if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_previousJoystickState != null)
            if (IsJoystickConnected() && axis != MyJoystickAxesEnum.None)
            {
                isPressed = GetPreviousJoystickAxisStateForGameplay(axis) > MyJoystickConstants.ANALOG_PRESSED_THRESHOLD;
            }
            if (!isPressed && axis == MyJoystickAxesEnum.None)
            {
                return true;
            }
            if (!IsJoystickAxisSupported(axis)) return false;
            return isPressed;
        }

        //  Check to see if a specific button on the joystick was released.
        public bool WasJoystickAxisReleased(MyJoystickAxesEnum axis)
        {
            bool isPressed = false;
            //if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_previousJoystickState != null)
            if (IsJoystickConnected() && axis != MyJoystickAxesEnum.None)
            {
                isPressed = GetPreviousJoystickAxisStateForGameplay(axis) <= MyJoystickConstants.ANALOG_PRESSED_THRESHOLD;
            }
            if (!isPressed && axis == MyJoystickAxesEnum.None)
            {
                return true;
            }
            if (!IsJoystickAxisSupported(axis)) return false;
            return isPressed;
        }

        #endregion

        #region Joystick settings

        public float GetJoystickSensitivity()
        {
            return m_joystickSensitivity;
        }

        public void SetJoystickSensitivity(float newSensitivity)
        {
            m_joystickSensitivity = newSensitivity;
        }

        public float GetJoystickExponent()
        {
            return m_joystickExponent;
        }

        public void SetJoystickExponent(float newExponent)
        {
            m_joystickExponent = newExponent;
        }

        public float GetJoystickDeadzone()
        {
            return m_joystickDeadzone;
        }

        public void SetJoystickDeadzone(float newDeadzone)
        {
            m_joystickDeadzone = newDeadzone;
        }

        #endregion

        #endregion

        //  Current mouse scrollwheel value.
        public int MouseScrollWheelValue()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return 0;
        }

        //  Previous mouse scrollwheel value.
        public int PreviousMouseScrollWheelValue()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return 0;
        }

        //  Delta mouse scrollwheel value.
        public int DeltaMouseScrollWheelValue()
        {
#if UNSHARPER
            Debug.Assert(false, "Mouse Support Not Implemented!");
#endif
            return 0;
        }

        //  Return actual mouse X position - for drawing cursor
        public int GetMouseX()
        {
            return m_actualMouseState.X;
        }

        //  Return actual mouse Y position - for drawing cursor
        public int GetMouseY()
        {
            return m_actualMouseState.Y;
        }

        //  Return actual mouse X position - for gameplay
        public int GetMouseXForGamePlay()
        {
            int inv = m_mouseXIsInverted ? -1 : 1;
            return (int)(m_mouseSensitivity * (inv * (m_actualMouseState.X)));
        }

        //  Return actual mouse Y position - for gameplay
        public int GetMouseYForGamePlay()
        {
            int inv = m_mouseYIsInverted ? -1 : 1;
            return (int)(m_mouseSensitivity * (inv * (m_actualMouseState.Y)));
        }

        public bool GetMouseXInversion()
        {
            return m_mouseXIsInverted;
        }

        public bool GetMouseYInversion()
        {
            return m_mouseYIsInverted;
        }

        public void SetMouseXInversion(bool inverted)
        {
            m_mouseXIsInverted = inverted;
        }

        public void SetMouseYInversion(bool inverted)
        {
            m_mouseYIsInverted = inverted;
        }

        public float GetMouseSensitivity()
        {
            return m_mouseSensitivity;
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            m_mouseSensitivity = sensitivity;
        }

        /// <summary>
        /// Returns immediatelly current cursor position.
        /// Obtains position on every call, it can get cursor data with higher rate than 60 fps
        /// </summary>
        public Vector2 GetMousePosition()
        {
            return m_absoluteMousePosition;
        }

        public Vector2 GetMouseAreaSize()
        {
            return m_bufferedInputSource.MouseAreaSize;
        }

        public void SetMousePosition(int x, int y)
        {
            //MyWindowsMouse.SetPosition(x, y);
            m_absoluteMousePosition = new Vector2(x, y);

            //MyTrace.Send(TraceWindow.Default, "MyWindowsMouse.SetPosition: " + x.ToString() + " " + y.ToString());
        }

        // Checks to see if the joystick is connected. This is used so we don't try to poll a joystick that doesn't exist.
        public bool IsJoystickConnected()
        {
            return m_joystick.IsConnected;
        }

        public void SetJoystickConnected(bool value)
        {
            Debug.Assert(false);
        }

        public bool JoystickAsMouse
        {
            get;
            set;
        }

        public bool IsJoystickLastUsed
        {
            get;
            set;
        }

        public event Action<bool> JoystickConnected;



        public bool IsGamepadKeyRightPressed()
        {
            return (m_actualJoystickState.Buttons & (uint)(JoystickHelper.XInputGamepadButtonFlags.DPadRight)) != 0x0;
        }

        public bool IsGamepadKeyLeftPressed()
        {
            return (m_actualJoystickState.Buttons & (uint)(JoystickHelper.XInputGamepadButtonFlags.DPadLeft)) != 0x0;
        }

        public bool IsGamepadKeyDownPressed()
        {
            return (m_actualJoystickState.Buttons & (uint)(JoystickHelper.XInputGamepadButtonFlags.DPadDown)) != 0x0;
        }

        public bool IsGamepadKeyUpPressed()
        {
            return (m_actualJoystickState.Buttons & (uint)(JoystickHelper.XInputGamepadButtonFlags.DPadUp)) != 0x0;
        }

        public bool WasGamepadKeyRightPressed()
        {
            return (m_previousJoystickState.Buttons & (uint)(JoystickHelper.XInputGamepadButtonFlags.DPadRight)) != 0x0;
        }

        public bool WasGamepadKeyLeftPressed()
        {
            return (m_previousJoystickState.Buttons & (uint)(JoystickHelper.XInputGamepadButtonFlags.DPadLeft)) != 0x0;
        }

        public bool WasGamepadKeyDownPressed()
        {
            return (m_previousJoystickState.Buttons & (uint)(JoystickHelper.XInputGamepadButtonFlags.DPadDown)) != 0x0;
        }

        public bool WasGamepadKeyUpPressed()
        {
            return (m_previousJoystickState.Buttons & (uint)(JoystickHelper.XInputGamepadButtonFlags.DPadUp)) != 0x0;
        }


        public bool IsNewGamepadKeyRightPressed() { return !WasGamepadKeyRightPressed() && IsGamepadKeyRightPressed(); }
        public bool IsNewGamepadKeyLeftPressed() { return !WasGamepadKeyLeftPressed() && IsGamepadKeyLeftPressed(); }
        public bool IsNewGamepadKeyDownPressed() { return !WasGamepadKeyDownPressed() && IsGamepadKeyDownPressed(); }
        public bool IsNewGamepadKeyUpPressed() { return !WasGamepadKeyUpPressed() && IsGamepadKeyUpPressed(); }

        public bool IsNewGamepadKeyRightReleased() { return WasGamepadKeyRightPressed() && !IsGamepadKeyRightPressed(); }
        public bool IsNewGamepadKeyLeftReleased() { return WasGamepadKeyLeftPressed() && !IsGamepadKeyLeftPressed(); }
        public bool IsNewGamepadKeyDownReleased() { return WasGamepadKeyDownPressed() && !IsGamepadKeyDownPressed(); }
        public bool IsNewGamepadKeyUpReleased() { return WasGamepadKeyUpPressed() && !IsGamepadKeyUpPressed(); }

        public void GetActualJoystickState(StringBuilder text)
        {
            if (m_joystick.IsConnected == false)
            {
                text.Append("No joystick detected.");
                return;
            }


            XB1Interface.XB1Interface.GamepadState gamepad = m_actualJoystickState;

            

            text.Append("Supported axes: ");
            if (IsJoystickAxisSupported(MyJoystickAxesEnum.Xpos)) text.Append("X ");
            if (IsJoystickAxisSupported(MyJoystickAxesEnum.Ypos)) text.Append("Y ");
            if (IsJoystickAxisSupported(MyJoystickAxesEnum.Zpos)) text.Append("Z ");
            if (IsJoystickAxisSupported(MyJoystickAxesEnum.RotationXpos)) text.Append("Rx ");
            if (IsJoystickAxisSupported(MyJoystickAxesEnum.RotationYpos)) text.Append("Ry ");
            if (IsJoystickAxisSupported(MyJoystickAxesEnum.RotationZpos)) text.Append("Rz ");
            if (IsJoystickAxisSupported(MyJoystickAxesEnum.Slider1pos)) text.Append("S1 ");
            if (IsJoystickAxisSupported(MyJoystickAxesEnum.Slider2pos)) text.Append("S2 ");
            text.AppendLine();

            text.Append("rotX: "); text.AppendDecimal(gamepad.rx, 4); text.AppendLine();
            text.Append("rotY: "); text.AppendDecimal(gamepad.ry, 4); text.AppendLine();
            text.Append("X: "); text.AppendDecimal(gamepad.lx, 4); text.AppendLine();
            text.Append("Y: "); text.AppendDecimal(gamepad.ly, 4); text.AppendLine();
            text.AppendLine();
            text.Append("Buttons: ");
            foreach (JoystickHelper.XInputGamepadButtonFlags i in Enum.GetValues(typeof(JoystickHelper.XInputGamepadButtonFlags)))
            {
                text.Append( (( (uint)(i) & gamepad.Buttons) != 0) ? "#" : "_");
                text.Append(" ");
            } text.AppendLine();
            
        }



        public bool IsJoystickAxisSupported(MyJoystickAxesEnum axis)
        {
            if (m_joystick.IsConnected == false)
            {
                return false;
            }
            
            switch (axis)
            {
                case MyJoystickAxesEnum.RotationZpos:
                case MyJoystickAxesEnum.RotationZneg:
                case MyJoystickAxesEnum.Zneg:
                case MyJoystickAxesEnum.Slider1pos:
                case MyJoystickAxesEnum.Slider1neg:
                case MyJoystickAxesEnum.Slider2pos:
                case MyJoystickAxesEnum.Slider2neg:
                case MyJoystickAxesEnum.None:
                    return false;
                default:
                    return true;
            }
        }

        private bool IsGamepadRTriggerPressed(XB1Interface.XB1Interface.GamepadState pad, float threshold = 0.2f)
        {
            return pad.rt >= threshold;
        }

        private bool IsGamepadLTriggerPressed(XB1Interface.XB1Interface.GamepadState pad, float threshold = 0.2f)
        {
            return pad.lt >= threshold;
        }

        private MyStringId m_SPRINT_SID = MyStringId.GetOrCompute("SPRINT");
        //  Check if an assigned control for game is new pressed.
        public bool IsNewGameControlPressed(MyStringId controlId)
        {
            //if (controlId == m_SPRINT_SID)
            //{
            //    return IsGamepadRTriggerPressed(m_actualJoystickState.Gamepad) &&
            //        (!(IsGamepadRTriggerPressed(m_previousJoystickState.Gamepad)));
            //}

            //return false;

            //  If you are trying to set a control that does not exist do nothing.
            MyControl control;
            if (m_gameControlsList.TryGetValue(controlId, out control))
                return control.IsNewPressed();
            else
                return false;
        }


        //  Check if an assigned control for game is currently pressed.
        public bool IsGameControlPressed(MyStringId controlId)
        {
            //if (controlId == m_SPRINT_SID)
            //{
            //    return IsGamepadRTriggerPressed(m_actualJoystickState.Gamepad);
            //}

            //return false;

            //  If you are trying to set a control that does not exist do nothing.
            MyControl control;
            if (m_gameControlsList.TryGetValue(controlId, out control))
                return control.IsPressed();
            else
                return false;
        }

        //  Check if an assigned control for game is new pressed.
        public bool IsNewGameControlReleased(MyStringId controlId)
        {
            //if (controlId == m_SPRINT_SID)
            //{
            //    return (!IsGamepadRTriggerPressed(m_actualJoystickState.Gamepad)) &&
            //        (IsGamepadRTriggerPressed(m_previousJoystickState.Gamepad));
            //}

            //return false;

            //  If you are trying to set a control that does not exist do nothing.
            MyControl control;
            if (m_gameControlsList.TryGetValue(controlId, out control))
                return control.IsNewReleased();
            else
                return false;
        }

        //  Check if an assigned control for game is currently pressed.
        public float GetGameControlAnalogState(MyStringId controlId)
        {
            //  If you are trying to set a control that does not exist do nothing.
            MyControl control;
            if (m_gameControlsList.TryGetValue(controlId, out control))
                return control.GetAnalogState();
            else
                return 0f;
        }

        //  Check is an assigned game control is released
        public bool IsGameControlReleased(MyStringId controlId)
        {
            //if (controlId == m_SPRINT_SID)
            //{
            //    return !IsGamepadRTriggerPressed(m_actualJoystickState.Gamepad);
            //}

            //return false;

            //  If you are trying to set a control that does not exist do nothing.
            MyControl control;
            if (m_gameControlsList.TryGetValue(controlId, out control))
                return control.IsNewReleased();
            else
                return false;
        }


        //  Return true if key is valid for user controls
        public bool IsKeyValid(MyKeys key)
        {
            Debug.Assert(false, "Not keyboard support!");
            return false;
        }

        public bool IsKeyDigit(MyKeys key)
        {
            Debug.Assert(false, "Not keyboard support!");
            return false;
            
        }

        //  Return true if mouse button is valid for user controls
        public bool IsMouseButtonValid(MyMouseButtonsEnum button)
        {
            foreach (var item in m_validMouseButtons)
            {
                if (item == button) return true;
            }
            return false;
        }

        //  Return true if joystick button is valid for user controls
        public bool IsJoystickButtonValid(MyJoystickButtonsEnum button)
        {
            foreach (var item in m_validJoystickButtons)
            {
                if (item == button) return true;
            }
            return false;
        }

        //  Return true if joystick axis is valid for user controls
        public bool IsJoystickAxisValid(MyJoystickAxesEnum axis)
        {
            foreach (var item in m_validJoystickAxes)
            {
                if (item == axis) return true;
            }
            return false;
        }

        //  Return true if key is used by some user control
        public MyControl GetControl(MyKeys key)
        {
            foreach (var item in m_gameControlsList.Values)
            {
                if (item.GetKeyboardControl() == key ||
                    item.GetSecondKeyboardControl() == key) return item;
            }
            return null;
        }

        //  Return true if mouse button is used by some user control
        public MyControl GetControl(MyMouseButtonsEnum button)
        {
            foreach (var item in m_gameControlsList.Values)
            {
                if (item.GetMouseControl() == button) return item;
            }
            return null;
        }

        public void GetListOfPressedKeys(List<MyKeys> keys)
        {
            GetPressedKeys(keys);
        }

        public void GetListOfPressedMouseButtons(List<MyMouseButtonsEnum> result)
        {
            result.Clear();

            if (IsLeftMousePressed()) result.Add(MyMouseButtonsEnum.Left);
            if (IsRightMousePressed()) result.Add(MyMouseButtonsEnum.Right);
            if (IsMiddleMousePressed()) result.Add(MyMouseButtonsEnum.Middle);
            if (IsXButton1MousePressed()) result.Add(MyMouseButtonsEnum.XButton1);
            if (IsXButton2MousePressed()) result.Add(MyMouseButtonsEnum.XButton2);
        }

        //  Returns an array MyControl that contains every assigned control for game.
        public DictionaryValuesReader<MyStringId, MyControl> GetGameControlsList()
        {
            return new DictionaryValuesReader<MyStringId, MyControl>(m_gameControlsList);
        }

        //  IMPORTANT! Use this function before attempting to assign new controls so that the controls can be re-set if the user does not like the changes.
        public void TakeSnapshot()
        {
            m_joystickInstanceNameSnapshot = JoystickInstanceName;
            CloneControls(m_gameControlsList, m_gameControlsSnapshot);
        }

        //  IMPORTANT! Only call this method after calling TakeSnapshot() to revert any changes made since TakeSnapshot() was last called. 
        public void RevertChanges()
        {
            JoystickInstanceName = m_joystickInstanceNameSnapshot;
            CloneControls(m_gameControlsSnapshot, m_gameControlsList);
        }

        //  Returns a string value of the button or key assigned to a control for game.
        public String GetGameControlTextEnum(MyStringId controlId)
        {
            return m_gameControlsList[controlId].GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
        }

        public MyControl GetGameControl(MyStringId controlId)
        {
            MyControl result;
            m_gameControlsList.TryGetValue(controlId, out result);
            return result;
        }


        //  This is used to copy the list of controls into backup lists or for reverting changes to the list
        private void CloneControls(Dictionary<MyStringId, MyControl> original, Dictionary<MyStringId, MyControl> copy)
        {
            if (original == null)
            {
                return;
            }

            foreach (var entry in original)
            {
                MyControl control;
                if (copy.TryGetValue(entry.Key, out control))
                    control.CopyFrom(entry.Value);
                else
                    copy[entry.Key] = new MyControl(entry.Value);
            }
        }

        public void RevertToDefaultControls()
        {
            m_mouseXIsInverted = IsMouseXInvertedDefault;
            m_mouseYIsInverted = IsMouseYInvertedDefault;
            m_mouseSensitivity = MouseSensitivityDefault;

            m_joystickSensitivity = JoystickSensitivityDefault;
            m_joystickDeadzone = JoystickDeadzoneDefault;
            m_joystickExponent = JoystickExponentDefault;
            CloneControls(m_defaultGameControlsList, m_gameControlsList);
        }

        //  Save all controls to the Config File.
        public void SaveControls(SerializableDictionary<string, object> controlsGeneral, SerializableDictionary<string, object> controlsButtons)
        {
            //SerializableDictionary<string, string> controlsData = new SerializableDictionary<string, string>();
            controlsGeneral.Dictionary.Clear();
            controlsGeneral.Dictionary.Add("mouseXIsInverted", m_mouseXIsInverted.ToString());
            controlsGeneral.Dictionary.Add("mouseYIsInverted", m_mouseYIsInverted.ToString());
            controlsGeneral.Dictionary.Add("mouseSensitivity", m_mouseSensitivity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            controlsGeneral.Dictionary.Add("joystickInstanceName", m_joystickInstanceName);
            controlsGeneral.Dictionary.Add("joystickSensitivity", m_joystickSensitivity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            controlsGeneral.Dictionary.Add("joystickExponent", m_joystickExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
            controlsGeneral.Dictionary.Add("joystickDeadzone", m_joystickDeadzone.ToString(System.Globalization.CultureInfo.InvariantCulture));

        }

        private bool LoadControls(SerializableDictionary<string, object> controlsGeneral, SerializableDictionary<string, object> controlsButtons)
        {
            if (controlsGeneral == null || controlsGeneral.Dictionary.Count == 0)
            {
                MyLog.Default.WriteLine("    Loading default controls");
                RevertToDefaultControls();
                return false;
            }

            try
            {
                m_mouseXIsInverted = bool.Parse((string)controlsGeneral["mouseXIsInverted"]);
                m_mouseYIsInverted = bool.Parse((string)controlsGeneral["mouseYIsInverted"]);
                m_mouseSensitivity = float.Parse((string)controlsGeneral["mouseSensitivity"], System.Globalization.CultureInfo.InvariantCulture);

                JoystickInstanceName = (string)controlsGeneral["joystickInstanceName"];

                m_joystickSensitivity = float.Parse((string)controlsGeneral["joystickSensitivity"], System.Globalization.CultureInfo.InvariantCulture);
                m_joystickExponent = float.Parse((string)controlsGeneral["joystickExponent"], System.Globalization.CultureInfo.InvariantCulture);
                m_joystickDeadzone = float.Parse((string)controlsGeneral["joystickDeadzone"], System.Globalization.CultureInfo.InvariantCulture);

                //  Load buttons and keys
                //LoadGameControls(controlsButtons);

                return true;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("    Error loading controls from config:");
                MyLog.Default.WriteLine(e);
                MyLog.Default.WriteLine("    Loading default controls");
                RevertToDefaultControls();
                return false;
            }
        }

        private void LoadGameControls(SerializableDictionary<string, object> controlsButtons)
        {
            return;
        }

        private void LoadGameControl(string controlName, MyStringId controlType, MyGuiInputDeviceEnum device)
        {
            switch (device)
            {
                case MyGuiInputDeviceEnum.Keyboard:
                    {
                        MyKeys key = (MyKeys)Enum.Parse(typeof(MyKeys), controlName);
                        if (!IsKeyValid(key))
                        {
                            throw new Exception("Key \"" + key.ToString() + "\" is already assigned or is not valid.");
                        }
                        FindNotAssignedGameControl(controlType, device).SetControl(MyGuiInputDeviceEnum.Keyboard, key);
                    }
                    break;
                case MyGuiInputDeviceEnum.KeyboardSecond:
                    {
                        MyKeys key = (MyKeys)Enum.Parse(typeof(MyKeys), controlName);
                        if (!IsKeyValid(key))
                        {
                            throw new Exception("Key \"" + key.ToString() + "\" is already assigned or is not valid.");
                        }
                        FindNotAssignedGameControl(controlType, device).SetControl(MyGuiInputDeviceEnum.KeyboardSecond, key);
                    }
                    break;
                case MyGuiInputDeviceEnum.Mouse:
                    MyMouseButtonsEnum mouse = ParseMyMouseButtonsEnum(controlName);
                    if (!IsMouseButtonValid(mouse))
                    {
                        throw new Exception("Mouse button \"" + mouse.ToString() + "\" is already assigned or is not valid.");
                    }
                    FindNotAssignedGameControl(controlType, device).SetControl(mouse);
                    break;
                case MyGuiInputDeviceEnum.None:
                    break;
                default:
                    break;
            }
        }


        public MyGuiInputDeviceEnum ParseMyGuiInputDeviceEnum(string s)
        {
            for (int i = 0; i < MyEnumsToStrings.GuiInputDeviceEnum.Length; i++)
            {
                if (MyEnumsToStrings.GuiInputDeviceEnum[i] == s) return (MyGuiInputDeviceEnum)i;
            }
            throw new ArgumentException("Value \"" + s + "\" is not from GuiInputDeviceEnum.", "s");
        }

        public MyJoystickButtonsEnum ParseMyJoystickButtonsEnum(string s)
        {
            for (int i = 0; i < MyEnumsToStrings.JoystickButtonsEnum.Length; i++)
            {
                if (MyEnumsToStrings.JoystickButtonsEnum[i] == s) return (MyJoystickButtonsEnum)i;
            }
            throw new ArgumentException("Value \"" + s + "\" is not from JoystickButtonsEnum.", "s");
        }

        public MyJoystickAxesEnum ParseMyJoystickAxesEnum(string s)
        {
            for (int i = 0; i < MyEnumsToStrings.JoystickAxesEnum.Length; i++)
            {
                if (MyEnumsToStrings.JoystickAxesEnum[i] == s) return (MyJoystickAxesEnum)i;
            }
            throw new ArgumentException("Value \"" + s + "\" is not from JoystickAxesEnum.", "s");
        }

        public MyMouseButtonsEnum ParseMyMouseButtonsEnum(string s)
        {
            for (int i = 0; i < MyEnumsToStrings.MouseButtonsEnum.Length; i++)
            {
                if (MyEnumsToStrings.MouseButtonsEnum[i] == s) return (MyMouseButtonsEnum)i;
            }
            throw new ArgumentException("Value \"" + s + "\" is not from MouseButtonsEnum.", "s");
        }

        public MyStringId? TryParseMyGameControlEnums(string s)
        {
            MyStringId id = MyStringId.GetOrCompute(s);
            if (m_gameControlsList.ContainsKey(id))
                return id;
            else
                return null;
        }

        public MyGuiControlTypeEnum ParseMyGuiControlTypeEnum(string s)
        {
            for (int i = 0; i < MyEnumsToStrings.ControlTypeEnum.Length; i++)
            {
                if (MyEnumsToStrings.ControlTypeEnum[i] == s) return (MyGuiControlTypeEnum)i;
            }
            throw new ArgumentException("Value \"" + s + "\" is not from MyGuiInputTypeEnum.", "s");
        }

        private MyControl FindNotAssignedGameControl(MyStringId controlId, MyGuiInputDeviceEnum deviceType)
        {
            MyControl control;
            if (!m_gameControlsList.TryGetValue(controlId, out control))
                throw new Exception("Game control \"" + controlId.ToString() + "\" not found in control list.");

            if (control.IsControlAssigned(deviceType))
                throw new Exception("Game control \"" + controlId.ToString() + "\" is already assigned.");

            return control;
        }


        public bool ENABLE_DEVELOPER_KEYS
        {
            get;
            private set;
        }


        public string GetKeyName(MyStringId controlId)
        {
            return GetGameControl(controlId).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
        }

        #region Functionality of the old PrimaryController

        public bool Trichording { get; set; }

        #endregion

        public string GetKeyName(MyKeys key)
        {
            return m_nameLookup.GetKeyName(key);
        }

        public string GetName(MyMouseButtonsEnum mouseButton)
        {
            return m_nameLookup.GetName(mouseButton);
        }

        public string GetName(MyJoystickButtonsEnum joystickButton)
        {
            return m_nameLookup.GetName(joystickButton);
        }

        public string GetName(MyJoystickAxesEnum joystickAxis)
        {
            return m_nameLookup.GetName(joystickAxis);
        }

        public string GetUnassignedName()
        {
            return m_nameLookup.UnassignedText;
        }
    }
}

#endif