﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !BLIT
using System.Windows.Forms;
#endif
using VRageMath;

namespace VRage
{

    public interface IExternalApp
    {
        void Draw();
        void Update();
        void UpdateMainThread();
    }

    public class MyRenderWindow : IMyRenderWindow, IMyBufferedInputSource
    {
#if !BLIT
        public Control Control;
        public Form TopLevelForm;
#else
		public SharpDX.Windows.RenderForm Control;
#endif

        private FastResourceLock m_bufferedCharsLock = new FastResourceLock();
        private List<char> m_bufferedChars = new List<char>();

        public bool DrawEnabled
        {
            get { return true; }
        }

        public IntPtr Handle
        {
            get { return Control.Handle; }
        }

        public void BeforeDraw()
        {
        }

        public void SetMouseCapture(bool capture)
        {
            if (capture)
            {
                Cursor.Clip = Control.RectangleToScreen(Control.ClientRectangle);
                Cursor.Hide();

            }
            else
            {
                Cursor.Clip = new System.Drawing.Rectangle(0, 0, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);                
                Cursor.Show();
            }
        }

        public void OnModeChanged(VRageRender.MyWindowModeEnum mode, int width, int height)
        {
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
            get { return new Vector2(); }
        }

        Vector2 IMyBufferedInputSource.MouseAreaSize
        {
            get { return new Vector2(Control.ClientSize.Width, Control.ClientSize.Height); }
        }
    }

}
