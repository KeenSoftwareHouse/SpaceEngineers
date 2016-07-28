using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Library.Utils;
using VRageRender;

namespace VRage
{
#if !XB1 // XB1_NOPROFILER
    /// <summary>
    /// Helper class, "shortcuts" to profiler
    /// </summary>
    [Unsharper.UnsharperDisableReflection()]
    public static class ProfilerShort
    {
        public const string PerformanceProfilingSymbol = VRageRender.Profiler.MyRenderProfiler.PerformanceProfilingSymbol;

        public static bool Autocommit
        {
            get { bool val = false; MyRenderProxy.GetRenderProfiler().GetAutocommit(ref val); return val; }
            set { MyRenderProxy.GetRenderProfiler().SetAutocommit(value); }
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void Begin(string blockName = null, float customValue = 0, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            MyRenderProxy.GetRenderProfiler().StartProfilingBlock(blockName, customValue, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void BeginNextBlock(string blockName = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            MyRenderProxy.GetRenderProfiler().StartNextBlock(blockName, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void End(float customValue = 0, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            MyRenderProxy.GetRenderProfiler().EndProfilingBlock(customValue, customTime, timeFormat, valueFormat, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void CustomValue(string name, float value, MyTimeSpan? customTime, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            MyRenderProxy.GetRenderProfiler().ProfileCustomValue(name, value, customTime, timeFormat, valueFormat, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void End(float customValue, float customTimeMs, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            MyRenderProxy.GetRenderProfiler().EndProfilingBlock(customValue, MyTimeSpan.FromMiliseconds(customTimeMs), timeFormat, valueFormat, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void CustomValue(string name, float value, float customTimeMs, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            MyRenderProxy.GetRenderProfiler().ProfileCustomValue(name, value, MyTimeSpan.FromMiliseconds(customTimeMs), timeFormat, valueFormat, member, line, file);
        }

        public static void Commit()
        {
            if (MyRenderProxy.GetRenderProfiler() != null)
            {
                MyRenderProxy.GetRenderProfiler().Commit();
            }
        }

        public static void DestroyThread()
        {
            if (MyRenderProxy.GetRenderProfiler() != null)
            {
                MyRenderProxy.GetRenderProfiler().DestroyThread();
            }
        }
    }
#else // XB1
    public static class ProfilerShort
    {
        public const string PerformanceProfilingSymbol = VRageRender.Profiler.MyRenderProfiler.PerformanceProfilingSymbol;

        public static bool Autocommit;

        public static void CustomValue(string name, float value, MyTimeSpan? customTime, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
        }

        public static void CustomValue(string name, float value, float customTimeMs, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
        }

        public static void Begin(string blockName = null, float customValue = 0, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
        }

        public static void End(float customValue = 0, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
        }

        public static void End(float customValue, float customTimeMs, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
        }

        public static void BeginNextBlock(string blockName = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
        }

        public static void Commit()
        {
        }

        public static void DestroyThread()
        {
        }
    }
#endif // XB1
}
