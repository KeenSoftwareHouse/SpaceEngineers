#region Using

using System;
using VRageMath;
using VRageRender.Profiler;
using Vector2 = VRageMath.Vector2;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Utils;
using VRageRender.Messages;

#endregion

namespace VRageRender
{
    public class MyNullRender : IMyRender
    {
        public string RootDirectory { get { return null; } set { } }
        public string RootDirectoryEffects { get { return null; } set { } }
        public string RootDirectoryDebug { get { return null; } set { } }

        public MyLog Log { get { return null; } }

        internal static MyRenderFont TryGetFont(int id)
        {
            return null;
        }

        public MySharedData SharedData { get { return null; } }
        public MyTimeSpan CurrentDrawTime { set; get; }

        public MyViewport MainViewport { get; private set; }
        public Vector2I BackBufferResolution { get; private set; }

        public void LoadContent(MyRenderQualityEnum quality)
        {
        }

        public void UnloadContent()
        {
        }

        public void UnloadData()
        {
        }

        public void ReloadContent(MyRenderQualityEnum quality)
        {
        }

        MyMessageQueue m_outputQueue = new MyMessageQueue();
        MyNullRenderProfiler m_profiler = new MyNullRenderProfiler();
        public MyMessageQueue OutputQueue { get { return m_outputQueue; } }
        public uint GlobalMessageCounter { get; set; }

        public MyNullRender()
        {
            m_profiler.SetAutocommit(false);
        }

        public void EnqueueMessage(MyRenderMessageBase message, bool limitMaxQueueSize)
        {
        }

        public void ProcessMessages()
        {
        }

        public void EnqueueOutputMessage(MyRenderMessageBase message)
        {
        }

        public void ResetEnvironmentProbes()
        {
            
        }

        public MyRenderProfiler GetRenderProfiler()
        {
            return m_profiler;
        }


        public void Draw(bool draw = true)
        {
        }

        //Video
        public bool IsVideoValid(uint id)
        {
            return false;
        }

        public VideoState GetVideoState(uint id)
        {
            return VideoState.Stopped;
        }

        public double GetVideoPosition(uint id)
        {
            return 0;
        }

        public Vector2 GetVideoSize(uint id)
        {
            return Vector2.Zero;
        }

        public MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            return default(MyRenderDeviceSettings);
        }

        public void DisposeDevice()
        {
        }

        public long GetAvailableTextureMemory()
        {
            return 0;
        }

        public MyRenderDeviceCooperativeLevel TestDeviceCooperativeLevel()
        {
            return MyRenderDeviceCooperativeLevel.Ok;
        }

        public bool ResetDevice()
        {
            return false;
        }

        public void DrawBegin()
        {
        }

        public void DrawEnd()
        {
        }

        bool IMyRender.IsSupported { get { return true; } }

        public bool SettingsChanged(MyRenderDeviceSettings settings)
        {
            return true;
        }

        public void ApplySettings(MyRenderDeviceSettings settings)
        {
        }

        public void Present()
        {
        }

        public void HandleFocusMessage(MyWindowFocusMessage msg) { }
        public void GenerateShaderCache(bool clean, OnShaderCacheProgressDelegate onShaderCacheProgress)
        {
        }
    }
}
