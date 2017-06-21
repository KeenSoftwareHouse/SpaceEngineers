#if !XB1

using SharpDX;
using SharpDX.DirectInput;
using System;
using VRage.Utils;

namespace VRage.Input
{
    public static class MyDirectInput
    {
        static DirectInput m_directInput;
        public static DirectInput DirectInput
        {
            get { return m_directInput; }
        }
        static Mouse m_mouse;
        static MouseState m_mouseState = new MouseState();

        public static void Initialize(IntPtr handle)
        {
            try
            {
                m_directInput = new DirectInput();
                m_mouse = new Mouse(m_directInput);
                try
                {
                    m_mouse.SetCooperativeLevel(handle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
                }
                catch
                {
                    MyLog.Default.WriteLine("WARNING: DirectInput SetCooperativeLevel failed");
                }
            }
            catch (SharpDXException ex)
            {
                MyLog.Default.WriteLine("DirectInput initialization error: " + ex);
            }
        }

        public static void Close()
        {
            if (m_mouse != null)
            {
                m_mouse.Dispose();
                m_mouse = null;
            }

            if (m_directInput != null)
            {
                m_directInput.Dispose();
                m_directInput = null;
            }
        }

        static public MyMouseState GetMouseState()
        {
            //Debug.Assert(m_mouse != null, "Mouse was closed but not created again");

            // This happens when changing device
            if (m_mouse == null)
                return new MyMouseState();

            MyMouseState mouseState = new MyMouseState();
            if (m_mouse.TryAcquire().Success)
            {
                try
                {
                    m_mouse.GetCurrentState(ref m_mouseState);

                    m_mouse.Poll();
                    mouseState = new MyMouseState()
                    {
                        X = m_mouseState.X,
                        Y = m_mouseState.Y,
                        LeftButton = m_mouseState.Buttons[0],
                        RightButton = m_mouseState.Buttons[1],
                        MiddleButton = m_mouseState.Buttons[2],
                        XButton1 = m_mouseState.Buttons[3],
                        XButton2 = m_mouseState.Buttons[4],
                        ScrollWheelValue = m_mouseState.Z
                    };
                }
                catch (SharpDXException)
                {
                    // This happens when mouse is unacquired between calling TryAcquire and GetCurrentState
                }
            }

            return mouseState;
        }
    }
}

#endif