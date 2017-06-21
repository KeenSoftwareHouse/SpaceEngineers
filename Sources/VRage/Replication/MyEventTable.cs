using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;
using VRage.Serialization;

namespace VRage.Network
{
    public class MyEventTable
    {
        MethodInfo m_createCallSite = typeof(MyEventTable).GetMethod("CreateCallSite", BindingFlags.Instance | BindingFlags.NonPublic);

        Dictionary<uint, CallSite> m_idToEvent;
        Dictionary<MethodInfo, CallSite> m_methodInfoLookup;
        Dictionary<object, CallSite> m_associateObjectLookup;

        public readonly MySynchronizedTypeInfo Type;

        public int Count { get { return m_idToEvent.Count; } }

        public MyEventTable(MySynchronizedTypeInfo type)
        {
            Type = type;

            // Copy base type event table so we don't have to crawl hierarchy during every event invocation.
            if (type != null && type.BaseType != null)
            {
                m_idToEvent = new Dictionary<uint, CallSite>(type.BaseType.EventTable.m_idToEvent);
                m_methodInfoLookup = new Dictionary<MethodInfo, CallSite>(type.BaseType.EventTable.m_methodInfoLookup);
                m_associateObjectLookup = new Dictionary<object, CallSite>(type.BaseType.EventTable.m_associateObjectLookup);
            }
            else
            {
                m_idToEvent = new Dictionary<uint, CallSite>();
                m_methodInfoLookup = new Dictionary<MethodInfo, CallSite>();
                m_associateObjectLookup = new Dictionary<object, CallSite>();
            }

            // Register event from this type
            if (Type != null)
            {
                RegisterEvents();
            }
        }

        public CallSite Get(uint id)
        {
            return m_idToEvent[id];
        }

        public CallSite Get<T>(object associatedObject, Func<T, Delegate> getter, T arg)
        {
            CallSite result;
            if (!m_associateObjectLookup.TryGetValue(associatedObject, out result))
            {
                var method = getter(arg).Method;
                result = m_methodInfoLookup[method];
                m_associateObjectLookup[associatedObject] = result;
            }
            return result;
        }

        public bool TryGet<T>(object associatedObject, Func<T, Delegate> getter, T arg, out CallSite site)
        {
            if (!m_associateObjectLookup.TryGetValue(associatedObject, out site))
            {
                var method = getter(arg).Method;
                if (m_methodInfoLookup.TryGetValue(method, out site))
                {
                    m_associateObjectLookup[associatedObject] = site;
                    return true;
                }
                return false;
            }
            return true;
        }

        public void AddStaticEvents(Type fromType)
        {
            RegisterEvents(fromType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Static);
        }

        void RegisterEvents()
        {
            // Static events are registered separately into standalone table
            RegisterEvents(Type.Type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
        }

		class EventReturn
		{
			public MethodInfo Method;
			public EventAttribute Event;

			public EventReturn(EventAttribute _event, MethodInfo _method)
			{
				Event = _event;
				Method = _method;
			}
		};

        void RegisterEvents(Type type, BindingFlags flags)
        {
#if !XB1_NOMULTIPLAYER
            var query = type.GetMethods(flags)
                .Select(s => new EventReturn(s.GetCustomAttribute<EventAttribute>(), s))
                .Where(s => s.Event != null)
                .OrderBy(s => s.Event.Order);
            
            foreach (var item in query)
            {
                var m = item.Method;
                Type instanceType = m.IsStatic ? typeof(IMyEventOwner) : m.DeclaringType;
                Type[] args = new Type[7] { instanceType, typeof(DBNull), typeof(DBNull), typeof(DBNull), typeof(DBNull), typeof(DBNull), typeof(DBNull) };
                var parameters = m.GetParameters();
                Debug.Assert(parameters.Length <= 6, "Max 6 arguments is supported for events");
                for (int i = 0; i < parameters.Length; i++)
                {
                    args[i + 1] = parameters[i].ParameterType;
                }

                CallSite site = (CallSite)m_createCallSite.MakeGenericMethod(args).Invoke(this, new object[] { m, (uint)m_idToEvent.Count });

                int num = (site.HasBroadcastExceptFlag ? 1 : 0) + (site.HasBroadcastFlag ? 1 : 0) + (site.HasClientFlag ? 1 : 0);
                if (num > 1)
                    throw new InvalidOperationException(String.Format("Event '{0}' can have only one of [Client], [Broadcast], [BroadcastExcept] attributes", site));

                m_idToEvent.Add(site.Id, site);
                m_methodInfoLookup.Add(m, site);
            }
#endif // XB1_NOMULTIPLAYER
        }

        CallSite CreateCallSite<T1, T2, T3, T4, T5, T6, T7>(MethodInfo info, uint id)
        {

#if UNSHARPER_TMP
			Debug.Assert(false, "To implement expression");
			return null;
#else
#if !XB1_NOMULTIPLAYER
            Type[] arguments = new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) };
            var p = arguments.Select(s => Expression.Parameter(s)).ToArray();

            Expression call;
            if(info.IsStatic)
                call = Expression.Call(info, p.Skip(1).Where(s => s.Type != typeof(DBNull)).ToArray());
            else
                call = Expression.Call(p.First(), info, p.Skip(1).Where(s => s.Type != typeof(DBNull)).ToArray());
            var handler = Expression.Lambda<Action<T1, T2, T3, T4, T5, T6, T7>>(call, p).Compile();

            var eventAttribute = info.GetCustomAttribute<EventAttribute>();
            var serverAttribute = info.GetCustomAttribute<ServerAttribute>();

            CallSiteFlags flags = CallSiteFlags.None;
            if (serverAttribute != null) flags |= CallSiteFlags.Server;
            if (info.HasAttribute<ClientAttribute>()) flags |= CallSiteFlags.Client;
            if (info.HasAttribute<BroadcastAttribute>()) flags |= CallSiteFlags.Broadcast;
            if (info.HasAttribute<BroadcastExceptAttribute>()) flags |= CallSiteFlags.BroadcastExcept;
            if (info.HasAttribute<ReliableAttribute>()) flags |= CallSiteFlags.Reliable;
            if (info.HasAttribute<RefreshReplicableAttribute>()) flags |= CallSiteFlags.RefreshReplicable;
            if (info.HasAttribute<BlockingAttribute>()) flags |= CallSiteFlags.Blocking;

            SerializeDelegate<T1, T2, T3, T4, T5, T6, T7> serializer = null;
            Func<T1, T2, T3, T4, T5, T6, T7, bool> validator = null;

            if (eventAttribute.Serialization != null)
            {
                var method = info.DeclaringType.GetMethod(eventAttribute.Serialization, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (method == null)
                    throw new InvalidOperationException(String.Format("Serialization method '{0}' for event '{1}' defined by type '{2}' not found", eventAttribute.Serialization, info.Name, info.DeclaringType.Name));
                if (!method.GetParameters().Skip(1).All(s => s.ParameterType.IsByRef))
                    throw new InvalidOperationException(String.Format("Serialization method '{0}' for event '{1}' defined by type '{2}' must have all arguments passed with 'ref' keyword (except BitStream)", eventAttribute.Serialization, info.Name, info.DeclaringType.Name));
                var args = MethodInfoExtensions.ExtractParameterExpressionsFrom<SerializeDelegate<T1, T2, T3, T4, T5, T6, T7>>();
                var c = Expression.Call(args.First(), method, args.Skip(1).Where(s => s.Type != typeof(DBNull)).ToArray());
                serializer = Expression.Lambda<SerializeDelegate<T1, T2, T3, T4, T5, T6, T7>>(c, args).Compile();
            }

            if (serverAttribute != null && serverAttribute.Validation != null)
            {
                var method = info.DeclaringType.GetMethod(serverAttribute.Validation, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (method == null)
                    throw new InvalidOperationException(String.Format("Validation method '{0}' for event '{1}' defined by type '{2}' not found", serverAttribute.Validation, info.Name, info.DeclaringType.Name));
                var args = MethodInfoExtensions.ExtractParameterExpressionsFrom<Func<T1, T2, T3, T4, T5, T6, T7, bool>>();
                var c = Expression.Call(args.First(), method, args.Skip(1).Where(s => s.Type != typeof(DBNull)).ToArray());
                validator = Expression.Lambda<Func<T1, T2, T3, T4, T5, T6, T7, bool>>(c, args).Compile();
            }

            serializer = serializer ?? CreateSerializer<T1, T2, T3, T4, T5, T6, T7>(info);
            validator = validator ?? CreateValidator<T1, T2, T3, T4, T5, T6, T7>();

            return new CallSite<T1, T2, T3, T4, T5, T6, T7>(Type, id, info, flags, handler, serializer, validator);
#else // XB1_NOMULTIPLAYER
            return null;
#endif // XB1_NOMULTIPLAYER
#endif
        }

        SerializeDelegate<T1, T2, T3, T4, T5, T6, T7> CreateSerializer<T1, T2, T3, T4, T5, T6, T7>(MethodInfo info)
        {
            // TODO: Use method info to get parameter attributes and create MySerializeInfo from them

            var s2 = MyFactory.GetSerializer<T2>();
            var s3 = MyFactory.GetSerializer<T3>();
            var s4 = MyFactory.GetSerializer<T4>();
            var s5 = MyFactory.GetSerializer<T5>();
            var s6 = MyFactory.GetSerializer<T6>();
            var s7 = MyFactory.GetSerializer<T7>();

            var args = info.GetParameters();
            var info2 = MySerializeInfo.CreateForParameter(args, 0);
            var info3 = MySerializeInfo.CreateForParameter(args, 1);
            var info4 = MySerializeInfo.CreateForParameter(args, 2);
            var info5 = MySerializeInfo.CreateForParameter(args, 3);
            var info6 = MySerializeInfo.CreateForParameter(args, 4);
            var info7 = MySerializeInfo.CreateForParameter(args, 5);

            return delegate(T1 inst, BitStream stream, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7)
            {
                if (stream.Reading)
                {
                    MySerializationHelpers.CreateAndRead(stream, out arg2, s2, info2);
                    MySerializationHelpers.CreateAndRead(stream, out arg3, s3, info3);
                    MySerializationHelpers.CreateAndRead(stream, out arg4, s4, info4);
                    MySerializationHelpers.CreateAndRead(stream, out arg5, s5, info5);
                    MySerializationHelpers.CreateAndRead(stream, out arg6, s6, info6);
                    MySerializationHelpers.CreateAndRead(stream, out arg7, s7, info7);
    
                }
                else
                {
                    MySerializationHelpers.Write(stream, ref arg2, s2, info2);
                    MySerializationHelpers.Write(stream, ref arg3, s3, info3);
                    MySerializationHelpers.Write(stream, ref arg4, s4, info4);
                    MySerializationHelpers.Write(stream, ref arg5, s5, info5);
                    MySerializationHelpers.Write(stream, ref arg6, s6, info6);
                    MySerializationHelpers.Write(stream, ref arg7, s7, info7);
                }
            };
        }

        Func<T1, T2, T3, T4, T5, T6, T7, bool> CreateValidator<T1, T2, T3, T4, T5, T6, T7>()
        {
            return (a1, a2, a3, a4, a5, a6, a7) => true;
        }
    }
}
