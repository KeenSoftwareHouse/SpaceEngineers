#if !XB1
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Rpc
{
    public struct RpcProxy<T>
    {
        private static DBNull e = default(DBNull);

        private T m_state;
        private RpcDispatcher<T> m_dispatcher;

        public RpcProxy(T state, RpcDispatcher<T> dispatcher)
        {
            m_state = state;
            m_dispatcher = dispatcher;
        }
        
        public void Call(Action action)
        {
            m_dispatcher.Dispatch(m_dispatcher.GetCallSite(action), ref m_state, ref e, ref e, ref e, ref e, ref e, ref e, ref e, action.Target);
        }

        public void Call<T1>(Action<T1> action, T1 arg1)
        {
            m_dispatcher.Dispatch(m_dispatcher.GetCallSite(action), ref m_state, ref arg1, ref e, ref e, ref e, ref e, ref e, ref e, action.Target);
        }

        public void Call<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            m_dispatcher.Dispatch(m_dispatcher.GetCallSite(action), ref m_state, ref arg1, ref arg2, ref e, ref e, ref e, ref e, ref e, action.Target);
        }

        public void Call<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            m_dispatcher.Dispatch(m_dispatcher.GetCallSite(action), ref m_state, ref arg1, ref arg2, ref arg3, ref e, ref e, ref e, ref e, action.Target);
        }

        public void SCall<T1>(Func<Action<T1>> action, T1 arg1)
        {
            Debug.Assert(action.Target == null, "SCall is designed for anonymous or static methods (no lambdas or instance methods)");
            m_dispatcher.Dispatch(m_dispatcher.GetSCallSite(action), ref m_state, ref arg1, ref e, ref e, ref e, ref e, ref e, ref e, action.Target);
        }

        public void XCall<T1>(T1 arg1, Func<T1, Action> action)
        {
            Debug.Assert(action.Target == null, "XCall is designed for anonymous methods (no lambdas or instance methods)");
            m_dispatcher.Dispatch(m_dispatcher.GetXCallSite(action, arg1), ref m_state, ref arg1, ref e, ref e, ref e, ref e, ref e, ref e, action.Target);
        }

        public void XCall<T1, T2>(T1 arg1, Func<T1, Action<T2>> action, T2 arg2)
        {
            Debug.Assert(action.Target == null, "XCall is designed for anonymous methods (no lambdas or instance methods)");
            m_dispatcher.Dispatch(m_dispatcher.GetXCallSite(action, arg1), ref m_state, ref arg1, ref arg2, ref e, ref e, ref e, ref e, ref e, action.Target);
        }

        public void XCall<T1, T2, T3>(T1 arg1, Func<T1, Action<T2, T3>> action, T2 arg2, T3 arg3)
        {
            Debug.Assert(action.Target == null, "XCall is designed for anonymous methods (no lambdas or instance methods)");
            m_dispatcher.Dispatch(m_dispatcher.GetXCallSite(action, arg1), ref m_state, ref arg1, ref arg2, ref arg3, ref e, ref e, ref e, ref e, action.Target);
        }
    }
}
#endif // !XB1
