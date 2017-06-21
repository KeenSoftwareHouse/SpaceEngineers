using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace VRage.Win32
{
    public static partial class WinApi
    {
#if !XB1
        public static void SendMessage<T>(ref T data, IntPtr windowHandle, IntPtr sourceWindowHandle = default(IntPtr))
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
                SendMessage(windowHandle, (uint)WM.COPYDATA, sourceWindowHandle, ref copyData);
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
#endif
    }
}
