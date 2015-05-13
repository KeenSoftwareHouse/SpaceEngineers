using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Win32
{
    public static partial class WinApi
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref MyCopyData lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(ConsoleEventHandler handler, bool add);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("user32.dll")]
        public static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern int GetMenuItemCount(IntPtr hMenu);

        [DllImport("user32.dll")]
        public static extern bool DrawMenuBar(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint MessageBox(IntPtr hWndle, String text, String caption, int buttons);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);

        // Install a thread-specific or global hook.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        // Uninstall a hook.
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        // Call the next hook in the hook sequence.
        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
        internal extern static uint MapVirtualKeyEx(uint key, MAPVK mappingType, IntPtr keyboardLayout);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static IntPtr LoadKeyboardLayout(string keyboardLayoutID, uint flags);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool UnloadKeyboardLayout(IntPtr handle);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
        public extern static IntPtr GetKeyboardLayout(IntPtr threadId);

        [DllImport("kernel32.dll")]
        internal extern static IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32")]
        public static extern bool SetProcessWorkingSetSize(IntPtr handle, int minSize, int maxSize);

#if PROFILING
        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(out long perfcount);

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceFrequency(out long freq);
#endif //PROFILING

        // marshaling system functions:
        // For getting system options:
        [DllImport("ntdll.dll", EntryPoint = "NtQueryTimerResolution")]
        public static extern NTSTATUS NtQueryTimerResolution(ref uint MinimumResolution, ref uint MaximumResolution, ref uint CurrentResolution);

        // For setting system options:
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern NTSTATUS NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);

    }
}
