using SharpDX;
using System;
using VRage.Library.Utils;
using VRageMath;
using VRageRender.Profiler;

namespace VRageRender
{
    /// <summary>
    /// Describes the state of a video player
    /// </summary>
    public enum VideoState
    {
        Playing,
        Paused,
        Stopped
    }

    public enum MyRenderDeviceCooperativeLevel
    {
        Ok,
        Lost,
        NotReset,
        DriverError
    }

    public class MyDeviceLostException : Exception
    {
    }

    public interface IMyRender
    {
        /// <summary>
        /// Must be possible to query during startup before render thread and window is created.
        /// </summary>
        bool IsSupported { get; }

        string RootDirectory { get; set; }
        string RootDirectoryEffects { get; set; }
        string RootDirectoryDebug { get; set; }

        //MyRenderFont TryGetFont(int id);
        MyRenderSettings Settings { get; }        
        MySharedData SharedData { get; }
        MyTimeSpan CurrentDrawTime { set; get; }

        MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry);
        void DisposeDevice();
        long GetAvailableTextureMemory();
        MyRenderDeviceCooperativeLevel TestDeviceCooperativeLevel();
        bool ResetDevice();

        void DrawBegin();
        void DrawEnd();

        bool SettingsChanged(MyRenderDeviceSettings settings);
        void ApplySettings(MyRenderDeviceSettings settings);

        void Present();
        void ClearBackbuffer(ColorBGRA clearColor);
        MyViewport MainViewport { get; }
        Vector2I BackBufferResolution { get; }

        void LoadContent(global::VRageRender.MyRenderQualityEnum quality);
        void UnloadContent();
        void UnloadData();
        void ReloadContent(MyRenderQualityEnum quality);

        MyMessageQueue OutputQueue { get; }
        uint GlobalMessageCounter { get; set; }
        void EnqueueMessage(IMyRenderMessage message, bool limitMaxQueueSize);
        void ProcessMessages();
        void EnqueueOutputMessage(IMyRenderMessage message);

        void ResetEnvironmentProbes();

        MyRenderProfiler GetRenderProfiler();
        void Draw(bool draw = true);
       
        //Video
        bool IsVideoValid(uint id);
        VideoState GetVideoState(uint id);

        void HandleFocusMessage(MyWindowFocusMessage msg);
    }
}
