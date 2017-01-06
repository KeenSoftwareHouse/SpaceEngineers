using System;
using System.Diagnostics;

namespace VRage
{
    public static class MyCompilationSymbols
    {
        public const bool PerformanceProfiling = false;

        public const bool ProfileFromStart = false;
        public const bool ProfileWorkingSetMemory = false;

        public const bool ProfileRenderMessages = false;

        public const bool EnableSharpDxObjectTracking = false;

        public static bool EnableNetworkPacketTracking = false;
        public const bool EnableNetworkClientUpdateTracking = false;
        public const bool EnableNetworkPositionTracking = false;
        public static bool EnableNetworkServerIncomingPacketTracking = false;

#if XB1
        //TODO for XB1?
        public static bool MemoryProfiling = false;
#else // !XB1
        public static bool MemoryProfiling = IsProfilerAttached();
#endif // !XB1

        public static bool PerformanceOrMemoryProfiling = MemoryProfiling || PerformanceProfiling;

        public const bool DX11Debug = false;
        // do not change to const - it should be possible to change the value in runtime
        public static bool DX11DebugOutput = false;
        // enable/disable print of DirectX Debug messages that have type of Information
        public const bool DX11DebugOutputEnableInfo = false;

        public const bool CreateRefenceDevice = false;

        // force stereo rendering even when OpenVR is not available
        public const bool DX11ForceStereo = false;

        public const bool EnableShaderDebuggingInNSight = false;
        public const bool EnableShaderPreprocessorInNSight = false; // In NSight, all preprocessors will be processed and you will see postprocessed code

        public const bool LogRenderGIDs = false;
        public const bool ReinterpretFormatsStoredInFiles = true; // if it is enabled, linear formats for textures will be reinterpret as SRGB

        public const string DX11DebugSymbol = DX11Debug ? "WINDOWS" : "__RANDOM_UNDEFINED_PROFILING_SYMBOL__";

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

        public static bool IsDebugBuild
        {
            get
            {
                bool debugging = false;
                SetDebug(ref debugging);
                return debugging;
            }
        }

        [Conditional("DEBUG")]
        private static void SetDebug(ref bool debugging)
        {
            debugging = true;
        }
    }
}
