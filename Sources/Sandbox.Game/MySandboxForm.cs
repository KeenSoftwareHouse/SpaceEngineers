#if !XB1

using Sandbox.Engine.Platform;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VRage;
using VRage.Utils;
using VRage.Win32;
using VRageMath;
using VRageRender.ExternalApp;

namespace Sandbox
{
    class MySandboxForm : GameWindowForm, IMyRenderWindow
    {
        private bool m_showCursor = true;
        private bool m_isCursorVisible = true;

        private bool m_captureMouse = true;

        private bool IsCursorVisible
        {
            get { return m_isCursorVisible; }
            set
            {
                if (!m_isCursorVisible && value)
                {
                    Cursor.Show();
                    m_isCursorVisible = value;
                }
                else if (m_isCursorVisible && !value)
                {
                    Cursor.Hide();
                    m_isCursorVisible = value;
                }
            }
        }

        public bool IsActive { get; private set; }

        public bool ShowCursor
        {
            get
            {
                return m_showCursor;
            }
            set
            {
                if (m_showCursor != value)
                {
                    m_showCursor = value;
                    IsCursorVisible = value;
                }
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            if (!IsActive)
            {
                IsActive = true;
                if (!ShowCursor)
                    IsCursorVisible = false;
            }
            base.OnActivated(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            if (IsActive)
            {
                IsActive = false;
                ClearClip();
                if (!IsCursorVisible)
                    IsCursorVisible = true;
            }
            base.OnDeactivate(e);
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            ClearClip();
            base.OnResizeBegin(e);
        }

        private void SetClip()
        {
            Cursor.Clip = this.RectangleToScreen(ClientRectangle);
        }

        private static void ClearClip()
        {
            Cursor.Clip = System.Drawing.Rectangle.Empty;
        }

        public void UpdateClip()
        {
            //GR: Catch exception when closing. Happens on synchronized rendering
            try
            {
                MySandboxGame.GameWindowHandle = Handle;
            }
            catch (ObjectDisposedException)
            {
                MySandboxGame.ExitThreadSafe();
                return;
            }
            // TODO: OP! Some old implementation, try finding something more safe
            Control c = Control.FromHandle(WinApi.GetForegroundWindow());

            bool isActive = false;

            if (c != null)
            {
                isActive = !c.TopLevelControl.InvokeRequired &&
                           Handle == c.TopLevelControl.Handle;
            }

            isActive = isActive && (m_captureMouse || !m_isCursorVisible);

            if (isActive)
                SetClip();
            else
                ClearClip();
        }

        public bool DrawEnabled
        {
            get { return WindowState != FormWindowState.Minimized; }
        }

        public void SetMouseCapture(bool capture)
        {
            m_captureMouse = capture;
            UpdateClip();
        }

        public void OnModeChanged(VRageRender.MyWindowModeEnum windowMode, int width, int height)
        {
            if (windowMode == VRageRender.MyWindowModeEnum.Window)
            {
                FormBorderStyle = FormBorderStyle.FixedSingle;
                TopMost = false;
            }
            else if (windowMode == VRageRender.MyWindowModeEnum.FullscreenWindow)
            {
                FormBorderStyle = FormBorderStyle.None;
                TopMost = false; // false for fullscreen window, shouldn't matter for true fullscren
                SizeGripStyle = SizeGripStyle.Hide;
            }
            else if(windowMode == VRageRender.MyWindowModeEnum.Fullscreen)
            {
                FormBorderStyle = FormBorderStyle.None;
                // Fullscreen used to have same settings as FullscreenWindow, but Dx11 render required change for Shadow Play to work.
                // It still seems like TopMost and SizeGripStyle should carry over from other branches.
            }

            ClientSize = new System.Drawing.Size(width, height);

            WinApi.DEVMODE mode = new WinApi.DEVMODE();
            WinApi.EnumDisplaySettings(null, WinApi.ENUM_CURRENT_SETTINGS, ref mode);
            VRage.Trace.MyTrace.Watch("Current display settings", string.Format("{0}x{1}", mode.dmPelsWidth, mode.dmPelsHeight));
            if (MyFakes.MOVE_WINDOW_TO_CORNER)
            {
                Location = new System.Drawing.Point(mode.dmPelsWidth - width, 0);
            }
            else
            {
                Location = new System.Drawing.Point(mode.dmPelsWidth / 2 - width / 2, mode.dmPelsHeight / 2 - height / 2);
            }

            // TODO: OP! Should be on different place
            Show();
            Activate();

            MySandboxGame.Static.UpdateMouseCapture();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MySandboxForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Name = "MySandboxForm";
            this.ResumeLayout(false);
        }

        public void BeforeDraw()
        {
            UpdateClip();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            MyMessageLoop.AddMessage(ref m);
        }
    }
}

#endif