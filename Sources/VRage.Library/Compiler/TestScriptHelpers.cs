using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public struct TestScriptHelpers
    {
#if XB1
		public static uint MessageBox(IntPtr hWndle, String text, String caption, int buttons)
		{
			return 0;
		}
#else
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint MessageBox(IntPtr hWndle, String text, String caption, int buttons);
#endif
        public static void DoEvilThings()
        {
            Debug.Fail("Evil thing happened!");
        }
    }
}
