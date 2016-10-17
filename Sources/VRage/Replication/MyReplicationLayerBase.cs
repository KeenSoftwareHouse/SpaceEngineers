using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Network;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace VRage.Network
{
    public abstract class MyReplicationLayerBase
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private bool m_registered = false;
#endif // XB1
        
        private static DBNull e = DBNull.Value;
        protected readonly MyTypeTable m_typeTable = new MyTypeTable();

        public Type GetType(TypeId id)
        {
            return m_typeTable.Get(id).Type;
        }

        public TypeId GetTypeId(Type id)
        {
            return m_typeTable.Get(id).TypeId;
        }

        public DateTime LastMessageFromServer { get; protected set; }

        protected static bool ShouldServerInvokeLocally(CallSite site, EndpointId? localClientEndpoint, EndpointId recipient)
        {
            // Invoke locally when:
            // A) Has server flag
            // B) Has client flag and recipient is serverEndpoint and server is also client (e.g. single player, normal server)
            // C) Has broadcast flag and recipient is serverEndpoint and server is also client (e.g. single player, normal server)
            return site.HasServerFlag || (localClientEndpoint.HasValue && recipient == localClientEndpoint.Value && (site.HasClientFlag || site.HasBroadcastFlag));
        }

        private bool TryGetInstanceCallSite<T>(Func<T, Delegate> callSiteGetter, T arg, out CallSite site)
        {
            var typeInfo = m_typeTable.Get(arg.GetType());
            return typeInfo.EventTable.TryGet(callSiteGetter, callSiteGetter, arg, out site);
        }

        private bool TryGetStaticCallSite<T>(Func<T, Delegate> callSiteGetter, out CallSite site)
        {
            return m_typeTable.StaticEventTable.TryGet(callSiteGetter, callSiteGetter, default(T), out site);
        }

        private CallSite GetCallSite<T>(Func<T, Delegate> callSiteGetter, T arg)
        {
            //Debug.Assert(callSiteGetter.Target == null, "RaiseEvent is designed for anonymous methods (no lambdas or instance methods)");

            CallSite site;
            if (arg == null)
                TryGetStaticCallSite(callSiteGetter, out site);
            else
                TryGetInstanceCallSite(callSiteGetter, arg, out site);

            if (site == null)
            {
                MethodInfo info = callSiteGetter(arg).Method;
                if (!info.HasAttribute<EventAttribute>())
                    throw new InvalidOperationException(String.Format("Event '{0}' in type '{1}' is missing attribute '{2}'", info.Name, info.DeclaringType.Name, typeof(EventAttribute).Name));
                else if (!info.DeclaringType.HasAttribute<StaticEventOwnerAttribute>() && !typeof(IMyEventProxy).IsAssignableFrom(info.DeclaringType) && !typeof(IMyNetObject).IsAssignableFrom(info.DeclaringType))
                    throw new InvalidOperationException(String.Format("Event '{0}' is defined in type '{1}', which does not implement '{2}' or '{3}' or has attribute '{4}'", info.Name, info.DeclaringType.Name, typeof(IMyEventOwner).Name, typeof(IMyNetObject).Name, typeof(StaticEventOwnerAttribute).Name));
                else
                    throw new InvalidOperationException(String.Format("Event '{0}' not found, is declaring type '{1}' registered within replication layer?", info.Name, info.DeclaringType.Name));
            }
            return site;
        }

        public void RaiseEvent<T1, T2>(T1 arg1, T2 arg2, Func<T1, Action> action, EndpointId endpointId = default(EndpointId), float unreliablePriority = 1)
            where T1 : IMyEventOwner
            where T2 : IMyEventOwner
        {
            DispatchEvent(GetCallSite(action, arg1), endpointId, unreliablePriority, ref arg1, ref e, ref e, ref e, ref e, ref e, ref e, ref arg2);
        }

        public void RaiseEvent<T1, T2, T3>(T1 arg1, T3 arg3, Func<T1, Action<T2>> action, T2 arg2, EndpointId endpointId = default(EndpointId), float unreliablePriority = 1)
            where T1 : IMyEventOwner
            where T3 : IMyEventOwner
        {
            DispatchEvent(GetCallSite(action, arg1), endpointId, unreliablePriority, ref arg1, ref arg2, ref e, ref e, ref e, ref e, ref e, ref arg3);
        }
        
        public void RaiseEvent<T1, T2, T3, T4>(T1 arg1, T4 arg4, Func<T1, Action<T2, T3>> action, T2 arg2, T3 arg3, EndpointId endpointId = default(EndpointId), float unreliablePriority = 1)
            where T1 : IMyEventOwner
            where T4 : IMyEventOwner
        {
            DispatchEvent(GetCallSite(action, arg1), endpointId, unreliablePriority, ref arg1, ref arg2, ref arg3, ref e, ref e, ref e, ref e, ref arg4);
        }

        public void RaiseEvent<T1, T2, T3, T4, T5>(T1 arg1, T5 arg5, Func<T1, Action<T2, T3, T4>> action, T2 arg2, T3 arg3, T4 arg4, EndpointId endpointId = default(EndpointId), float unreliablePriority = 1)
            where T1 : IMyEventOwner
            where T5 : IMyEventOwner
        {
            DispatchEvent(GetCallSite(action, arg1), endpointId, unreliablePriority, ref arg1, ref arg2, ref arg3, ref arg4, ref e, ref e, ref e, ref arg5);
        }

        public void RaiseEvent<T1, T2, T3, T4, T5, T6>(T1 arg1, T6 arg6, Func<T1, Action<T2, T3, T4, T5>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, EndpointId endpointId = default(EndpointId), float unreliablePriority = 1)
            where T1 : IMyEventOwner
            where T6 : IMyEventOwner
        {
            DispatchEvent(GetCallSite(action, arg1), endpointId, unreliablePriority, ref arg1, ref arg2, ref arg3, ref arg4, ref arg5, ref e, ref e, ref arg6);
        }

        public void RaiseEvent<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T7 arg7, Func<T1, Action<T2, T3, T4, T5, T6>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, EndpointId endpointId = default(EndpointId), float unreliablePriority = 1)
            where T1 : IMyEventOwner
            where T7 : IMyEventOwner
        {
            DispatchEvent(GetCallSite(action, arg1), endpointId, unreliablePriority, ref arg1, ref arg2, ref arg3, ref arg4, ref arg5, ref arg6, ref e, ref arg7);
        }

        public void RaiseEvent<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T8 arg8, Func<T1, Action<T2, T3, T4, T5, T6, T7>> action, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, EndpointId endpointId = default(EndpointId), float unreliablePriority = 1)
            where T1 : IMyEventOwner
            where T8 : IMyEventOwner
        {
            DispatchEvent(GetCallSite(action, arg1), endpointId, unreliablePriority, ref arg1, ref arg2, ref arg3, ref arg4, ref arg5, ref arg6, ref arg7, ref arg8);
        }

        protected abstract void DispatchEvent<T1, T2, T3, T4, T5, T6, T7, T8>(CallSite callSite, EndpointId recipient, float unreliablePriority, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8)
            where T1 : IMyEventOwner
            where T8 : IMyEventOwner;

        /// <summary>
        /// Invokes event locally without validation and with empty Sender and ClientData.
        /// </summary>
        internal void InvokeLocally<T1, T2, T3, T4, T5, T6, T7>(CallSite<T1, T2, T3, T4, T5, T6, T7> site, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            using (MyEventContext.Set(default(EndpointId), null, false))
            {
                site.Handler(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
        }

        public void RegisterFromAssembly(IEnumerable<Assembly> assemblies)
        {
            foreach (var item in assemblies)
            {
                RegisterFromAssembly(item);
            }
        }

        public void RegisterFromAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return;
            }
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (var type in MyAssembly.GetTypes())
#else // !XB1
            foreach (var type in assembly.GetTypes())
#endif // !XB1
            {
                if (MyTypeTable.ShouldRegister(type))
                {
                    m_typeTable.Register(type);
                }
            }
        }
    }
}
