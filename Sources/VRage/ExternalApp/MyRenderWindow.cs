using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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
        public Control Control;
        public Form TopLevelForm;

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
