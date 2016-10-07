#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Win32;

namespace VRage.Service
{
    public delegate void ApplicationExitHandler(ref bool stopListening);

    public static class ExitListenerSTA
    {
        private static ApplicationExitHandler m_onExit;

        public static void Listen()
        {
            WinApi.MSG msg;

            if (WinApi.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0))
            {
                WinApi.TranslateMessage(ref msg);
                WinApi.DispatchMessage(ref msg);

                if (msg.message == 0x10 && Raise()) // WM_CLOSE
                {
                    return;
                }
            }
        }

        private static bool Raise()
        {
            bool stopListening = true;

            var handler = m_onExit;
            if (handler != null) handler(ref stopListening);

            return stopListening;
        }

        /// <summary>
        /// Raised when close message is sent to thread message pump (e.g. "taskkill.exe /im MyApp.exe")
        /// Use only in windowless applications!
        /// </summary>
        public static event ApplicationExitHandler OnExit
        {
            add { m_onExit += value; }
            remove { m_onExit -= value; }
        }
    }
}
#endif // !XB1
