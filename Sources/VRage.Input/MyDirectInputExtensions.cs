using SharpDX;
#if !UNSHARPER
using SharpDX.DirectInput;
#endif
using VRage.Native;
using System.Diagnostics;

namespace VRage.Input
{
    static class MyDirectInputExtensions
    {
#if !UNSHARPER
		public static unsafe Result TryAcquire(this Device device)
        {
#if BLIT || BLITCREMENTAL
			Debug.Assert(false);
			return Result.False;
#else
			// Number 7 is offset in member function pointer table, it's same number as found in SharpDX.DirectInput.Device.Acquire()
            // It's unlikely to change, because it would make existing native apps not working
            return (Result)NativeCall<int>.Method(device.NativePointer, 7);
#endif
        }
#endif
    }
}
