#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Native
{
#if !UNSHARPER
    public static partial class NativeCall<TResult>
    {
        public static TResult Function(IntPtr address)
        {
            return NativeCallHelper<Func<IntPtr, TResult>>.Invoke(address);
        }

        public static TResult Function<TArg1>(IntPtr address, TArg1 arg1)
        {
            return NativeCallHelper<Func<IntPtr, TArg1, TResult>>.Invoke(address, arg1);
        }

        public static TResult Function<TArg1, TArg2>(IntPtr address, TArg1 arg1, TArg2 arg2)
        {
            return NativeCallHelper<Func<IntPtr, TArg1, TArg2, TResult>>.Invoke(address, arg1, arg2);
        }

        public static TResult Function<TArg1, TArg2, TArg3>(IntPtr address, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return NativeCallHelper<Func<IntPtr, TArg1, TArg2, TArg3, TResult>>.Invoke(address, arg1, arg2, arg3);
        }

        public static TResult Function<TArg1, TArg2, TArg3, TArg4>(IntPtr address, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return NativeCallHelper<Func<IntPtr, TArg1, TArg2, TArg3, TArg4, TResult>>.Invoke(address, arg1, arg2, arg3, arg4);
        }

        public static TResult Function<TArg1, TArg2, TArg3, TArg4, TArg5>(IntPtr address, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return NativeCallHelper<Func<IntPtr, TArg1, TArg2, TArg3, TArg4, TArg5, TResult>>.Invoke(address, arg1, arg2, arg3, arg4, arg5);
        }

        public static TResult Function<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(IntPtr address, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return NativeCallHelper<Func<IntPtr, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>>.Invoke(address, arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }
#endif
}

#endif // !XB1
