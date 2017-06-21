using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
#if XB1
using System.Diagnostics;
#endif

namespace System.IO
{
    public static class IOExceptionExtensions
    {
        public static bool IsFileLocked(this System.IO.IOException e)
        {
#if XB1
			Debug.Assert(false, "Implement per platform");
			return false;
#else
            var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
#endif
        }
    }
}
