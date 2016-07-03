﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Trace
{

#if !UNSHARPER
    public enum TraceWindow
    {
        Default,
        Saving,
        ParallelParticles,
        Server,
        EntityId,
        Multiplayer,
        MultiplayerFiltered,
        MultiplayerAlerts,
        Analytics,
        Ai,
    }

    public delegate ITrace InitTraceHandler(string traceId, string traceName);

    public static class MyTrace
    {
        const string WindowPrefix = "SE_ID";
        const string WindowName = "SE";

        static Dictionary<int, ITrace> m_traces;
        static MyNullTrace m_nullTrace = new MyNullTrace();

        [Conditional("DEBUG")]
        public static void Init(InitTraceHandler handler)
        {
            InitInternal(handler);
        }

        [Conditional("DEBUG")]
        public static void InitWinTrace()
        {
            InitInternal(InitWintraceHandler);
        }

        [Conditional("DEBUG"), Conditional("DEVELOP")]
        private static void InitInternal(InitTraceHandler handler)
        {
            m_traces = new Dictionary<int, ITrace>();

            //var windowName = Process.GetCurrentProcess().ProcessName.Replace(".vshost", "");
            var windowName = WindowName;

            foreach (var e in Enum.GetValues(typeof(TraceWindow)))
            {
                var name = ((TraceWindow)e == TraceWindow.Default) ? windowName : (windowName + "_" + e.ToString());

                string id = String.Format("{0}_{1}", WindowPrefix, name);
                m_traces[(int)e] = handler(id, name);
            }
        }

        private static ITrace InitWintraceHandler(string traceId, string traceName)
        {
            return MyWintraceWrapper.CreateTrace(traceId, traceName);
        }

        [Conditional("DEBUG")]
        public static void Watch(string name, object value)
        {
            GetTrace(TraceWindow.Default).Watch(name, value);
        }

        [Conditional("DEBUG")]
        public static void Send(TraceWindow window, string msg, string comment = null)
        {
            GetTrace(window).Send(msg, comment);
        }

        public static ITrace GetTrace(TraceWindow window)
        {
            ITrace trace;
            if (m_traces == null || !m_traces.TryGetValue((int)window, out trace))
            {
                trace = m_nullTrace;
            }
            return trace;
        }
    }

#endif
}
