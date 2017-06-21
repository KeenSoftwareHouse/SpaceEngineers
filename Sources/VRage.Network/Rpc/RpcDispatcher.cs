#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Rpc
{
    public delegate void RpcTransmitter<TState>(ref TState state, BitStream output);

    public class RpcDispatcher<TState>
    {
        private BitStream m_sendStream = new BitStream();

        public CallSiteCache Cache;
        public RpcTransmitter<TState> Transmitter;

        public CallSite GetCallSite(Delegate callSite)
        {
            return Cache[callSite.Method];
        }

        public CallSite GetSCallSite(Func<Delegate> callSiteGetter)
        {
            return Cache.Get(callSiteGetter, callSiteGetter);
        }

        public CallSite GetXCallSite<T>(Func<T, Delegate> callSiteGetter, T arg)
        {
            if (arg == null)
                throw new InvalidOperationException("Target of XCall cannot be null");
            return Cache.Get(callSiteGetter, callSiteGetter, arg);
        }

        public void Dispatch<T1, T2, T3, T4, T5, T6, T7>(CallSite callSite, ref TState state, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, object target = null)
        {
            //if (!callSite.MethodInfo.IsStatic && target == null)
                //throw new InvalidOperationException(String.Format("Method {0}.{1} is not static, but null was provided as instance", callSite.MethodInfo.DeclaringType.Name, callSite.MethodInfo.Name));

            m_sendStream.ResetWrite();
            m_sendStream.WriteUInt16(callSite.Id);
            ((CallSite<T1, T2, T3, T4, T5, T6, T7>)callSite).Serialize(m_sendStream, ref arg1, ref arg2, ref arg3, ref arg4, ref arg5, ref arg6, ref arg7);
            Transmitter(ref state, m_sendStream);
        }

        public void Invoke(BitStream stream)
        {
            ushort id = stream.ReadUInt16();
            var callSite = Cache[id];
            callSite.Invoke(stream);
        }
    }
}
#endif // !XB1
