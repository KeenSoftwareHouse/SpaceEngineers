#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Library.Utils
{
    public static class BlittableHelper<T>
    {
        public static readonly bool IsBlittable;

        static BlittableHelper()
        {
            try
            {
                // Class test
                if (default(T) != null)
                {
                    // Non-blittable types cannot allocate pinned handle
                    GCHandle.Alloc(default(T), GCHandleType.Pinned).Free();
                    IsBlittable = true;
                }
            }
            catch { }
        }
    } 
}
#endif // !XB1
