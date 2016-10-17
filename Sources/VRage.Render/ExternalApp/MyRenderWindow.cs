using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VRage;
using VRageMath;
#if !XB1
#endif

namespace VRageRender.ExternalApp
{

    public interface IExternalApp
    {
        void Draw();
        void Update();
        void UpdateMainThread();
    }

    public class MyRenderWindow : IMyRenderWindow, IMyBufferedInputSource
    {
#if !XB1
        public Control Control;
        public Form TopLevelForm;
#else
		public SharpDX.Windows.RenderForm Control;
#endif

        private FastResourceLock m_bufferedCharsLock = new FastResourceLock();
        private List<char> m_bufferedChars = new List<char>();

        public void AddChar(char ch)
        {
            m_bufferedChars.Add(ch);
        }

        public bool DrawEnabled
        {
            get { return true; }
        }

        public IntPtr Handle
        {
#if !XB1
            get { return Control.Handle; }
#else // XB1
            get
            {
                System.Diagnostics.Debug.Assert(false);
                return IntPtr.Zero;
            }
#endif // XB1
        }

        public void BeforeDraw()
        {
        }

        public void SetMouseCapture(bool capture)
        {
#if !XB1
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
#endif
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
#if !XB1
            get { return new Vector2(Control.ClientSize.Width, Control.ClientSize.Height); }
#else // XB1
            get
            {
                System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
                return new Vector2();//TODO
            }
#endif // XB1
        }
    }

}
