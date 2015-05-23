
#region Using

using System;
using System.Collections.Generic;
using System.Threading;
using SharpDX;
using VRage;
using VRage.Generics;
using VRage.Import;
using VRageMath;
using VRageRender.Graphics;
using VRageRender.Lights;
using VRageRender.Messages;
using VRageRender.Profiler;
using System.Diagnostics;
using System.Text;
using Vector2 = VRageMath.Vector2;
using VRage.Library.Utils;

#endregion

namespace VRageRender
{
    public class MyNullRender : IMyRender
    {
        public string RootDirectory { get { return null; } set { } }
        public string RootDirectoryEffects { get { return null; } set { } }
        public string RootDirectoryDebug { get { return null; } set { } }

        public MyRenderFont TryGetFont(int id)
        {
            return null;
        }

        public MyRenderSettings Settings { get { return null; } }
        public MySharedData SharedData { get { return null; } }
        public MyTimeSpan CurrentDrawTime { set; get; }

        public void ClearBackbuffer(ColorBGRA clearColor)
        {
            return;
        }

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
        public MyMessageQueue OutputQueue { get { return m_outputQueue; } }
        public uint GlobalMessageCounter { get { return 0; } set { } }

        public void EnqueueMessage(IMyRenderMessage message, bool limitMaxQueueSize)
        {
        }

        public void ProcessMessages()
        {
        }

        public void EnqueueOutputMessage(IMyRenderMessage message)
        {
        }

        public void ResetEnvironmentProbes()
        {
            
        }

        public MyRenderProfiler GetRenderProfiler()
        {
            return null;
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
    }
}
