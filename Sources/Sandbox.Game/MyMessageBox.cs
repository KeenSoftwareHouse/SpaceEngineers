using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Sandbox
{
    /// <summary>
    /// Represents possible values returned by the MessageBox function.
    /// </summary>
    public enum MessageBoxResult : uint
    {
        Ok = 1,
        Cancel,
        Abort,
        Retry,
        Ignore,
        Yes,
        No,
        Close,
        Help,
        TryAgain,
        Continue,
        Timeout = 32000
    }

    ///<summary>
    /// Flags that define appearance and behaviour of a standard message box displayed by a call to the MessageBox function.
    /// </summary>    
    [Flags]
    public enum MessageBoxOptions : uint
    {
        OkOnly = 0x000000,
        OkCancel = 0x000001,
        AbortRetryIgnore = 0x000002,
        YesNoCancel = 0x000003,
        YesNo = 0x000004,
        RetryCancel = 0x000005,
        CancelTryContinue = 0x000006,
        IconHand = 0x000010,
        IconQuestion = 0x000020,
        IconExclamation = 0x000030,
        IconAsterisk = 0x000040,
        UserIcon = 0x000080,
        DefButton2 = 0x000100,
        DefButton3 = 0x000200,
        DefButton4 = 0x000300,
        SystemModal = 0x001000,
        TaskModal = 0x002000,
        Help = 0x004000,
        NoFocus = 0x008000,
        SetForeground = 0x010000,
        DefaultDesktopOnly = 0x020000,
        Topmost = 0x040000,
        Right = 0x080000,
        RTLReading = 0x100000
    }

    // This class has to be here, in main app, it can't be in any lib!
    public static class MyMessageBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "MessageBox")]
        static extern MessageBoxResult Show(IntPtr hWnd, String text, String caption, int options);

        public static MessageBoxResult Show(IntPtr hWnd, String text, String caption, MessageBoxOptions options)
        {
            return Show(hWnd, text, caption, (int)options);
        }
    }
}
