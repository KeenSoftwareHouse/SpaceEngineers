#if !XB1
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Rpc
{
    public class CallSite<T1, T2, T3, T4, T5, T6, T7> : CallSite
    {
        public Action<T1, T2, T3, T4, T5, T6, T7> Handler;
        public Serializer<T1> Serializer1;
        public Serializer<T2> Serializer2;
        public Serializer<T3> Serializer3;
        public Serializer<T4> Serializer4;
        public Serializer<T5> Serializer5;
        public Serializer<T6> Serializer6;
        public Serializer<T7> Serializer7;
        public Serializer<object> SerializerTarget;

        public override void Build(MethodInfo info, ushort id, CallSiteCache cache)
        {
            Id = id;
            MethodInfo = info;

            Serializer1 = cache.GetSerializer<T1>();
            Serializer2 = cache.GetSerializer<T2>();
            Serializer3 = cache.GetSerializer<T3>();
            Serializer4 = cache.GetSerializer<T4>();
            Serializer5 = cache.GetSerializer<T5>();
            Serializer6 = cache.GetSerializer<T6>();
            Serializer7 = cache.GetSerializer<T7>();

            // TODO: Target
            //SerializerTarget = cache.GetSerializer(MethodInfo);

            var p = Arguments.Select(s => Expression.Parameter(s)).ToArray();
            
            Expression call;
            if (info.IsStatic)
            {
                call = Expression.Call(info, p.Where(s => s.Type != typeof(DBNull)).ToArray());
            }
            else
            {
                call = Expression.Call(p.First(), info, p.Skip(1).Where(s => s.Type != typeof(DBNull)).ToArray());
                //call = Expression.Call(fieldEx.First(), info, fieldEx.Skip(1).ToArray());
            }
            Handler = Expression.Lambda<Action<T1, T2, T3, T4, T5, T6, T7>>(call, p).Compile();
        }

        public void Serialize(BitStream output, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7)
        {
            var site = (CallSite)this;
            Serializer1.Write(output, ref arg1);
            Serializer2.Write(output, ref arg2);
            Serializer3.Write(output, ref arg3);
            Serializer4.Write(output, ref arg4);
            Serializer5.Write(output, ref arg5);
            Serializer6.Write(output, ref arg6);
            Serializer7.Write(output, ref arg7);
        }

        public override void Invoke(BitStream stream)
        {
            T1 arg1; T2 arg2; T3 arg3; T4 arg4; T5 arg5; T6 arg6; T7 arg7;
            Serializer1.Read(stream, out arg1);
            Serializer2.Read(stream, out arg2);
            Serializer3.Read(stream, out arg3);
            Serializer4.Read(stream, out arg4);
            Serializer5.Read(stream, out arg5);
            Serializer6.Read(stream, out arg6);
            Serializer7.Read(stream, out arg7);
            Handler(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }
}
#endif // !XB1
