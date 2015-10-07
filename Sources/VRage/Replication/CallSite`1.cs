using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Serialization;

namespace VRage.Network
{
    public delegate void SerializeDelegate<T1, T2, T3, T4, T5, T6, T7>(T1 inst, BitStream stream, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7);

    class CallSite<T1, T2, T3, T4, T5, T6, T7> : CallSite
    {
        public readonly Action<T1, T2, T3, T4, T5, T6, T7> Handler;
        public readonly SerializeDelegate<T1, T2, T3, T4, T5, T6, T7> Serializer;
        public readonly Func<T1, T2, T3, T4, T5, T6, T7, bool> Validator;

        public CallSite(MySynchronizedTypeInfo owner, uint id, MethodInfo info, CallSiteFlags flags, Action<T1, T2, T3, T4, T5, T6, T7> handler,
            SerializeDelegate<T1, T2, T3, T4, T5, T6, T7> serializer, Func<T1, T2, T3, T4, T5, T6, T7, bool> validator)
            : base(owner, id, info, flags)
        {
            Handler = handler;
            Serializer = serializer;
            Validator = validator;
        }

        public override bool Invoke(BitStream stream, object obj, bool validate)
        {
            Debug.Assert(stream.Reading, "Expecting stream which supports reading");
            T1 arg1 = (T1)obj;
            T2 arg2 = default(T2);
            T3 arg3 = default(T3);
            T4 arg4 = default(T4);
            T5 arg5 = default(T5);
            T6 arg6 = default(T6);
            T7 arg7 = default(T7);
            Serializer(arg1, stream, ref arg2, ref arg3, ref arg4, ref arg5, ref arg6, ref arg7);
            if (validate && !Validator(arg1, arg2, arg3, arg4, arg5, arg6, arg7))
            {
                return false;
            }
            else
            {
                Handler(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return true;
            }
        }
    }
}
