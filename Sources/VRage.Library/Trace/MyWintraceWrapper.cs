#if !XB1
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using VRage.FileSystem;

namespace VRage.Trace
{

#if !UNSHARPER
    class MyWintraceWrapper : ITrace
    {
        static Type m_winTraceType;
        static object m_winWatches;

        object m_trace;
        Action m_clearAll;
        Action<string, object> m_send;
        Action<string, string> m_debugSend;

        static MyWintraceWrapper()
        {
            Assembly assembly = TryLoad("TraceTool.dll") ?? TryLoad(MyFileSystem.ExePath + "/../../../../../../3rd/TraceTool/TraceTool.dll");

            if (assembly != null)
            {
                m_winTraceType = assembly.GetType("TraceTool.WinTrace");

                var ttraceType = assembly.GetType("TraceTool.TTrace");
                m_winWatches = ttraceType.GetProperty("Watches").GetGetMethod().Invoke(null, new object[] { });
            }
        }

        static Assembly TryLoad(string assembly)
        {
            if (!File.Exists(assembly))
            {
                return null;
            }

            try
            {
                return Assembly.LoadFrom(assembly);
            }
            catch (Exception) { return null; }
        }

        public static ITrace CreateTrace(string id, string name)
        {
            if (m_winTraceType != null)
            {
                return new MyWintraceWrapper(Activator.CreateInstance(m_winTraceType, id, name));
            }
            return new MyNullTrace();
        }


        private MyWintraceWrapper(object trace)
        {
            m_trace = trace;
            m_clearAll = Expression.Lambda<Action>(Expression.Call(Expression.Constant(m_trace), trace.GetType().GetMethod("ClearAll"))).Compile();
            m_clearAll();

            var name = Expression.Parameter(typeof(string));
            var value = Expression.Parameter(typeof(object));
            var call = Expression.Call(Expression.Constant(m_winWatches), m_winWatches.GetType().GetMethod("Send"), name, value);
            m_send = Expression.Lambda<Action<string, object>>(call, name, value).Compile();

            var msg = Expression.Parameter(typeof(string));
            var comment = Expression.Parameter(typeof(string));
            var debugMember = Expression.PropertyOrField(Expression.Constant(m_trace), "Debug");
            var debugCall = Expression.Call(debugMember, debugMember.Expression.Type.GetMethod("Send", new Type[] { typeof(string), typeof(string) }), msg, comment);
            m_debugSend = Expression.Lambda<Action<string, string>>(debugCall, msg, comment).Compile();
        }

        public void Send(string msg, string comment = null)
        {
            m_debugSend(msg, comment);
        }

        public void Watch(string name, object value)
        {
            try
            {
                m_send(name, value);
            }
            catch
            { //Sometimes it just fails...
            }
        }
    }

#endif
}
#endif // !XB1
