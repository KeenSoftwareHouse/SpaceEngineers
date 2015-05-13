using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Native
{
    public static class NativeMethod
    {
        public static unsafe IntPtr CalculateAddress(IntPtr instance, int methodOffset)
        {
            return *(IntPtr*)instance.ToPointer() + methodOffset * sizeof(void*);
        }
    }
}
