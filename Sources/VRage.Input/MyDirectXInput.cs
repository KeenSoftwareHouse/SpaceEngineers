#if !XB1
#region Using

using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using VRage.Collections;
using VRage.Cryptography;
using VRage.Library.Utils;
using VRage.Serialization;
using VRage.Utils;
using VRage.Win32;
using VRageMath;
using VRage.OpenVRWrapper;
using VRage.Profiler;
using VRageRender.ExternalApp;

#endregion


namespace VRage.Input
{
    static class JoystickExtensions
    {
        public static bool IsPressed(this JoystickState state, int button)
        {
            return state.Buttons[button];
        }

        public static bool IsReleased(this JoystickState state, int button)
        {
            return !IsPressed(state, button);
        }
    }

    public partial class MyDirectXInput : IMyInput
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        // An umanaged function that retrieves the states of each key
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        static extern short GetKeyState(int keyCode);

        public bool IsCapsLock { get { return (((ushort)GetKeyState(0x14)) & 0xffff) != 0; } }
        public bool IsNumLock { get { return (((ushort)GetKeyState(0x90)) & 0xffff) != 0; } }
        public bool IsScrollLock { get { return (((ushort)GetKeyState(0x91)) & 0xffff) != 0; } }

        //Added by Gregory in order to Override some update properties fot testing Tool
        internal bool OverrideUpdate = false;

        Vector2 m_absoluteMousePosition;

        //  State Variables
        MyMouseState m_previousMouseState;
        JoystickState m_previousJoystickState;
        MyGuiLocalizedKeyboardState m_keyboardState;
        MyMouseState m_actualMouseState;
        public MyMouseState ActualMouseState
        {
            get {return m_actualMouseState;}
        }
        MyMouseState m_actualMouseStateRaw;
        JoystickState m_actualJoystickState;
        public JoystickState ActualJoystickState
        {
            get { return m_actualJoystickState; }
        }
        bool m_joystickXAxisSupported;
        bool m_joystickYAxisSupported;
        bool m_joystickZAxisSupported;
        bool m_joystickRotationXAxisSupported;
        bool m_joystickRotationYAxisSupported;
        bool m_joystickRotationZAxisSupported;
        bool m_joystickSlider1AxisSupported;
        bool m_joystickSlider2AxisSupported;

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

        MyKeyHasher m_hasher = new MyKeyHasher();

        //  Control lists
        Dictionary<MyStringId, MyControl> m_defaultGameControlsList;
        Dictionary<MyStringId, MyControl> m_gameControlsList = new Dictionary<MyStringId, MyControl>(MyStringId.Comparer);
        Dictionary<MyStringId, MyControl> m_gameControlsSnapshot = new Dictionary<MyStringId, MyControl>(MyStringId.Comparer);
        HashSet<MyStringId>               m_gameControlsBlacklist = new HashSet<MyStringId>();

            //  Lists of valid keys and buttons
        List<MyKeys> m_validKeyboardKeys = new List<MyKeys>();
        List<MyJoystickButtonsEnum> m_validJoystickButtons = new List<MyJoystickButtonsEnum>();
        List<MyJoystickAxesEnum> m_validJoystickAxes = new List<MyJoystickAxesEnum>();
        List<MyMouseButtonsEnum> m_validMouseButtons = new List<MyMouseButtonsEnum>();

        List<MyKeys> m_digitKeys = new List<MyKeys>();

        Array m_allKeys = Enum.GetValues(typeof(MyKeys));

        //  Joystick variables
        Device m_joystick = null;
        DeviceType? m_joystickType = null;
        bool m_joystickConnected = false;

        //Declare the hook handle as an int.
        // IntPtr m_hHook = IntPtr.Zero;
        // WinApi.HookProc m_hookHandler;

        IMyBufferedInputSource m_bufferedInputSource;
        IMyControlNameLookup m_nameLookup;
        List<char> m_currentTextInput = new List<char>();
        List<MyKeys> m_tmpPressedKeys = new List<MyKeys>(10);


        public MyDirectXInput(
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

        public void AddDefaultControl(MyStringId stringId, MyControl control)
        {
            m_gameControlsList[stringId] = control;
            m_defaultGameControlsList[stringId] = control;
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

            #region Digit keys only

            m_digitKeys.Add(MyKeys.D0);
            m_digitKeys.Add(MyKeys.D1);
            m_digitKeys.Add(MyKeys.D2);
            m_digitKeys.Add(MyKeys.D3);
            m_digitKeys.Add(MyKeys.D4);
            m_digitKeys.Add(MyKeys.D5);
            m_digitKeys.Add(MyKeys.D6);
            m_digitKeys.Add(MyKeys.D7);
            m_digitKeys.Add(MyKeys.D8);
            m_digitKeys.Add(MyKeys.D9);
            m_digitKeys.Add(MyKeys.NumPad0);
            m_digitKeys.Add(MyKeys.NumPad1);
            m_digitKeys.Add(MyKeys.NumPad2);
            m_digitKeys.Add(MyKeys.NumPad3);
            m_digitKeys.Add(MyKeys.NumPad4);
            m_digitKeys.Add(MyKeys.NumPad5);
            m_digitKeys.Add(MyKeys.NumPad6);
            m_digitKeys.Add(MyKeys.NumPad7);
            m_digitKeys.Add(MyKeys.NumPad8);
            m_digitKeys.Add(MyKeys.NumPad9);

            #endregion

            #region Lists of assignable keys and buttons

            //  List of assignable keyboard keys
            m_validKeyboardKeys.Add(MyKeys.A);
            m_validKeyboardKeys.Add(MyKeys.Add);
            m_validKeyboardKeys.Add(MyKeys.B);
            m_validKeyboardKeys.Add(MyKeys.Back);
            m_validKeyboardKeys.Add(MyKeys.C);
            m_validKeyboardKeys.Add(MyKeys.CapsLock);
            m_validKeyboardKeys.Add(MyKeys.D);
            m_validKeyboardKeys.Add(MyKeys.D0);
            m_validKeyboardKeys.Add(MyKeys.D1);
            m_validKeyboardKeys.Add(MyKeys.D2);
            m_validKeyboardKeys.Add(MyKeys.D3);
            m_validKeyboardKeys.Add(MyKeys.D4);
            m_validKeyboardKeys.Add(MyKeys.D5);
            m_validKeyboardKeys.Add(MyKeys.D6);
            m_validKeyboardKeys.Add(MyKeys.D7);
            m_validKeyboardKeys.Add(MyKeys.D8);
            m_validKeyboardKeys.Add(MyKeys.D9);
            m_validKeyboardKeys.Add(MyKeys.Decimal);
            m_validKeyboardKeys.Add(MyKeys.Delete);
            m_validKeyboardKeys.Add(MyKeys.Divide);
            m_validKeyboardKeys.Add(MyKeys.Down);
            m_validKeyboardKeys.Add(MyKeys.E);
            m_validKeyboardKeys.Add(MyKeys.End);
            m_validKeyboardKeys.Add(MyKeys.Enter);
            m_validKeyboardKeys.Add(MyKeys.F);
            m_validKeyboardKeys.Add(MyKeys.G);
            m_validKeyboardKeys.Add(MyKeys.H);
            m_validKeyboardKeys.Add(MyKeys.Home);
            m_validKeyboardKeys.Add(MyKeys.I);
            m_validKeyboardKeys.Add(MyKeys.Insert);
            m_validKeyboardKeys.Add(MyKeys.J);
            m_validKeyboardKeys.Add(MyKeys.K);
            m_validKeyboardKeys.Add(MyKeys.L);
            m_validKeyboardKeys.Add(MyKeys.Left);
            m_validKeyboardKeys.Add(MyKeys.LeftAlt);
            m_validKeyboardKeys.Add(MyKeys.LeftControl);
            m_validKeyboardKeys.Add(MyKeys.LeftShift);
            m_validKeyboardKeys.Add(MyKeys.M);
            m_validKeyboardKeys.Add(MyKeys.Multiply);
            m_validKeyboardKeys.Add(MyKeys.N);
            m_validKeyboardKeys.Add(MyKeys.None);
            m_validKeyboardKeys.Add(MyKeys.NumPad0);
            m_validKeyboardKeys.Add(MyKeys.NumPad1);
            m_validKeyboardKeys.Add(MyKeys.NumPad2);
            m_validKeyboardKeys.Add(MyKeys.NumPad3);
            m_validKeyboardKeys.Add(MyKeys.NumPad4);
            m_validKeyboardKeys.Add(MyKeys.NumPad5);
            m_validKeyboardKeys.Add(MyKeys.NumPad6);
            m_validKeyboardKeys.Add(MyKeys.NumPad7);
            m_validKeyboardKeys.Add(MyKeys.NumPad8);
            m_validKeyboardKeys.Add(MyKeys.NumPad9);
            m_validKeyboardKeys.Add(MyKeys.O);
            m_validKeyboardKeys.Add(MyKeys.OemCloseBrackets);
            m_validKeyboardKeys.Add(MyKeys.OemComma);
            m_validKeyboardKeys.Add(MyKeys.OemMinus);
            m_validKeyboardKeys.Add(MyKeys.OemOpenBrackets);
            m_validKeyboardKeys.Add(MyKeys.OemPeriod);
            m_validKeyboardKeys.Add(MyKeys.OemPipe);
            m_validKeyboardKeys.Add(MyKeys.OemPlus);
            m_validKeyboardKeys.Add(MyKeys.OemQuestion);
            m_validKeyboardKeys.Add(MyKeys.OemQuotes);
            m_validKeyboardKeys.Add(MyKeys.OemSemicolon);
            m_validKeyboardKeys.Add(MyKeys.OemTilde);
            m_validKeyboardKeys.Add(MyKeys.OemBackslash);
            m_validKeyboardKeys.Add(MyKeys.P);
            m_validKeyboardKeys.Add(MyKeys.PageDown);
            m_validKeyboardKeys.Add(MyKeys.PageUp);
            m_validKeyboardKeys.Add(MyKeys.Pause);
            m_validKeyboardKeys.Add(MyKeys.Q);
            m_validKeyboardKeys.Add(MyKeys.R);
            m_validKeyboardKeys.Add(MyKeys.Right);
            m_validKeyboardKeys.Add(MyKeys.RightAlt);
            m_validKeyboardKeys.Add(MyKeys.RightControl);
            m_validKeyboardKeys.Add(MyKeys.RightShift);
            m_validKeyboardKeys.Add(MyKeys.S);
            m_validKeyboardKeys.Add(MyKeys.Space);
            m_validKeyboardKeys.Add(MyKeys.Subtract);
            m_validKeyboardKeys.Add(MyKeys.T);
            m_validKeyboardKeys.Add(MyKeys.Tab);
            m_validKeyboardKeys.Add(MyKeys.U);
            m_validKeyboardKeys.Add(MyKeys.Up);
            m_validKeyboardKeys.Add(MyKeys.V);
            m_validKeyboardKeys.Add(MyKeys.W);
            m_validKeyboardKeys.Add(MyKeys.X);
            m_validKeyboardKeys.Add(MyKeys.Y);
            m_validKeyboardKeys.Add(MyKeys.Z);
            m_validKeyboardKeys.Add(MyKeys.F1);
            m_validKeyboardKeys.Add(MyKeys.F2);
            m_validKeyboardKeys.Add(MyKeys.F3);
            m_validKeyboardKeys.Add(MyKeys.F4);
            m_validKeyboardKeys.Add(MyKeys.F5);
            m_validKeyboardKeys.Add(MyKeys.F6);
            m_validKeyboardKeys.Add(MyKeys.F7);
            m_validKeyboardKeys.Add(MyKeys.F8);
            m_validKeyboardKeys.Add(MyKeys.F9);
            m_validKeyboardKeys.Add(MyKeys.F10);
            m_validKeyboardKeys.Add(MyKeys.F11);
            m_validKeyboardKeys.Add(MyKeys.F12);

            //  List of assignable mouse buttons
            m_validMouseButtons.Add(MyMouseButtonsEnum.Left);
            m_validMouseButtons.Add(MyMouseButtonsEnum.Middle);
            m_validMouseButtons.Add(MyMouseButtonsEnum.Right);
            m_validMouseButtons.Add(MyMouseButtonsEnum.XButton1);
            m_validMouseButtons.Add(MyMouseButtonsEnum.XButton2);
            m_validMouseButtons.Add(MyMouseButtonsEnum.None);

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
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Zneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationXpos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationXneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationYpos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationYneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationZpos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.RotationZneg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Slider1pos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Slider1neg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Slider2pos);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.Slider2neg);
            m_validJoystickAxes.Add(MyJoystickAxesEnum.None);

            #endregion

            CheckValidControls(m_defaultGameControlsList);

            LoadControls(controlsGeneral, controlsButtons);
            TakeSnapshot();
            ClearBlacklist();
        }

        IntPtr m_windowHandle;
        public void LoadContent(IntPtr windowHandle)
        {
            m_windowHandle = windowHandle;

            //MySandboxGame.Static.WindowHandle
            MyWindowsMouse.SetWindow(windowHandle);

            //MyRawInput.RegisterDevice(SharpDX.Multimedia.UsagePage.Generic, SharpDX.Multimedia.UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None, MySandboxGame.Static.WindowHandle);
            //MyRawInput.MouseInput += MyRawInput_MouseInput;
            MyDirectInput.Initialize(windowHandle);

            InitDevicePluginHandlerCallBack();

            if (ENABLE_DEVELOPER_KEYS)
            {
                MyLog.Default.WriteLine("DEVELOPER KEYS ENABLED");
            }


            //  Make sure that DirectInput has been initialized
            //    InitializeJoystickIfPossible();

            m_keyboardState = new MyGuiLocalizedKeyboardState();
        }

        public IntPtr WindowHandle
        {
            get { return m_windowHandle; }
        }

        public ListReader<char> TextInput
        {
            get { return new ListReader<char>(m_currentTextInput); }
        }


        public void UnloadData()
        {
            UninitDevicePluginHandlerCallBack();

            MyDirectInput.Close();
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

            var devices = MyDirectInput.DirectInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                results.Add(device.InstanceName.Replace("\0",string.Empty));
            }
            return results;
        }

        //call this on call back when something is beeing plugged in or unplugged
        void InitializeJoystickIfPossible()
        {
            // try to dispose of the old joystick
            if (m_joystick != null)
            {
                m_joystick.Dispose();
                m_joystick = null;
                SetJoystickConnected(false);
                m_joystickType = null;
            }
            if (m_joystick == null)
            {
                // Joystick disabled?
                if (m_joystickInstanceName == null) return;

                //  Try to grab the joystick with the correct instance name
                var attachedDevices = MyDirectInput.DirectInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
                foreach (var device in attachedDevices)
                {
                    if (!device.InstanceName.Contains(m_joystickInstanceName))
                        continue;

                    try
                    {
                        //device.Type
                        m_joystick = new Joystick(MyDirectInput.DirectInput, device.InstanceGuid);
                        m_joystickType = device.Type;

                        //MethodInfo setCooperativeLevel = typeof(Device).GetMethod("SetCooperativeLevel",
                        //                                                             new[]
                        //                                                                 {
                        //                                                                     typeof (IntPtr),
                        //                                                                     typeof (CooperativeLevel)
                        //                                                                 });

                        m_joystick.SetCooperativeLevel(m_windowHandle, CooperativeLevel.NonExclusive | CooperativeLevel.Background);
                        //// Workaround for not need to reference System.Windows.Forms
                        //setCooperativeLevel.Invoke(m_joystick,
                        //                           new object[]
                        //                               {
                        //                                   m_windowHandle,
                        //                                   CooperativeLevel.NonExclusive | CooperativeLevel.Background
                        //                               });

                        break;
                    }
                    catch (SharpDX.SharpDXException)
                    {
                    }
                }

                // load and acquire joystick
                // both joystick and xbox 360 gamepad are treated as joystick device by slimdx
                if (m_joystick != null)
                {
                    int sliderCount = 0;
                    m_joystickXAxisSupported = m_joystickYAxisSupported = m_joystickZAxisSupported = false;
                    m_joystickRotationXAxisSupported = m_joystickRotationYAxisSupported = m_joystickRotationZAxisSupported = false;
                    m_joystickSlider1AxisSupported = m_joystickSlider2AxisSupported = false;
                    foreach (DeviceObjectInstance doi in m_joystick.GetObjects())
                    {
                        if ((doi.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
                        {
                            // set range 0..65535 for each axis
                            m_joystick.GetObjectPropertiesById(doi.ObjectId).Range = new InputRange(0, 65535);

                            // find out which axes are supported
                            if (doi.ObjectType == ObjectGuid.XAxis) m_joystickXAxisSupported = true;
                            else if (doi.ObjectType == ObjectGuid.YAxis) m_joystickYAxisSupported = true;
                            else if (doi.ObjectType == ObjectGuid.ZAxis) m_joystickZAxisSupported = true;
                            else if (doi.ObjectType == ObjectGuid.RxAxis) m_joystickRotationXAxisSupported = true;
                            else if (doi.ObjectType == ObjectGuid.RyAxis) m_joystickRotationYAxisSupported = true;
                            else if (doi.ObjectType == ObjectGuid.RzAxis) m_joystickRotationZAxisSupported = true;
                            else if (doi.ObjectType == ObjectGuid.Slider)
                            {
                                sliderCount++;
                                if (sliderCount >= 1) m_joystickSlider1AxisSupported = true;
                                if (sliderCount >= 2) m_joystickSlider2AxisSupported = true;
                            }
                        }
                    }

                    // acquire the device
                    try
                    {
                        m_joystick.Acquire();
                        SetJoystickConnected(true);
                    }
                    catch (SharpDX.SharpDXException)
                    {
                    }
                }
            }
        }

        private void InitDevicePluginHandlerCallBack()
        {
            MyMessageLoop.AddMessageHandler(VRage.Win32.WinApi.WM.DEVICECHANGE, DeviceChangeCallback);
        }

        private void DeviceChangeCallback(ref Message m)
        {
            if (m_joystick == null || !MyDirectInput.DirectInput.IsDeviceAttached(m_joystick.Information.InstanceGuid))
            {
                InitializeJoystickIfPossible();
            }
        }

        private void UninitDevicePluginHandlerCallBack()
        {
            MyMessageLoop.RemoveMessageHandler(VRage.Win32.WinApi.WM.DEVICECHANGE, DeviceChangeCallback);
        }

        internal void ClearStates()
        {
            m_keyboardState.ClearStates();
            m_previousMouseState = m_actualMouseState;
            m_actualMouseState = new MyMouseState();
            m_actualMouseStateRaw.ClearPosition();
            MyOpenVR.ClearButtonStates();
        }

        internal void UpdateStatesFromPlayback(MyKeyboardState currentKeyboard, MyKeyboardState previousKeyboard, MyMouseState currentMouse, MyMouseState previousMouse, JoystickState currentJoystick, JoystickState previousJoystick, int x, int y)
        {
            m_keyboardState.UpdateStatesFromSnapshot(currentKeyboard,previousKeyboard);
            m_previousMouseState = previousMouse;
            m_actualMouseState = currentMouse;
            m_actualJoystickState = currentJoystick;
            m_previousJoystickState = previousJoystick;
            m_absoluteMousePosition = new Vector2(x, y);
            if (m_gameWasFocused)
            {
                MyWindowsMouse.SetPosition(x, y);
            }
        }

        internal void UpdateStates()
        {
            ProfilerShort.Begin("MyDirectXInput::UpdateStates");
            m_previousMouseState = m_actualMouseState;
            m_keyboardState.UpdateStates();
            //m_actualMouseState = Sandbox.Engine.Input.MyWindowsMouse.GetCurrentState();
            //m_actualMouseState = m_actualMouseStateRaw;
            m_actualMouseStateRaw = MyDirectInput.GetMouseState();
            int wheel = m_actualMouseState.ScrollWheelValue + m_actualMouseStateRaw.ScrollWheelValue;
            m_actualMouseState = m_actualMouseStateRaw;
            m_actualMouseState.ScrollWheelValue = wheel;
            m_actualMouseStateRaw.ClearPosition();

            int x, y;
            MyWindowsMouse.GetPosition(out x, out y);
            m_absoluteMousePosition = new Vector2(x, y);

            MyOpenVR.ClearButtonStates();
            MyOpenVR.PollEvents();//if this crashes because of some strange error you don't know, maybe openvr_api.cs was updated and you should check VREvent_Keyboard_t definition there (or just comment it out from VREvent_Data_t :-/ )

            if (IsJoystickConnected())
            {
                //  Try/catch block around the joystick .Poll() function to catch an exception thrown when the device is detached DURING gameplay. 
                try
                {
                    m_joystick.Acquire();
                    m_joystick.Poll();
                    m_previousJoystickState = m_actualJoystickState;
                    m_actualJoystickState = ((Joystick)m_joystick).GetCurrentState();

                    if (JoystickAsMouse)
                    {
                        var xPos = GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Xpos);
                        var xNeg = -GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Xneg);
                        var yPos = GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Ypos);
                        var yNeg = -GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Yneg);
                        m_absoluteMousePosition.X += (xPos + xNeg) * MyJoystickConstants.JOYSTICK_AS_MOUSE_MULTIPLIER;
                        m_absoluteMousePosition.Y += (yPos + yNeg) * MyJoystickConstants.JOYSTICK_AS_MOUSE_MULTIPLIER;
                        MyWindowsMouse.SetPosition((int)m_absoluteMousePosition.X, (int)m_absoluteMousePosition.Y);
                    }
                }
                catch
                {
                    SetJoystickConnected(false);
                }
            }

            if (IsJoystickLastUsed)
            {
                if (IsAnyMousePressed() || IsAnyKeyPress())
                    IsJoystickLastUsed = false;
            }
            else
            {
                if (IsAnyJoystickButtonPressed() || IsAnyJoystickAxisPressed())
                    IsJoystickLastUsed = true;
            }

            m_hasher.Keys.Clear();
            GetPressedKeys(m_hasher.Keys);
            // This is our secret key combo stored as hash + salt so it cannot be extracted from sources.
            if(!ENABLE_DEVELOPER_KEYS && m_hasher.TestHash("B12885A220B56226022423E34A12A182", "salt!@#"))
            {
                ENABLE_DEVELOPER_KEYS = true;
                MyLog.Default.WriteLine("DEVELOPER KEYS ENABLED");
            }
            ProfilerShort.End();
        }


        bool m_gameWasFocused = false;

        //  Update keyboard/mouse input and return true if application has focus (is active). Otherwise false.
        public bool Update(bool gameFocused)
        {
            ProfilerShort.Begin("MyDirectXInput::Update");
            bool ret;

            if (!m_gameWasFocused && gameFocused)
            {
                //We call 2x Update states to not receive NewKeyPressed if user keeps pressed key while AltTabbing
                UpdateStates();
            }

            m_gameWasFocused = gameFocused;


            if (!gameFocused && !OverrideUpdate)
            {
                ClearStates();
                ProfilerShort.End();
                return false;
            }
            ProfilerShort.BeginNextBlock("MyDirectXInput::Update2");

            //if (m_recordingBeingPlayed != null)
            //    UpdateStatesFromRecording();
            //else

            if (!OverrideUpdate)
            {
                UpdateStates();
            }
            
            //if (m_isRandomTestRun)
            //    GenerateRandomStates();

            //if (IsNewLeftMouseReleased())
            //{
            //    var screenCoordinate = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(MyGuiManager.MouseCursorPosition);

            //    m_leftButtonDoubleClick = MySandboxGame.TotalTimeInMilliseconds - m_lastLeftButtonClickTime < DOUBLE_CLICK_DELAY &&
            //        (screenCoordinate - m_lastLeftButtonMousePosition).LengthSquared() < DOUBLE_CLICK_MAXIMUM_DISTANCE_SQUARED;

            //    m_lastLeftButtonClickTime = m_leftButtonDoubleClick ? 0 : MySandboxGame.TotalTimeInMilliseconds;
            //    m_lastLeftButtonMousePosition = screenCoordinate;
            //}
            //else
            //{
            //    m_leftButtonDoubleClick = false;
            //}

            ret = true;
            ProfilerShort.BeginNextBlock("MyDirectXInput::Update3");
            //if (m_isRecordingInput)
            //  RecordInputSnapshot();
            m_bufferedInputSource.SwapBufferedTextInput(ref m_currentTextInput);
            ProfilerShort.End();

            

            return ret;
        }

        //  Return true if ANY key IS pressed, that means that the key was pressed now. During previous Update it wasn't pressed at all.
        public bool IsAnyKeyPress()
        {
            return (m_keyboardState.IsAnyKeyPressed());
        }

        //  Return true if ANY NEW key IS pressed, that means that the key was pressed now. During previous Update it wasn't pressed at all.
        public bool IsAnyNewKeyPress()
        {
            return (m_keyboardState.IsAnyKeyPressed() && !m_keyboardState.GetPreviousKeyboardState().IsAnyKeyPressed());
        }

        //  Return true if ANY mouse key IS pressed.
        public bool IsAnyMousePressed()
        {
            return m_actualMouseState.LeftButton ||
                   m_actualMouseState.MiddleButton ||
                   m_actualMouseState.RightButton ||
                   m_actualMouseState.XButton1 ||
                   m_actualMouseState.XButton2;
        }

        public bool IsAnyNewMousePressed()
        {
            return IsNewLeftMousePressed() ||
                   IsNewMiddleMousePressed() ||
                   IsNewRightMousePressed() ||
                   IsNewXButton1MousePressed() ||
                   IsNewXButton2MousePressed();
        }

        //  Check to see if any button is currently pressed on the joystick
        public bool IsAnyJoystickButtonPressed()
        {
            if (m_joystickConnected)
            {
                bool dpad = IsGamepadKeyDownPressed() || IsGamepadKeyLeftPressed() || IsGamepadKeyRightPressed() || IsGamepadKeyUpPressed();
                if (dpad)
                    return true;
                for (int i = 0; i < MyJoystickConstants.MAXIMUM_BUTTON_COUNT; i++)
                {
                    if (m_actualJoystickState.Buttons[i])
                        return true;
                }
            }
            return false;
        }

        public bool IsAnyNewJoystickButtonPressed()
        {
            if (m_joystickConnected)
            {
                for (int i = 0; i < MyJoystickConstants.MAXIMUM_BUTTON_COUNT; i++)
                {
                    if (m_actualJoystickState.Buttons[i] && !m_previousJoystickState.Buttons[i])
                        return true;
                }
            }
            return false;
        }

        public bool IsNewGameControlJoystickOnlyPressed(MyStringId controlId)
        {
            // Do nothing for blocked controls.
            if (IsControlBlocked(controlId)) return false;

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
            // Do nothing for blocked controls.
            if (IsControlBlocked(controlId)) return false;

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
            // Do nothing for blocked controls.
            if (IsControlBlocked(controlId)) return false;

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
            if (m_joystickConnected)
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
            return IsAnyMousePressed() || IsAnyJoystickButtonPressed();
        }

        public bool IsAnyNewMouseOrJoystickPressed()
        {
            return IsAnyNewMousePressed() || IsAnyNewJoystickButtonPressed();
        }

        public bool IsNewPrimaryButtonPressed()
        {
            return IsNewLeftMousePressed() || IsJoystickButtonNewPressed(MyJoystickButtonsEnum.J01);
        }

        public bool IsNewSecondaryButtonPressed()
        {
            return IsNewRightMousePressed() || IsJoystickButtonNewPressed(MyJoystickButtonsEnum.J02);
        }

        public bool IsNewPrimaryButtonReleased()
        {
            return IsNewLeftMouseReleased() || IsNewJoystickButtonReleased(MyJoystickButtonsEnum.J01);
        }

        public bool IsNewSecondaryButtonReleased()
        {
            return IsNewRightMouseReleased() || IsNewJoystickButtonReleased(MyJoystickButtonsEnum.J02);
        }

        public bool IsPrimaryButtonReleased()
        {
            return IsLeftMouseReleased() || IsJoystickButtonReleased(MyJoystickButtonsEnum.J01);
        }

        public bool IsSecondaryButtonReleased()
        {
            return IsRightMouseReleased() || IsJoystickButtonReleased(MyJoystickButtonsEnum.J02);
        }

        public bool IsPrimaryButtonPressed()
        {
            return IsLeftMousePressed() || IsJoystickButtonPressed(MyJoystickButtonsEnum.J01);
        }

        public bool IsSecondaryButtonPressed()
        {
            return IsRightMousePressed() || IsJoystickButtonPressed(MyJoystickButtonsEnum.J02);
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
            return IsKeyPress(MyKeys.LeftWindows) || IsKeyPress(MyKeys.RightWindows);
        }

        //True if any SHIFT key is pressed
        public bool IsAnyShiftKeyPressed()
        {
            return IsKeyPress(MyKeys.LeftShift) || IsKeyPress(MyKeys.RightShift);
        }

        //True if any ALT key is pressed
        public bool IsAnyAltKeyPressed()
        {
            return IsKeyPress(MyKeys.Alt) || IsKeyPress(MyKeys.LeftAlt) || IsKeyPress(MyKeys.RightAlt);
        }

        //True if any CTRL key is pressed
        public bool IsAnyCtrlKeyPressed()
        {
            return IsKeyPress(MyKeys.LeftControl) || IsKeyPress(MyKeys.RightControl);
        }

        //  Gets an array of values that correspond to the keyboard keys that are currently
        //  being pressed. Reference page contains links to related code samples.
        public void GetPressedKeys(List<MyKeys> keys)
        {
            m_keyboardState.GetActualPressedKeys(keys);
        }

        #region Key Button States

        //  Return true if new key pressed right now. Don't care if it was pressed in previous update too.
        public bool IsKeyPress(MyKeys key)
        {
            return m_keyboardState.IsKeyDown(key);
        }

        //  Return true if new key was pressed, that means this key was pressed now. During previous Update it wasn't pressed at all.
        public bool IsNewKeyPressed(MyKeys key)
        {
            return (m_keyboardState.IsKeyDown(key) && m_keyboardState.IsPreviousKeyUp(key));
        }

        //  Return true if key was pressed in previous update and now it is not.
        public bool IsNewKeyReleased(MyKeys key)
        {
            return (m_keyboardState.IsKeyUp(key) && m_keyboardState.IsPreviousKeyDown(key));
        }

        #endregion

        #region Mouse Button States
        public bool IsMousePressed(MyMouseButtonsEnum button)
        {
            switch (button)
            {
                case MyMouseButtonsEnum.Left: return IsLeftMousePressed();
                case MyMouseButtonsEnum.Middle: return IsMiddleMousePressed();
                case MyMouseButtonsEnum.Right: return IsRightMousePressed();
                case MyMouseButtonsEnum.XButton1: return IsXButton1MousePressed();
                case MyMouseButtonsEnum.XButton2: return IsXButton2MousePressed();
                default:
                    return false;
            }
        }

        public bool IsMouseReleased(MyMouseButtonsEnum button)
        {
            switch (button)
            {
                case MyMouseButtonsEnum.Left: return IsLeftMouseReleased();
                case MyMouseButtonsEnum.Middle: return IsMiddleMouseReleased();
                case MyMouseButtonsEnum.Right: return IsRightMouseReleased();
                case MyMouseButtonsEnum.XButton1: return IsXButton1MouseReleased();
                case MyMouseButtonsEnum.XButton2: return IsXButton2MouseReleased();
                default:
                    return false;
            }
        }

        public bool WasMousePressed(MyMouseButtonsEnum button)
        {
            switch (button)
            {
                case MyMouseButtonsEnum.Left: return WasLeftMousePressed();
                case MyMouseButtonsEnum.Middle: return WasMiddleMousePressed();
                case MyMouseButtonsEnum.Right: return WasRightMousePressed();
                case MyMouseButtonsEnum.XButton1: return WasXButton1MousePressed();
                case MyMouseButtonsEnum.XButton2: return WasXButton2MousePressed();
                default:
                    return false;
            }
        }

        public bool WasMouseReleased(MyMouseButtonsEnum button)
        {
            switch (button)
            {
                case MyMouseButtonsEnum.Left: return WasLeftMouseReleased();
                case MyMouseButtonsEnum.Middle: return WasMiddleMouseReleased();
                case MyMouseButtonsEnum.Right: return WasRightMouseReleased();
                case MyMouseButtonsEnum.XButton1: return WasXButton1MouseReleased();
                case MyMouseButtonsEnum.XButton2: return WasXButton2MouseReleased();
                default:
                    return false;
            }
        }

        public bool IsNewMousePressed(MyMouseButtonsEnum button)
        {
            switch (button)
            {
                case MyMouseButtonsEnum.Left: return IsNewLeftMousePressed();
                case MyMouseButtonsEnum.Middle: return IsNewMiddleMousePressed();
                case MyMouseButtonsEnum.Right: return IsNewRightMousePressed();
                case MyMouseButtonsEnum.XButton1: return IsNewXButton1MousePressed();
                case MyMouseButtonsEnum.XButton2: return IsNewXButton2MousePressed();
                default:
                    return false;
            }
        }

        public bool IsNewMouseReleased(MyMouseButtonsEnum button)
        {
            switch (button)
            {
                case MyMouseButtonsEnum.Left: return IsNewLeftMouseReleased();
                case MyMouseButtonsEnum.Middle: return IsNewMiddleMouseReleased();
                case MyMouseButtonsEnum.Right: return IsNewRightMouseReleased();
                case MyMouseButtonsEnum.XButton1: return IsNewXButton1MouseReleased();
                case MyMouseButtonsEnum.XButton2: return IsNewXButton2MouseReleased();
                default:
                    return false;
            }
        }

        #endregion

        #region Left Mouse Button States

        //  True if LEFT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewLeftMousePressed()
        {
            return (IsLeftMousePressed() && WasLeftMouseReleased());
        }

        //  True if LEFT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        public bool IsNewLeftMouseReleased()
        {
            return (IsLeftMouseReleased() && WasLeftMousePressed());
        }

        //  True if LEFT mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsLeftMousePressed()
        {
            return (m_actualMouseState.LeftButton);
        }

        //  True if LEFT mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsLeftMouseReleased()
        {
            return (m_actualMouseState.LeftButton == false);
        }

        public bool WasLeftMouseReleased()
        {
            return (m_previousMouseState.LeftButton == false);
        }

        //  True if LEFT mouse was pressed in previous update.
        public bool WasLeftMousePressed()
        {
            return (m_previousMouseState.LeftButton);
        }

        #endregion

        #region Right Mouse Button states

        //  True if RIGHT mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsRightMousePressed()
        {
            return (m_actualMouseState.RightButton);
        }

        //  True if RIGHT mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsRightMouseReleased()
        {
            return (m_actualMouseState.RightButton == false);
        }

        //  True if RIGHT mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewRightMousePressed()
        {
            return ((m_actualMouseState.RightButton) && (m_previousMouseState.RightButton == false));
        }

        //  True if RIGHT mouse is released right now, but previous update wasn't pressed. So this is one-time release.
        public bool IsNewRightMouseReleased()
        {
            return ((m_actualMouseState.RightButton == false) && (m_previousMouseState.RightButton));
        }

        public bool WasRightMousePressed()
        {
            return (m_previousMouseState.RightButton);
        }

        public bool WasRightMouseReleased()
        {
            return (m_previousMouseState.RightButton == false);
        }

        #endregion

        #region Middle Mouse Button States

        //  True if MIDDLE mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsMiddleMousePressed()
        {
            return (m_actualMouseState.MiddleButton);
        }

        //  True if MIDDLE mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsMiddleMouseReleased()
        {
            return (m_actualMouseState.MiddleButton == false);
        }

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewMiddleMousePressed()
        {
            return ((m_actualMouseState.MiddleButton) && (m_previousMouseState.MiddleButton == false));
        }

        //  True if MIDDLE mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewMiddleMouseReleased()
        {
            return ((m_actualMouseState.MiddleButton == false) && (m_previousMouseState.MiddleButton));
        }

        public bool WasMiddleMousePressed()
        {
            return (m_previousMouseState.MiddleButton);
        }

        public bool WasMiddleMouseReleased()
        {
            return (m_previousMouseState.MiddleButton == false);
        }

        #endregion

        #region XButton1 Mouse Button States

        //  True if XButton1 mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsXButton1MousePressed()
        {
            return (m_actualMouseState.XButton1);
        }

        //  True if XButton1 mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsXButton1MouseReleased()
        {
            return (m_actualMouseState.XButton1 == false);
        }

        //  True if XButton1 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewXButton1MousePressed()
        {
            return ((m_actualMouseState.XButton1) && (m_previousMouseState.XButton1 == false));
        }

        public bool IsNewXButton1MouseReleased()
        {
            return ((m_actualMouseState.XButton1 == false) && (m_previousMouseState.XButton1));
        }

        public bool WasXButton1MousePressed()
        {
            return (m_previousMouseState.XButton1);
        }

        public bool WasXButton1MouseReleased()
        {
            return (m_previousMouseState.XButton1 == false);
        }

        #endregion

        #region XButton2 Mouse Button States

        //  True if XButton2 mouse is pressed right now. Don't care if it was pressed in previous update too.
        public bool IsXButton2MousePressed()
        {
            return (m_actualMouseState.XButton2);
        }

        //  True if XButton2 mouse is released (not pressed) right now. Don't care if it was pressed/released in previous update too.
        public bool IsXButton2MouseReleased()
        {
            return (m_actualMouseState.XButton2 == false);
        }

        //  True if XButton2 mouse is pressed right now, but previous update wasn't pressed. So this is one-time press.
        public bool IsNewXButton2MousePressed()
        {
            return ((m_actualMouseState.XButton2) && (m_previousMouseState.XButton2 == false));
        }

        public bool IsNewXButton2MouseReleased()
        {
            return ((m_actualMouseState.XButton2 == false) && (m_previousMouseState.XButton2));
        }

        public bool WasXButton2MousePressed()
        {
            return (m_previousMouseState.XButton2);
        }

        public bool WasXButton2MouseReleased()
        {
            return (m_previousMouseState.XButton2 == false);
        }

        #endregion

        #region Joystick button States

        //  Check to see if a specific button on the joystick is pressed.
        public bool IsJoystickButtonPressed(MyJoystickButtonsEnum button)
        {
            bool isPressed = false;
            if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_actualJoystickState != null)
            {
                switch (button)
                {
                    case MyJoystickButtonsEnum.JDLeft: isPressed = IsGamepadKeyLeftPressed(); break;
                    case MyJoystickButtonsEnum.JDRight: isPressed = IsGamepadKeyRightPressed(); break;
                    case MyJoystickButtonsEnum.JDUp: isPressed = IsGamepadKeyUpPressed(); break;
                    case MyJoystickButtonsEnum.JDDown: isPressed = IsGamepadKeyDownPressed(); break;
                    default: isPressed = (m_actualJoystickState.Buttons[(int)button - 5]); break;
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
            if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_actualJoystickState != null && m_previousJoystickState != null)
            {
                switch (button)
                {
                    case MyJoystickButtonsEnum.JDLeft: isNewPressed = IsNewGamepadKeyLeftPressed(); break;
                    case MyJoystickButtonsEnum.JDRight: isNewPressed = IsNewGamepadKeyRightPressed(); break;
                    case MyJoystickButtonsEnum.JDUp: isNewPressed = IsNewGamepadKeyUpPressed(); break;
                    case MyJoystickButtonsEnum.JDDown: isNewPressed = IsNewGamepadKeyDownPressed(); break;
                    default: isNewPressed = ((m_actualJoystickState.IsPressed((int)button - 5)) && (m_previousJoystickState.IsPressed((int)button - 5) == false)); break;
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
            if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_actualJoystickState != null && m_previousJoystickState != null)
            {
                switch (button)
                {
                    case MyJoystickButtonsEnum.JDLeft: isReleased = IsNewGamepadKeyLeftReleased(); break;
                    case MyJoystickButtonsEnum.JDRight: isReleased = IsNewGamepadKeyRightReleased(); break;
                    case MyJoystickButtonsEnum.JDUp: isReleased = IsNewGamepadKeyUpReleased(); break;
                    case MyJoystickButtonsEnum.JDDown: isReleased = IsNewGamepadKeyDownReleased(); break;
                    default: isReleased = ((m_actualJoystickState.IsReleased((int)button - 5)) && (m_previousJoystickState.IsPressed((int)button - 5))); break;
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
            if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_actualJoystickState != null)
            {
                switch (button)
                {
                    case MyJoystickButtonsEnum.JDLeft: isReleased = !IsGamepadKeyLeftPressed(); break;
                    case MyJoystickButtonsEnum.JDRight: isReleased = !IsGamepadKeyRightPressed(); break;
                    case MyJoystickButtonsEnum.JDUp: isReleased = !IsGamepadKeyUpPressed(); break;
                    case MyJoystickButtonsEnum.JDDown: isReleased = !IsGamepadKeyDownPressed(); break;
                    default: isReleased = m_actualJoystickState.IsReleased((int)button - 5); break;
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
            if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_previousJoystickState != null)
            {
                switch (button)
                {
                    case MyJoystickButtonsEnum.JDLeft: wasPressed = WasGamepadKeyLeftPressed(); break;
                    case MyJoystickButtonsEnum.JDRight: wasPressed = WasGamepadKeyRightPressed(); break;
                    case MyJoystickButtonsEnum.JDUp: wasPressed = WasGamepadKeyUpPressed(); break;
                    case MyJoystickButtonsEnum.JDDown: wasPressed = WasGamepadKeyDownPressed(); break;
                    default: wasPressed = (m_previousJoystickState.Buttons[(int)button - 5]); break;
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
            if (m_joystickConnected && button != MyJoystickButtonsEnum.None && m_previousJoystickState != null)
            {
                switch (button)
                {
                    case MyJoystickButtonsEnum.JDLeft: wasReleased = !WasGamepadKeyLeftPressed(); break;
                    case MyJoystickButtonsEnum.JDRight: wasReleased = !WasGamepadKeyRightPressed(); break;
                    case MyJoystickButtonsEnum.JDUp: wasReleased = !WasGamepadKeyUpPressed(); break;
                    case MyJoystickButtonsEnum.JDDown: wasReleased = !WasGamepadKeyDownPressed(); break;
                    default: wasReleased = (m_previousJoystickState.IsReleased((int)button - 5)); break;
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

        //  Find out how much a specific joystick axis is pressed.
        //  Return a raw number between 0 and 65535. 32768 is the middle value.
        public float GetJoystickAxisStateRaw(MyJoystickAxesEnum axis)
        {
            int value = 32768;
            if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null && IsJoystickAxisSupported(axis))
            {
                switch (axis)
                {
                    case MyJoystickAxesEnum.RotationXpos:
                    case MyJoystickAxesEnum.RotationXneg: value = m_actualJoystickState.RotationX; break;
                    case MyJoystickAxesEnum.RotationYpos:
                    case MyJoystickAxesEnum.RotationYneg: value = m_actualJoystickState.RotationY; break;
                    case MyJoystickAxesEnum.RotationZpos:
                    case MyJoystickAxesEnum.RotationZneg: value = m_actualJoystickState.RotationZ; break;
                    case MyJoystickAxesEnum.Xpos:
                    case MyJoystickAxesEnum.Xneg: value = m_actualJoystickState.X; break;
                    case MyJoystickAxesEnum.Ypos:
                    case MyJoystickAxesEnum.Yneg: value = m_actualJoystickState.Y; break;
                    case MyJoystickAxesEnum.Zpos:
                    case MyJoystickAxesEnum.Zneg: value = m_actualJoystickState.Z; break;
                    case MyJoystickAxesEnum.Slider1pos:
                    case MyJoystickAxesEnum.Slider1neg:
                        {
                            var array = m_actualJoystickState.Sliders;
                            value = (array.Length < 1) ? 32768 : array[0];
                        }
                        break;
                    case MyJoystickAxesEnum.Slider2pos:
                    case MyJoystickAxesEnum.Slider2neg:
                        {
                            var array = m_actualJoystickState.Sliders;
                            value = (array.Length < 2) ? 32768 : array[1];
                        }
                        break;
                }
            }
            return value;
        }

        //  Find out how much a specific joystick axis was pressed.
        //  Return a raw number between 0 and 65535. 32768 is the middle value.
        public float GetPreviousJoystickAxisStateRaw(MyJoystickAxesEnum axis)
        {
            int value = 32768;
            if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_previousJoystickState != null && IsJoystickAxisSupported(axis))
            {
                switch (axis)
                {
                    case MyJoystickAxesEnum.RotationXpos:
                    case MyJoystickAxesEnum.RotationXneg: value = m_previousJoystickState.RotationX; break;
                    case MyJoystickAxesEnum.RotationYpos:
                    case MyJoystickAxesEnum.RotationYneg: value = m_previousJoystickState.RotationY; break;
                    case MyJoystickAxesEnum.RotationZpos:
                    case MyJoystickAxesEnum.RotationZneg: value = m_previousJoystickState.RotationZ; break;
                    case MyJoystickAxesEnum.Xpos:
                    case MyJoystickAxesEnum.Xneg: value = m_previousJoystickState.X; break;
                    case MyJoystickAxesEnum.Ypos:
                    case MyJoystickAxesEnum.Yneg: value = m_previousJoystickState.Y; break;
                    case MyJoystickAxesEnum.Zpos:
                    case MyJoystickAxesEnum.Zneg: value = m_previousJoystickState.Z; break;
                    case MyJoystickAxesEnum.Slider1pos:
                    case MyJoystickAxesEnum.Slider1neg:
                        {
                            var array = m_previousJoystickState.Sliders;
                            value = (array.Length < 1) ? 32768 : array[0];
                        }
                        break;
                    case MyJoystickAxesEnum.Slider2pos:
                    case MyJoystickAxesEnum.Slider2neg:
                        {
                            var array = m_previousJoystickState.Sliders;
                            value = (array.Length < 2) ? 32768 : array[1];
                        }
                        break;
                }
            }
            return value;
        }


        public float GetJoystickX()
        {
            return GetJoystickAxisStateRaw(MyJoystickAxesEnum.Xpos);
        }

        public float GetJoystickY()
        {
            return GetJoystickAxisStateRaw(MyJoystickAxesEnum.Ypos);
        }


        //  Find out how much a specific joystick half-axis is pressed.
        //  Return a number between 0 and 1 (taking deadzone, sensitivity and non-linearity into account).
        public float GetJoystickAxisStateForGameplay(MyJoystickAxesEnum axis)
        {
            if (m_joystickConnected && IsJoystickAxisSupported(axis))
            {
                // Input position scaled to (-1..1).
                float position = ((float)GetJoystickAxisStateRaw(axis) - (float)MyJoystickConstants.CENTER_AXIS) / (float)MyJoystickConstants.CENTER_AXIS;

                switch (axis)
                {
                    case MyJoystickAxesEnum.RotationXneg:
                    case MyJoystickAxesEnum.Xneg:
                    case MyJoystickAxesEnum.RotationYneg:
                    case MyJoystickAxesEnum.Yneg:
                    case MyJoystickAxesEnum.RotationZneg:
                    case MyJoystickAxesEnum.Zneg:
                    case MyJoystickAxesEnum.Slider1neg:
                    case MyJoystickAxesEnum.Slider2neg:
                        if (position >= 0) return 0;
                        break;
                    case MyJoystickAxesEnum.RotationXpos:
                    case MyJoystickAxesEnum.Xpos:
                    case MyJoystickAxesEnum.RotationYpos:
                    case MyJoystickAxesEnum.Ypos:
                    case MyJoystickAxesEnum.RotationZpos:
                    case MyJoystickAxesEnum.Zpos:
                    case MyJoystickAxesEnum.Slider1pos:
                    case MyJoystickAxesEnum.Slider2pos:
                        if (position <= 0) return 0;
                        break;
                    default:
                        MyDebug.AssertDebug(false, "Unknown joystick axis!");
                        break;
                }

                float distance = Math.Abs(position);
                if (distance > m_joystickDeadzone)
                {
                    distance = (distance - m_joystickDeadzone) / (1 - m_joystickDeadzone);  // Rescale distance to (0..1) outside the deadzone.
                    return m_joystickSensitivity * (float)Math.Pow(distance, m_joystickExponent);
                }
            }

            return 0;
        }

        //  Find out how much a specific joystick half-axis is pressed.
        //  Return a number between 0 and 1 (taking deadzone, sensitivity and non-linearity into account).
        public float GetPreviousJoystickAxisStateForGameplay(MyJoystickAxesEnum axis)
        {
            if (m_joystickConnected && IsJoystickAxisSupported(axis))
            {
                // Input position scaled to (-1..1).
                float position = ((float)GetPreviousJoystickAxisStateRaw(axis) - (float)MyJoystickConstants.CENTER_AXIS) / (float)MyJoystickConstants.CENTER_AXIS;

                switch (axis)
                {
                    case MyJoystickAxesEnum.RotationXneg:
                    case MyJoystickAxesEnum.Xneg:
                    case MyJoystickAxesEnum.RotationYneg:
                    case MyJoystickAxesEnum.Yneg:
                    case MyJoystickAxesEnum.RotationZneg:
                    case MyJoystickAxesEnum.Zneg:
                    case MyJoystickAxesEnum.Slider1neg:
                    case MyJoystickAxesEnum.Slider2neg:
                        if (position >= 0) return 0;
                        break;
                    case MyJoystickAxesEnum.RotationXpos:
                    case MyJoystickAxesEnum.Xpos:
                    case MyJoystickAxesEnum.RotationYpos:
                    case MyJoystickAxesEnum.Ypos:
                    case MyJoystickAxesEnum.RotationZpos:
                    case MyJoystickAxesEnum.Zpos:
                    case MyJoystickAxesEnum.Slider1pos:
                    case MyJoystickAxesEnum.Slider2pos:
                        if (position <= 0) return 0;
                        break;
                    default:
                        MyDebug.AssertDebug(false, "Unknown joystick axis!");
                        break;
                }

                float distance = Math.Abs(position);
                if (distance > m_joystickDeadzone)
                {
                    distance = (distance - m_joystickDeadzone) / (1 - m_joystickDeadzone);  // Rescale distance to (0..1) outside the deadzone.
                    return m_joystickSensitivity * (float)Math.Pow(distance, m_joystickExponent);
                }
            }

            return 0;
        }

        #region Joystick analog axes used for digital controls

        public bool IsJoystickAxisPressed(MyJoystickAxesEnum axis)
        {
            bool isPressed = false;
            if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null)
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
            if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null && m_previousJoystickState != null)
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
            if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null && m_previousJoystickState != null)
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
            if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_actualJoystickState != null)
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
            if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_previousJoystickState != null)
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
            if (m_joystickConnected && axis != MyJoystickAxesEnum.None && m_previousJoystickState != null)
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
            return m_actualMouseState.ScrollWheelValue;
        }

        //  Previous mouse scrollwheel value.
        public int PreviousMouseScrollWheelValue()
        {
            return m_previousMouseState.ScrollWheelValue;
        }

        //  Delta mouse scrollwheel value.
        public int DeltaMouseScrollWheelValue()
        {
            return MouseScrollWheelValue() - PreviousMouseScrollWheelValue();
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
            MyWindowsMouse.SetPosition(x, y);

            //MyTrace.Send(TraceWindow.Default, "MyWindowsMouse.SetPosition: " + x.ToString() + " " + y.ToString());
        }

        // Checks to see if the joystick is connected. This is used so we don't try to poll a joystick that doesn't exist.
        public bool IsJoystickConnected()
        {
            return m_joystickConnected;
        }

        public void SetJoystickConnected(bool value)
        {
            if (m_joystickConnected != value)
            {
                m_joystickConnected = value;
                if (JoystickConnected != null)
                    JoystickConnected(value);
            }
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

        //  POVDirection()
        //
        //  @return int[] of size NumJoystickPOVs(). Will contain the raw int
        //  representing the directing the POV is currently pointing. Use the public 
        //  consts below to determine direction:
        //  POV_NEUTRAL 
        //  POV_NORTH 
        //  POV_NORTHEAST
        //  POV_EAST
        //  POV_SOUTHEAST
        //  POV_SOUTH
        //  POV_SOUTHWEST
        //  POV_WEST
        //  POV_NORTHWEST
        //
        //  If no joystick is present return null
        public int[] POVDirection()
        {
            if (m_joystickConnected)
            {
                return m_actualJoystickState.PointOfViewControllers;
            }
            return null;
        }

        /// <summary>
        /// Get the actual and previous gamepad key directions (use the first POV controller).
        /// Returns false if this type of input is not available.
        /// </summary>
        public bool GetGamepadKeyDirections(out int actual, out int previous)
        {
            if (m_joystickConnected && m_actualJoystickState != null && m_previousJoystickState != null)
            {
                int[] actualPOVControllers = m_actualJoystickState.PointOfViewControllers;
                int[] previousPOVControllers = m_previousJoystickState.PointOfViewControllers;
                //Trace.SendMsgLastCall(actualPOVControllers[0].ToString() + previousPOVControllers[0].ToString());

                if (actualPOVControllers != null && previousPOVControllers != null)
                {
                    actual = actualPOVControllers[0];
                    previous = previousPOVControllers[0];
                    return true;
                }

            }
            actual = -1;
            previous = -1;
            return false;
        }

        public bool IsGamepadKeyRightPressed()
        {
            int actual, previous;
            if (GetGamepadKeyDirections(out actual, out previous)) return (actual >= 4500 && actual <= 13500);
            return false;
        }

        public bool IsGamepadKeyLeftPressed()
        {
            int actual, previous;
            if (GetGamepadKeyDirections(out actual, out previous)) return (actual >= 22500 && actual <= 31500);
            return false;
        }

        public bool IsGamepadKeyDownPressed()
        {
            int actual, previous;
            if (GetGamepadKeyDirections(out actual, out previous)) return (actual >= 13500 && actual <= 22500);
            return false;
        }

        public bool IsGamepadKeyUpPressed()
        {
            int actual, previous;
            if (GetGamepadKeyDirections(out actual, out previous)) return (actual >= 0 && actual <= 4500) || (actual >= 31500 && actual <= 36000);
            return false;
        }


        public bool WasGamepadKeyRightPressed()
        {
            int actual, previous;
            if (GetGamepadKeyDirections(out actual, out previous)) return (previous >= 4500 && previous <= 13500);
            return false;
        }

        public bool WasGamepadKeyLeftPressed()
        {
            int actual, previous;
            if (GetGamepadKeyDirections(out actual, out previous)) return (previous >= 22500 && previous <= 31500);
            return false;
        }

        public bool WasGamepadKeyDownPressed()
        {
            int actual, previous;
            if (GetGamepadKeyDirections(out actual, out previous)) return (previous >= 13500 && previous <= 22500);
            return false;
        }

        public bool WasGamepadKeyUpPressed()
        {
            int actual, previous;
            if (GetGamepadKeyDirections(out actual, out previous)) return (previous >= 0 && previous <= 4500) || (previous >= 31500 && previous <= 36000);
            return false;
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
            if (m_actualJoystickState == null)
            {
                text.Append("No joystick detected.");
                return;
            }

            var joy = m_actualJoystickState;

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

            text.Append("accX: "); text.AppendInt32(joy.AccelerationX); text.AppendLine();
            text.Append("accY: "); text.AppendInt32(joy.AccelerationY); text.AppendLine();
            text.Append("accZ: "); text.AppendInt32(joy.AccelerationZ); text.AppendLine();
            text.Append("angAccX: "); text.AppendInt32(joy.AngularAccelerationX); text.AppendLine();
            text.Append("angAccY: "); text.AppendInt32(joy.AngularAccelerationY); text.AppendLine();
            text.Append("angAccZ: "); text.AppendInt32(joy.AngularAccelerationZ); text.AppendLine();
            text.Append("angVelX: "); text.AppendInt32(joy.AngularVelocityX); text.AppendLine();
            text.Append("angVelY: "); text.AppendInt32(joy.AngularVelocityY); text.AppendLine();
            text.Append("angVelZ: "); text.AppendInt32(joy.AngularVelocityZ); text.AppendLine();
            text.Append("forX: "); text.AppendInt32(joy.ForceX); text.AppendLine();
            text.Append("forY: "); text.AppendInt32(joy.ForceY); text.AppendLine();
            text.Append("forZ: "); text.AppendInt32(joy.ForceZ); text.AppendLine();
            text.Append("rotX: "); text.AppendInt32(joy.RotationX); text.AppendLine();
            text.Append("rotY: "); text.AppendInt32(joy.RotationY); text.AppendLine();
            text.Append("rotZ: "); text.AppendInt32(joy.RotationZ); text.AppendLine();
            text.Append("torqX: "); text.AppendInt32(joy.TorqueX); text.AppendLine();
            text.Append("torqY: "); text.AppendInt32(joy.TorqueY); text.AppendLine();
            text.Append("torqZ: "); text.AppendInt32(joy.TorqueZ); text.AppendLine();
            text.Append("velX: "); text.AppendInt32(joy.VelocityX); text.AppendLine();
            text.Append("velY: "); text.AppendInt32(joy.VelocityY); text.AppendLine();
            text.Append("velZ: "); text.AppendInt32(joy.VelocityZ); text.AppendLine();
            text.Append("X: "); text.AppendInt32(joy.X); text.AppendLine();
            text.Append("Y: "); text.AppendInt32(joy.Y); text.AppendLine();
            text.Append("Z: "); text.AppendInt32(joy.Z); text.AppendLine();
            text.AppendLine();
            text.Append("AccSliders: "); foreach (var i in joy.AccelerationSliders) { text.AppendInt32(i); text.Append(" "); } text.AppendLine();
            text.Append("Buttons: "); foreach (var i in joy.Buttons) { text.Append(i ? "#" : "_"); text.Append(" "); } text.AppendLine();
            text.Append("ForSliders: "); foreach (var i in joy.ForceSliders) { text.AppendInt32(i); text.Append(" "); } text.AppendLine();
            text.Append("POVControllers: "); foreach (var i in joy.PointOfViewControllers) { text.AppendInt32(i); text.Append(" "); } text.AppendLine();
            text.Append("Sliders: "); foreach (var i in joy.Sliders) { text.AppendInt32(i); text.Append(" "); } text.AppendLine();
            text.Append("VelocitySliders: "); foreach (var i in joy.VelocitySliders) { text.AppendInt32(i); text.Append(" "); } text.AppendLine();
        }

        public bool IsJoystickAxisSupported(MyJoystickAxesEnum axis)
        {
            if (!m_joystickConnected) return false;
            switch (axis)
            {
                case MyJoystickAxesEnum.Xpos:
                case MyJoystickAxesEnum.Xneg: return m_joystickXAxisSupported;
                case MyJoystickAxesEnum.Ypos:
                case MyJoystickAxesEnum.Yneg: return m_joystickYAxisSupported;
                case MyJoystickAxesEnum.Zpos:
                case MyJoystickAxesEnum.Zneg: return m_joystickZAxisSupported;
                case MyJoystickAxesEnum.RotationXpos:
                case MyJoystickAxesEnum.RotationXneg: return m_joystickRotationXAxisSupported;
                case MyJoystickAxesEnum.RotationYpos:
                case MyJoystickAxesEnum.RotationYneg: return m_joystickRotationYAxisSupported;
                case MyJoystickAxesEnum.RotationZpos:
                case MyJoystickAxesEnum.RotationZneg: return m_joystickRotationZAxisSupported;
                case MyJoystickAxesEnum.Slider1pos:
                case MyJoystickAxesEnum.Slider1neg: return m_joystickSlider1AxisSupported;
                case MyJoystickAxesEnum.Slider2pos:
                case MyJoystickAxesEnum.Slider2neg: return m_joystickSlider2AxisSupported;
                default: return false;
            }
        }

        //  Check if an assigned control for game is new pressed.
        public bool IsNewGameControlPressed(MyStringId controlId)
        {
            // Do nothing for blocked controls.
            if (IsControlBlocked(controlId)) return false;
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
            // Do nothing for blocked controls.
            if(IsControlBlocked(controlId)) return false;
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
            // Do nothing for blocked controls.
            if (IsControlBlocked(controlId)) return false;
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
            // Do nothing for blocked controls.
            if (IsControlBlocked(controlId)) return 0f;
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
            // Do nothing for blocked controls.
            if (IsControlBlocked(controlId)) return false;
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
            foreach (var item in m_validKeyboardKeys)
            {
                if (item == key) return true;
            }

            return false;
        }

        public bool IsKeyDigit(MyKeys key)
        {
            return m_digitKeys.Contains(key);
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
            return m_gameControlsList;
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


            controlsButtons.Dictionary.Clear();

            foreach (MyControl control in m_gameControlsList.Values)
            {
                var gameControlData = new SerializableDictionary<string, string>();
                controlsButtons[control.GetGameControlEnum().ToString()] = gameControlData;

                gameControlData["Keyboard"] = control.GetKeyboardControl().ToString();

                gameControlData["Keyboard2"] = control.GetSecondKeyboardControl().ToString();

                gameControlData["Mouse"] = MyEnumsToStrings.MouseButtonsEnum[(int)control.GetMouseControl()];
            }
        }

        private bool LoadControls(SerializableDictionary<string, object> controlsGeneral, SerializableDictionary<string, object> controlsButtons)
        {
            if (controlsGeneral.Dictionary.Count == 0)
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
                LoadGameControls(controlsButtons);

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
            if (controlsButtons.Dictionary.Count == 0)
            {
                throw new Exception("ControlsButtons config parameter is empty.");
            }

            //BEFORE - FIRE_PRIMARY:Mouse:Left;
            //AFTER - FIRE_PRIMARY:Keyboard:A:Mouse:Left:Joystick:None
            foreach (var gameControlButton in controlsButtons.Dictionary)
            {
                var controlType = TryParseMyGameControlEnums(gameControlButton.Key);

                // deprecated controls will remain without any control set to it
                if (controlType.HasValue)
                {
                    m_gameControlsList[controlType.Value].SetNoControl();

                    var gameControlData = (SerializableDictionary<string, string>)gameControlButton.Value;

                    LoadGameControl(gameControlData["Keyboard"], controlType.Value, ParseMyGuiInputDeviceEnum("Keyboard"));
                    LoadGameControl(gameControlData["Keyboard2"], controlType.Value, ParseMyGuiInputDeviceEnum("KeyboardSecond"));
                    LoadGameControl(gameControlData["Mouse"], controlType.Value, ParseMyGuiInputDeviceEnum("Mouse"));
                }
            }
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

        public void SetControlBlock(MyStringId controlEnum, bool block = false)
        {
            if (block)
            {
                m_gameControlsBlacklist.Add(controlEnum);
            }
            else
            {
                m_gameControlsBlacklist.Remove(controlEnum);
            }
        }

        public bool IsControlBlocked(MyStringId controlEnum)
        {
            return m_gameControlsBlacklist.Contains(controlEnum);
        }

        public void ClearBlacklist()
        {
            m_gameControlsBlacklist.Clear();
        }
    }
}
#endif