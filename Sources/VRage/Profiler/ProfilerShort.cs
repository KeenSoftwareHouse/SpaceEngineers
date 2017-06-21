using System.Diagnostics;
using System.Runtime.CompilerServices;
using VRage.Library.Utils;

namespace VRage.Profiler
{
#if !XB1 // XB1_NOPROFILER
    /// <summary>
    /// Helper class, "shortcuts" to profiler
    /// </summary>
    [Unsharper.UnsharperDisableReflection()]
    public static class ProfilerShort
    {
        public const string PerformanceProfilingSymbol = MyRenderProfiler.PerformanceProfilingSymbol;

        private static MyRenderProfiler m_profiler;

        public static MyRenderProfiler Profiler
        {
            get
            {
                Debug.Assert(m_profiler != null, "No profiler set. It should be done by VRage.Render.MyRenderProxy during renderer initialization.");
                return m_profiler;
            }
            private set { m_profiler = value; }
        }

        public static void SetProfiler(MyRenderProfiler profiler)
        {
            Profiler = profiler;
        }

        public static bool Autocommit
        {
            get { bool val = false; Profiler.GetAutocommit(ref val); return val; }
            set { Profiler.SetAutocommit(value); }
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void Begin(string blockName = null, float customValue = 0, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            Profiler.StartProfilingBlock(blockName, customValue, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void BeginNextBlock(string blockName = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            Profiler.StartNextBlock(blockName, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void End(float customValue = 0, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            Profiler.EndProfilingBlock(customValue, customTime, timeFormat, valueFormat, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void CustomValue(string name, float value, MyTimeSpan? customTime, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            Profiler.ProfileCustomValue(name, value, customTime, timeFormat, valueFormat, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void End(float customValue, float customTimeMs, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            Profiler.EndProfilingBlock(customValue, MyTimeSpan.FromMilliseconds(customTimeMs), timeFormat, valueFormat, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public static void CustomValue(string name, float value, float customTimeMs, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            Profiler.ProfileCustomValue(name, value, MyTimeSpan.FromMilliseconds(customTimeMs), timeFormat, valueFormat, member, line, file);
        }

        public static void Commit()
        {
            if (Profiler != null)
            {
                Profiler.Commit();
            }
        }

        public static void DestroyThread()
        {
            if (Profiler != null)
            {
                Profiler.DestroyThread();
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
