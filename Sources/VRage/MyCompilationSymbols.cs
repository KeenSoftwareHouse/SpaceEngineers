using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    public static class MyCompilationSymbols
    {
        public const bool PerformanceProfiling = false;

        public const bool ProfileFromStart = false;
        public const bool ProfileWorkingSetMemory = false;

        public const bool ProfileRenderMessages = false;

        public const bool EnableSharpDxObjectTracking = false;

#if XB1
        //TODO for XB1?
        public static bool MemoryProfiling = false;
#else // !XB1
        public static bool MemoryProfiling = IsProfilerAttached();
#endif // !XB1

        public static bool PerformanceOrMemoryProfiling = MemoryProfiling || PerformanceProfiling;

        public const bool DX11Debug = false;
        public const bool DX11DebugOutput = false;
        // enable/disable print of DirectX Debug messages that have type of Information
        public const bool DX11DebugOutputEnableInfo = false;
        // force stereo rendering even when OpenVR is not available
        public const bool DX11ForceStereo = false;

#if !XB1
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
#endif // !XB1
    }
}
