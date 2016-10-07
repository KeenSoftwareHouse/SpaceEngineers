#if !XB1
using System;

namespace VRage.Utils
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Custom message box
    /// </summary>
    public static class MyMessageBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint MessageBox(IntPtr hWndle, String text, String caption, int buttons);

        public static void Show(string caption, string text)
        {
            MessageBox(new IntPtr(), text, caption, 0);
        }
    }
}
#endif // !XB1
