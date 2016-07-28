#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Native
{
#if !UNSHARPER
    public class NativeCallHelper<TDelegate>
            where TDelegate : class
    {
        public static readonly TDelegate Invoke = Create();

        static TDelegate Create()
        {
            var t = typeof(TDelegate);
            var invoke = t.GetMethod("Invoke");

            Type[] parameters = invoke.GetParameters().Select(s => s.ParameterType).ToArray();

            if (parameters.Length == 0 || parameters[0] != typeof(IntPtr))
                throw new InvalidOperationException("First parameter must be function pointer");

            // Parameter 0: pointer
            // Parameter 1: instance (optional)
            // Parameter x: arguments

            var callParameters = parameters.Skip(1).Select(s => s == typeof(IntPtr) ? typeof(void*) : s).ToArray();

            DynamicMethod m = new DynamicMethod(String.Empty, invoke.ReturnType, parameters, Assembly.GetExecutingAssembly().ManifestModule);
            var gen = m.GetILGenerator();

            // Args
            for (int i = 1; i < parameters.Length; i++)
            {
                gen.Emit(OpCodes.Ldarg, i);
            }

            // Function pointer
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldind_I);

            // Call
            gen.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, invoke.ReturnType, callParameters);
            gen.Emit(OpCodes.Ret);
            return m.CreateDelegate<TDelegate>();
        }
    }
#endif
}

#endif // !XB1
