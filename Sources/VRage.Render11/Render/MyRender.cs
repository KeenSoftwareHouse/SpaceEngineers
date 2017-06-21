using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using SharpDX.Direct3D;
using VRage;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Render11.Profiler;
using VRage.Render11.Profiler.Internal;
using VRage.Utils;
using VRageRender.Messages;
using VRageRender.Profiler;

namespace VRageRender
{   
    internal partial class MyRender11
    {
        internal static ObjectIDGenerator IdGenerator = new ObjectIDGenerator();

        internal static MyTimeSpan CurrentDrawTime;
        internal static MyTimeSpan CurrentUpdateTime;
        internal static MyTimeSpan PreviousDrawTime;
        internal static MyTimeSpan TimeDelta { get { return CurrentDrawTime - PreviousDrawTime; } }
        internal static MySharedData SharedData = new MySharedData();
        internal static MyLog Log = new MyLog(true);

        internal static string RootDirectory = MyFileSystem.ContentPath;
        internal static string RootDirectoryEffects = MyFileSystem.ContentPath;
        internal static string RootDirectoryDebug = MyFileSystem.ContentPath;

        internal static uint GlobalMessageCounter = 0;

        public static int GameplayFrameCounter { get; private set; }
        internal static MyRenderSettings Settings = MyRenderSettings.Default;

        internal static MyEnvironment Environment = new MyEnvironment();

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

        internal static void TransferLocalMessages()
        {
            // Enqueue messages from last frame. (Usually debug draw)
            while (SharedData.MessagesForNextFrame.Count > 0)
            {
                var msg = SharedData.MessagesForNextFrame.Dequeue();

                if(msg.MessageClass == MyRenderMessageType.DebugDraw)
                    m_debugDrawMessages.Enqueue(msg);
                else
                    SharedData.CurrentUpdateFrame.Enqueue(msg);
            }
        }

        internal static void EnqueueMessage(MyRenderMessageBase message, bool limitMaxQueueSize)
        {
            if (MyRenderProxy.RenderThread != null && Thread.CurrentThread == MyRenderProxy.RenderThread.SystemThread)
            {
                SharedData.MessagesForNextFrame.Enqueue(message);
            }
            else
                SharedData.CurrentUpdateFrame.Enqueue(message);
        }

        internal static void EnqueueOutputMessage(MyRenderMessageBase message)
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