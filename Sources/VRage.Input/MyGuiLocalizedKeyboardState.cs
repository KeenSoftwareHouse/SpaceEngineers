#if !XB1

using System;
using System.Collections.Generic;
using VRage.Win32;

namespace VRage.Input
{
	[Unsharper.UnsharperDisableReflection]
    public class MyGuiLocalizedKeyboardState
    {
        static HashSet<byte> m_localKeys;

        internal const uint KLF_NOTELLSHELL = 0x00000080;

        public struct KeyboardLayout : IDisposable
        {
            public readonly IntPtr Handle;

            public KeyboardLayout(IntPtr handle)
                : this()
            {
                Handle = handle;
            }

            public KeyboardLayout(string keyboardLayoutID)
#if UNSHARPER
				:this(new IntPtr())
#else
                : this(WinApi.LoadKeyboardLayout(keyboardLayoutID, KLF_NOTELLSHELL))
#endif
            {
            }

            public bool IsDisposed
            {
                get;
                private set;
            }

            public void Dispose()
            {
                if (IsDisposed)
                    return;
#if UNSHARPER
					System.Diagnostics.Debug.Assert(false);
#else
                WinApi.UnloadKeyboardLayout(Handle);
#endif
                IsDisposed = true;
            }

            public static KeyboardLayout US_English = new KeyboardLayout("00000409");

            public static KeyboardLayout Active
            {
                get
                {
#if UNSHARPER
					System.Diagnostics.Debug.Assert(false);
					return new KeyboardLayout();
#else
					return new KeyboardLayout(WinApi.GetKeyboardLayout(IntPtr.Zero));
#endif
                }
            }
        }

        private MyKeyboardState m_previousKeyboardState;
        private MyKeyboardState m_actualKeyboardState;

        public MyGuiLocalizedKeyboardState()
        {
            m_actualKeyboardState = MyWindowsKeyboard.GetCurrentState();

            if (m_localKeys == null)
            {
                m_localKeys = new HashSet<byte>();

                AddLocalKey(MyKeys.LeftControl);
                AddLocalKey(MyKeys.LeftAlt);
                AddLocalKey(MyKeys.LeftShift);
                AddLocalKey(MyKeys.RightAlt);
                AddLocalKey(MyKeys.RightControl);
                AddLocalKey(MyKeys.RightShift);
                AddLocalKey(MyKeys.Delete);
                AddLocalKey(MyKeys.NumPad0);
                AddLocalKey(MyKeys.NumPad1);
                AddLocalKey(MyKeys.NumPad2);
                AddLocalKey(MyKeys.NumPad3);
                AddLocalKey(MyKeys.NumPad4);
                AddLocalKey(MyKeys.NumPad5);
                AddLocalKey(MyKeys.NumPad6);
                AddLocalKey(MyKeys.NumPad7);
                AddLocalKey(MyKeys.NumPad8);
                AddLocalKey(MyKeys.NumPad9);
                AddLocalKey(MyKeys.Decimal);
                AddLocalKey(MyKeys.LeftWindows);
                AddLocalKey(MyKeys.RightWindows);
                AddLocalKey(MyKeys.Apps);
                AddLocalKey(MyKeys.Pause);
                AddLocalKey(MyKeys.Divide);
            }
        }

        void AddLocalKey(MyKeys key)
        {
            m_localKeys.Add((byte)key);
        }

        public void ClearStates()
        {
            m_previousKeyboardState = m_actualKeyboardState;
            m_actualKeyboardState = new MyKeyboardState();
        }


        public void UpdateStates()
        {
            m_previousKeyboardState = m_actualKeyboardState;
            m_actualKeyboardState = MyWindowsKeyboard.GetCurrentState();
        }

        // Used to simulate input for recordings
        public void UpdateStatesFromSnapshot(MyKeyboardState state)
        {
            m_previousKeyboardState = m_actualKeyboardState;
            m_actualKeyboardState = state;
        }

        public void UpdateStatesFromSnapshot(MyKeyboardState currentState, MyKeyboardState previousState)
        {
            m_previousKeyboardState = previousState;
            m_actualKeyboardState = currentState;
        }

        public MyKeyboardState GetActualKeyboardState()
        {
            return m_actualKeyboardState;
        }

        public MyKeyboardState GetPreviousKeyboardState()
        {
            return m_previousKeyboardState;
        }

        public void SetKey(MyKeys key, bool value)
        {
            m_actualKeyboardState.SetKey(key, value);
        }

        public bool IsPreviousKeyDown(MyKeys key, bool isLocalKey)
        {
            if (!isLocalKey)
                key = LocalToUSEnglish(key);

            return m_previousKeyboardState.IsKeyDown((MyKeys)key);
        }

        public bool IsPreviousKeyDown(MyKeys key)
        {
            return IsPreviousKeyDown(key, IsKeyLocal(key));
        }

        public bool IsPreviousKeyUp(MyKeys key, bool isLocalKey)
        {
            if (!isLocalKey)
                key = LocalToUSEnglish(key);

            return m_previousKeyboardState.IsKeyUp((MyKeys)key);
        }

        public bool IsPreviousKeyUp(MyKeys key)
        {
            return IsPreviousKeyUp(key, IsKeyLocal(key));
        }


        public bool IsKeyDown(MyKeys key, bool isLocalKey)
        {
            if (!isLocalKey)
                key = LocalToUSEnglish(key);

            return m_actualKeyboardState.IsKeyDown((MyKeys)key);
        }

        public bool IsKeyUp(MyKeys key, bool isLocalKey)
        {
            if (!isLocalKey)
                key = LocalToUSEnglish(key);

            return m_actualKeyboardState.IsKeyUp((MyKeys)key);
        }

        public bool IsKeyDown(MyKeys key)
        {
            return IsKeyDown(key, IsKeyLocal(key));
        }

        bool IsKeyLocal(MyKeys key)
        {
            return m_localKeys.Contains((byte)key);
        }

        public bool IsKeyUp(MyKeys key)
        {
            return IsKeyUp(key, IsKeyLocal(key));
        }

        // Maps a localized character like 'S' to the virtual scan code
        //  for that key on the user's keyboard ('O' in dvorak, for example)
        public static MyKeys USEnglishToLocal(MyKeys key)
        {
            return key;

            //var activeScanCode = MyWindowsAPIWrapper.MapVirtualKeyEx((uint)key, MAPVK.VK_TO_VSC, KeyboardLayout.US_English.Handle);
            //var nativeVirtualCode = MyWindowsAPIWrapper.MapVirtualKeyEx(activeScanCode, MAPVK.VSC_TO_VK, KeyboardLayout.Active.Handle);

            //return (Keys)nativeVirtualCode;
        }

        public static MyKeys LocalToUSEnglish(MyKeys key)
        {
            return key;

            /*
            var activeScanCode = MyWindowsAPIWrapper.MapVirtualKeyEx((uint)key, MAPVK.VK_TO_VSC, KeyboardLayout.US_English.Handle);
            var nativeVirtualCode = MyWindowsAPIWrapper.MapVirtualKeyEx(activeScanCode, MAPVK.VSC_TO_VK, KeyboardLayout.Active.Handle);
              */

            //var activeScanCode = MyWindowsAPIWrapper.MapVirtualKeyEx((uint)key, MAPVK.VK_TO_VSC, KeyboardLayout.Active.Handle);
            //var nativeVirtualCode = MyWindowsAPIWrapper.MapVirtualKeyEx(activeScanCode, MAPVK.VSC_TO_VK, KeyboardLayout.US_English.Handle);

            //return (Keys)nativeVirtualCode;
        }

        public bool IsAnyKeyPressed()
        {
            return m_actualKeyboardState.IsAnyKeyPressed();
        }

        public void GetActualPressedKeys(List<MyKeys> keys)
        {
            m_actualKeyboardState.GetPressedKeys(keys);
            for (int i = 0; i < keys.Count; i++)
            {
                if (!IsKeyLocal((MyKeys)keys[i]))
                    keys[i] = (MyKeys)USEnglishToLocal((MyKeys)keys[i]);
            }
        }
    }
}

#endif