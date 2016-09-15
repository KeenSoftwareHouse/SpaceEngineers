#if !XB1

using System;
using System.Drawing;
using System.Windows.Forms;
using SharpDX.Win32;
using SharpDX;
using SharpDX.Desktop;
using System.Collections.Generic;
using Sandbox.Engine.Utils;
using System.Runtime.InteropServices;
using VRage.Win32;
using System.Threading;
using VRage;
using Vector2 = VRageMath.Vector2;
using System.Diagnostics;
using VRageRender;
using VRage.Utils;
using VRageRender.ExternalApp;

namespace Sandbox.Engine.Platform
{
    /// <summary>
    /// No events like KeyDown, MouseMove... are raised.
    /// Activated and Deactivated is still raised.
    /// Prevents creating garbage.
    /// </summary>
    internal class GameWindowForm : Form, IMessageFilter, IMyBufferedInputSource
    {
        #region Fields

        private bool allowUserResizing;

        private MouseEventArgs m_emptyMouseEventArgs = new MouseEventArgs(0, 0, 0, 0, 0);

        #endregion

        /// <summary>
        /// Messages which are bypassed.
        /// These messages are handled by DefWindowProc before it arrives to window WndProc.
        /// It prevents allocations in System.Windows.Forms
        /// </summary>
        public HashSet<int> BypassedMessages { get; private set; }

        private FastResourceLock m_bufferedCharsLock = new FastResourceLock();
        private List<char> m_bufferedChars = new List<char>();
        private Vector2 m_mousePosition;

        public GameWindowForm()
            : this("VRage")
        {
        }

        public GameWindowForm(string text)
        {
            // By default, non resizable
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;

            BypassedMessages = new HashSet<int>();

            BypassedMessages.Add((int)WinApi.WM.KEYDOWN);
            BypassedMessages.Add((int)WinApi.WM.KEYUP);
            BypassedMessages.Add((int)WinApi.WM.CHAR);
            BypassedMessages.Add((int)WinApi.WM.DEADCHAR);
            BypassedMessages.Add((int)WinApi.WM.SYSKEYDOWN);
            BypassedMessages.Add((int)WinApi.WM.SYSKEYUP);
            BypassedMessages.Add((int)WinApi.WM.SYSCHAR);
            BypassedMessages.Add((int)WinApi.WM.SYSDEADCHAR);

            BypassedMessages.Add((int)WinApi.WM.MOUSEWHEEL);
            BypassedMessages.Add((int)WinApi.WM.MOUSEMOVE);
            BypassedMessages.Add((int)WinApi.WM.LBUTTONDOWN);
            BypassedMessages.Add((int)WinApi.WM.LBUTTONUP);
            BypassedMessages.Add((int)WinApi.WM.LBUTTONDBLCLK);
            BypassedMessages.Add((int)WinApi.WM.RBUTTONDOWN);
            BypassedMessages.Add((int)WinApi.WM.RBUTTONUP);
            BypassedMessages.Add((int)WinApi.WM.RBUTTONDBLCLK);
            BypassedMessages.Add((int)WinApi.WM.MBUTTONDOWN);
            BypassedMessages.Add((int)WinApi.WM.MBUTTONUP);
            BypassedMessages.Add((int)WinApi.WM.MBUTTONDBLCLK);
            BypassedMessages.Add((int)WinApi.WM.XBUTTONDBLCLK);
            BypassedMessages.Add((int)WinApi.WM.XBUTTONDOWN);
            BypassedMessages.Add((int)WinApi.WM.XBUTTONUP);

            BypassedMessages.Add((int)WinApi.WM.ERASEBKGND);
            BypassedMessages.Add((int)WinApi.WM.SHOWWINDOW);
            BypassedMessages.Add((int)WinApi.WM.ACTIVATE);
            BypassedMessages.Add((int)WinApi.WM.SETFOCUS);
            BypassedMessages.Add((int)WinApi.WM.KILLFOCUS);

            BypassedMessages.Add((int)WinApi.WM.IME_NOTIFY);
        }

        internal bool AllowUserResizing
        {
            get
            {
                return allowUserResizing;
            }
            set
            {
                if (allowUserResizing != value)
                {
                    allowUserResizing = value;
                    MaximizeBox = allowUserResizing;
                    FormBorderStyle = allowUserResizing ? FormBorderStyle.Sizable : FormBorderStyle.FixedSingle;
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            MessageFilterHook.AddMessageFilter(this.Handle, this);
            base.OnLoad(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            MessageFilterHook.RemoveMessageFilter(this.Handle, this);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            var size = Size;

            var screen = Screen.GetWorkingArea(this);

            var location = Location;

            if (size.Height > screen.Height)
            {
                Location = new System.Drawing.Point(Location.X, screen.Height - size.Height); 
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Because of ALT and its focus to menu
            if (m.Msg == (int)WinApi.WM.SYSKEYDOWN)
                return;

            if (m.Msg == (int)WinApi.WM.CHAR)
            {
                char input = (char)m.WParam;
                using (m_bufferedCharsLock.AcquireExclusiveUsing())
                {
                    m_bufferedChars.Add(input);
                }
                return;
            }

            if (m.Msg == (int)WinApi.WM.MOUSEMOVE)
            {
                m_mousePosition.X = unchecked((short)(long)m.LParam);
                m_mousePosition.Y = unchecked((short)((long)m.LParam >> 16));
            }

            base.WndProc(ref m);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == (int)WinApi.WM.MOUSEMOVE)
            {
                return false;
            }

            if (m.Msg == (int)WinApi.WM.CHAR)
            {
                return false;
            }

            if (m.Msg == (int)WinApi.WM.SYSKEYDOWN)
            {
                return true;
            }

            if (m.Msg == (int)WinApi.WM.SYSKEYUP)
            {
                return true;
            }

            if (m.Msg == (int)WinApi.WM.SYSCHAR)
            {
                return true;
            }

            if (m.Msg == (int)WinApi.WM.SYSDEADCHAR)
            {
                return true;
            }

            if (m.Msg == (int)WinApi.WM.KEYUP)
            {
                return true;
            }

            if (m.Msg == (int)WinApi.WM.KEYDOWN)
            {
                return true;
            }

            if (m.Msg == (int)WinApi.WM.SYSCOMMAND)
            {
                //http://msdn.microsoft.com/en-us/library/windows/desktop/ms646360(v=vs.85).aspx
                WinApi.SystemCommands correctWParam = (WinApi.SystemCommands)((int)m.WParam & 0xFFF0);

                if (correctWParam == WinApi.SystemCommands.SC_MOUSEMENU)
                    return true;
            }

            if (m.Msg == (int)WinApi.WM.NCRBUTTONDOWN)
            {
                return true;
            }

            if (m.Msg == (int)WinApi.WM.ACTIVATE)
            {
                MyRenderProxy.HandleFocusMessage(MyWindowFocusMessage.Activate);
            }

            if (m.Msg == (int)WinApi.WM.SETFOCUS)
            {
                MyRenderProxy.HandleFocusMessage(MyWindowFocusMessage.SetFocus);
            }

            if (BypassedMessages.Contains(m.Msg))
            {
                if (m.Msg == (int)WinApi.WM.ACTIVATE)
                {
                    if (m.WParam == IntPtr.Zero)
                        OnDeactivate(EventArgs.Empty);
                    else
                        OnActivated(EventArgs.Empty);
                }
                if (m.Msg == (int)WinApi.WM.MOUSEMOVE)
                {
                    OnMouseMove(m_emptyMouseEventArgs);
                }

                m.Result = WinApi.DefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam);
                return true;
            }

            return false;
        }

        void IMyBufferedInputSource.SwapBufferedTextInput(ref List<char> swappedBuffer)
        {
            swappedBuffer.Clear();
            using (m_bufferedCharsLock.AcquireExclusiveUsing())
            {
                var tmp = swappedBuffer;
                swappedBuffer = m_bufferedChars;
                m_bufferedChars = tmp;
            }
        }

        Vector2 IMyBufferedInputSource.MousePosition
        {
            get { return m_mousePosition; }
        }

        Vector2 IMyBufferedInputSource.MouseAreaSize 
        {
            get { return new Vector2(ClientSize.Width, ClientSize.Height); }
        }
    }
}

#endif