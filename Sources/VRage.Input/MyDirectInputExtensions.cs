#if !XB1
using SharpDX;
using SharpDX.DirectInput;
using VRage.Native;
using System.Diagnostics;

namespace VRage.Input
{
    static class MyDirectInputExtensions
    {
		public static unsafe Result TryAcquire(this Device device)
        {
			// Number 7 is offset in member function pointer table, it's same number as found in SharpDX.DirectInput.Device.Acquire()
            // It's unlikely to change, because it would make existing native apps not working
            return (Result)NativeCall<int>.Method(device.NativePointer, 7);
        }
    }
}

#endif