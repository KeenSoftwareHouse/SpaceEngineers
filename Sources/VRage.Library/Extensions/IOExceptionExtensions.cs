using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public static class IOExceptionExtensions
    {
        public static bool IsFileLocked(this System.IO.IOException e)
        {
            var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }
    }
}
