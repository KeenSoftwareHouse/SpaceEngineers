#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Native
{
    public static partial class NativeCall
    {
#if !UNSHARPER
        public static void Method(IntPtr instance, int methodOffset)
        {
            NativeCallHelper<Action<IntPtr, IntPtr>>.Invoke(NativeMethod.CalculateAddress(instance, methodOffset), instance);
        }

        public static void Method<TArg1>(IntPtr instance, int methodOffset, TArg1 arg1)
        {
            NativeCallHelper<Action<IntPtr, IntPtr, TArg1>>.Invoke(NativeMethod.CalculateAddress(instance, methodOffset), instance, arg1);
        }

        public static void Method<TArg1, TArg2>(IntPtr instance, int methodOffset, TArg1 arg1, TArg2 arg2)
        {
            NativeCallHelper<Action<IntPtr, IntPtr, TArg1, TArg2>>.Invoke(NativeMethod.CalculateAddress(instance, methodOffset), instance, arg1, arg2);
        }

        public static void Method<TArg1, TArg2, TArg3>(IntPtr instance, int methodOffset, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            NativeCallHelper<Action<IntPtr, IntPtr, TArg1, TArg2, TArg3>>.Invoke(NativeMethod.CalculateAddress(instance, methodOffset), instance, arg1, arg2, arg3);
        }

        public static void Method<TArg1, TArg2, TArg3, TArg4>(IntPtr instance, int methodOffset, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            NativeCallHelper<Action<IntPtr, IntPtr, TArg1, TArg2, TArg3, TArg4>>.Invoke(NativeMethod.CalculateAddress(instance, methodOffset), instance, arg1, arg2, arg3, arg4);
        }

        public static void Method<TArg1, TArg2, TArg3, TArg4, TArg5>(IntPtr instance, int methodOffset, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            NativeCallHelper<Action<IntPtr, IntPtr, TArg1, TArg2, TArg3, TArg4, TArg5>>.Invoke(NativeMethod.CalculateAddress(instance, methodOffset), instance, arg1, arg2, arg3, arg4, arg5);
        }

        public static void Method<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(IntPtr instance, int methodOffset, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            NativeCallHelper<Action<IntPtr, IntPtr, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>>.Invoke(NativeMethod.CalculateAddress(instance, methodOffset), instance, arg1, arg2, arg3, arg4, arg5, arg6);
        }
#endif
	}
}
#endif // !XB1
