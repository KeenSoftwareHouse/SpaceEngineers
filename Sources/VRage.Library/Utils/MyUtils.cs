using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Library.Utils
{
    public class MyLibraryUtils
    {
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void AssertBlittable<T>()
        {
            try
            {
                // Non-blittable types cannot allocate pinned handler
                if (default(T) != null) // This will rule out classes without constraint
                {
                    var handle = GCHandle.Alloc(default(T), GCHandleType.Pinned);
                    handle.Free();
                }
            }
            catch { }
            Debug.Fail("Type '" + typeof(T).Name + "' is not blittable");
        }

        public static void ThrowNonBlittable<T>()
        {
            try
            {
                // class test
                if (default(T) == null)
                {
                    throw new InvalidOperationException("Class is never blittable");
                }
                else
                {
                    // Non-blittable types cannot allocate pinned handler
                    var handle = GCHandle.Alloc(default(T), GCHandleType.Pinned);
                    handle.Free();
                }

            }
            catch(Exception e)
            {
                throw new InvalidOperationException("Type '" + typeof(T) + "' is not blittable", e);
            }
        }
    }
}
