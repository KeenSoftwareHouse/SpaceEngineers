using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using VRage;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRageRender.Profiler;

namespace VRageRender
{   
    enum MyMsaaSamplesEnum
    {
        None, MSAA_2, MSAA_4, MSAA_8
    }

    internal static class MyMsaaSamplesEnumExtensions
    {
        internal static int SamplesNum(this MyMsaaSamplesEnum value)
        {
            switch(value)
            {
                case VRageRender.MyMsaaSamplesEnum.MSAA_2:
                    return 2;
                case VRageRender.MyMsaaSamplesEnum.MSAA_4:
                    return 4;
                case VRageRender.MyMsaaSamplesEnum.MSAA_8:
                    return 8;
                default:
                    return 1;

            }
        }
    }

    struct MyRenderQualityData
    {
        internal MyMsaaSamplesEnum m_msaaSettings;
        internal bool IsMultisampled { get { return m_msaaSettings != MyMsaaSamplesEnum.None; } }
        internal int MsaaSamplesNum { get { return m_msaaSettings.SamplesNum(); } }

        internal int ShadowmapCascadeResolution;

        internal static MyRenderQualityData CreateDefault()
        {
            MyRenderQualityData rq;
            rq.m_msaaSettings = MyMsaaSamplesEnum.MSAA_4;
            rq.ShadowmapCascadeResolution = 1024;
            return rq;
        }
    }

    class MyRenderQualitySettings
    {
        internal MyRenderQualityData Current { get; private set; }

        internal bool m_rebuildShaders = false;
        internal bool m_recreateRTs = false;
        internal string m_shaderGlobalMacros = "";
        
        internal void Change(MyRenderQualityData renderQuality)
        {
            if (renderQuality.m_msaaSettings != Current.m_msaaSettings)
            { 
                m_rebuildShaders = true;
                m_recreateRTs = true;
            }

            Current = renderQuality;

            var list = new List<string>();
            if(Current.IsMultisampled)
            {
                list.Add(String.Format("MS_SAMPLE_COUNT {0}", Current.MsaaSamplesNum));
            }
            m_shaderGlobalMacros = MyShaderDefines.Build(list.ToArray());
        }

        internal MyRenderQualitySettings()
        {
            Change(MyRenderQualityData.CreateDefault());
        }
    }

    internal partial class MyRender11
    {
        internal static ObjectIDGenerator IdGenerator = new ObjectIDGenerator();

        internal static MyTimeSpan CurrentDrawTime;
        internal static MyTimeSpan CurrentUpdateTime;
        internal static MyTimeSpan PreviousDrawTime;
        internal static MyTimeSpan TimeDelta { get { return CurrentDrawTime - PreviousDrawTime; } }
        internal static MySharedData SharedData = new MySharedData();
        internal static MyLog Log = new MyLog();

        internal static string RootDirectory = MyFileSystem.ContentPath;
        internal static string RootDirectoryEffects = MyFileSystem.ContentPath;
        internal static string RootDirectoryDebug = MyFileSystem.ContentPath;

        internal static uint GlobalMessageCounter = 0;

        internal static MyRenderSettings Settings = new MyRenderSettings();

#if DEBUG
        internal const bool DebugMode = true;
#else
        internal const bool DebugMode = false;
#endif

        static MyRender11()
        {
            const string logName = "VRageRender-DirectX11.log";
            Log.Init(logName, new System.Text.StringBuilder("Version unknown"));
            Log.WriteLine("VRage renderer started");
        }

        internal static void EnqueueMessage(IMyRenderMessage message, bool limitMaxQueueSize)
        {
            SharedData.CurrentUpdateFrame.RenderInput.Add(message);
        }

        internal static void EnqueueOutputMessage(IMyRenderMessage message)
        {
            SharedData.RenderOutputMessageQueue.Enqueue(message);
        }

        internal static MyMessageQueue OutputQueue
        {
            get { return SharedData.RenderOutputMessageQueue; }
        }

        // Profiling
        static MyRenderProfiler m_renderProfiler = new MyRenderProfilerDX11();

        internal static MyRenderProfiler GetRenderProfiler()
        {
            return m_renderProfiler;
        }
    }
}