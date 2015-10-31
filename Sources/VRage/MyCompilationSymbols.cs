using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    public static class MyCompilationSymbols
    {
        public const bool PerformanceProfiling = true;

        public const bool EnableSharpDxObjectTracking = false;

        public static bool MemoryProfiling = IsProfilerAttached();

        public static bool PerformanceOrMemoryProfiling = MemoryProfiling || PerformanceProfiling;

        /// <summary>
        /// This is reliable, enforced by .NET documentation.
        /// Without this environment variable, CLR won't attach profiler.
        /// </summary>
        static bool IsProfilerAttached()
        {
            var CorProfiling = Environment.GetEnvironmentVariable("cor_enable_profiling") ?? String.Empty;
            var CoreClrProfiling = Environment.GetEnvironmentVariable("coreclr_enable_profiling") ?? String.Empty;
            return CorProfiling.Trim() == "1" || CoreClrProfiling.Trim() == "1";
        }
    }
}
