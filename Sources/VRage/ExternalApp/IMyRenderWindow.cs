using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    public interface IMyRenderWindow
    {
        /// <summary>
        /// True when Present on device should be called (e.g. window not minimized)
        /// </summary>
        bool DrawEnabled { get; }

        /// <summary>
        /// Target window handle
        /// </summary>
        IntPtr Handle { get; }

        void BeforeDraw();

        void SetMouseCapture(bool capture);

        /// <summary>
        /// Called by render when display mode has changed
        /// </summary>
        void OnModeChanged(VRageRender.MyWindowModeEnum mode, int width, int height);
    }
}
