using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Win32
{
#if !UNSHARPER

    public static partial class WinApi
    {
#if XB1 // from winapi-structures
		public delegate bool ConsoleEventHandler(CtrlType sig);
        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct MyCopyData
        {
            public IntPtr Data;
            public int DataSize;
            public IntPtr DataPointer;
        }

#if !XB1
        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            public const int CCHDEVICENAME = 32;
            public const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            [FieldOffset(0)]
            public string dmDeviceName;
            [FieldOffset(32)]
            public Int16 dmSpecVersion;
            [FieldOffset(34)]
            public Int16 dmDriverVersion;
            [FieldOffset(36)]
            public Int16 dmSize;
            [FieldOffset(38)]
            public Int16 dmDriverExtra;
            [FieldOffset(40)]
            public DM dmFields;

            [FieldOffset(44)]
            Int16 dmOrientation;
            [FieldOffset(46)]
            Int16 dmPaperSize;
            [FieldOffset(48)]
            Int16 dmPaperLength;
            [FieldOffset(50)]
            Int16 dmPaperWidth;
            [FieldOffset(52)]
            Int16 dmScale;
            [FieldOffset(54)]
            Int16 dmCopies;
            [FieldOffset(56)]
            Int16 dmDefaultSource;
            [FieldOffset(58)]
            Int16 dmPrintQuality;

            [FieldOffset(44)]
            public POINTL dmPosition;
            [FieldOffset(52)]
            public Int32 dmDisplayOrientation;
            [FieldOffset(56)]
            public Int32 dmDisplayFixedOutput;

            [FieldOffset(60)]
            public short dmColor; // See note below!
            [FieldOffset(62)]
            public short dmDuplex; // See note below!
            [FieldOffset(64)]
            public short dmYResolution;
            [FieldOffset(66)]
            public short dmTTOption;
            [FieldOffset(68)]
            public short dmCollate; // See note below!
            [FieldOffset(72)]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            [FieldOffset(102)]
            public Int16 dmLogPixels;
            [FieldOffset(104)]
            public Int32 dmBitsPerPel;
            [FieldOffset(108)]
            public Int32 dmPelsWidth;
            [FieldOffset(112)]
            public Int32 dmPelsHeight;
            [FieldOffset(116)]
            public Int32 dmDisplayFlags;
            [FieldOffset(116)]
            public Int32 dmNup;
            [FieldOffset(120)]
            public Int32 dmDisplayFrequency;
        }
#endif // !XB1

        public struct POINTL
        {
            public Int32 x;
            public Int32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public UInt32 message;
            public IntPtr wParam;
            public IntPtr lParam;
            public UInt32 time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DeviceChangeHookStruct
        {
            public int lParam;
            public int wParam;
            public int message;
            public int hwnd;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

		// From Winapi-Helpers
#if !XB1
		public static void SendMessage<T>(ref T data, IntPtr windowHandle)
				   where T : struct
		{
			try
			{
				int structSize = Marshal.SizeOf(data);
				IntPtr structPtr = Marshal.AllocHGlobal(structSize);
				Marshal.StructureToPtr(data, structPtr, true);
				WinApi.MyCopyData copyData = new WinApi.MyCopyData();
				copyData.DataSize = structSize;
				copyData.DataPointer = structPtr;
				SendMessage(windowHandle, (uint)WM.COPYDATA, IntPtr.Zero, ref copyData);
				Marshal.FreeHGlobal(structPtr);
			}
			catch (Exception)
			{

			}
		}

		public static IntPtr FindWindowInParent(string parentName, string childName)
		{
			IntPtr parent = FindWindow(null, parentName);
			if (parent != IntPtr.Zero)
			{
				return FindChildWindow(parent, childName);
			}
			return IntPtr.Zero;
		}

		public static IntPtr FindChildWindow(IntPtr windowHandle, string childName)
		{
			IntPtr child = FindWindowEx(windowHandle, IntPtr.Zero, null, childName);
			var int322 = windowHandle.ToInt32();
			if (child != IntPtr.Zero)
			{
				return child;
			}
			else
			{
				IntPtr firstChild = IntPtr.Zero;
				child = GetWindow(windowHandle, GW_CHILD);
				while (firstChild != child && child != IntPtr.Zero)
				{
					if (firstChild == IntPtr.Zero)
						firstChild = child;
					IntPtr retChild = FindChildWindow(child, childName);
					if (retChild != IntPtr.Zero)
						return retChild;
					child = GetWindow(child, GW_HWNDNEXT);
				}
				return IntPtr.Zero;
			}
		}

		public static bool IsValidWindow(IntPtr windowHandle)
		{
			return IsWindow(windowHandle);
		}

		static Func<long> m_workingSetDelegate;

		/// <summary>
		/// Gets working set size without creating garbage, it's also faster.
		/// Environment.WorkingSet create garbage.
		/// </summary>
		public static long WorkingSet
		{
			get
			{
				if (m_workingSetDelegate == null)
				{
					// To properly initialize security permission
					long testVal = Environment.WorkingSet;

					// Avoids allocation (internally WorkingSet property allocates during each call and is slower than this).
					var info = typeof(System.Environment).GetMethod("GetWorkingSet", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
					m_workingSetDelegate = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), info);
				}
				return m_workingSetDelegate();
			}
		}
#endif // !XB1

#endif
#if !XB1
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
#endif // !XB1

#if !XB1
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
#endif // !XB1

#if !XB1
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
#endif // !XB1

    }
#elif false
	[Unsharper.UnsharperDisableReflection()]
	public static partial class WinApi
	{

		[DllImport("user32.dll")]
		public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
	}
#endif	
}
