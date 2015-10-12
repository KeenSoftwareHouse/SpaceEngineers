using SharpDX;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using VRage.Collections;
using VRage;
using VRage.Stats;
using VRage.Win32;
using VRageRender;
using VRage.Library.Utils;

namespace VRage
{
    /// <summary>
    /// Initializes window on render thread and returns it's handle of window/control where to draw
    /// </summary>
    public delegate IMyRenderWindow InitHandler();

    public delegate void SizeChangedHandler(int width, int height, MyViewport viewport);

    public class MyRenderThread
    {
        class StartParams
        {
            public InitHandler InitHandler;
            public MyRenderDeviceSettings? SettingsToTry;
            public MyRenderQualityEnum RenderQuality;
        }

        readonly MyGameTimer m_timer;
        MyTimeSpan m_messageProcessingStart; // Used for profiling message queue

        volatile bool m_stopped = false;

        IMyRenderWindow m_renderWindow;
        MyRenderQualityEnum m_currentQuality;

        System.Windows.Forms.Control m_form;

        private MyRenderDeviceSettings m_settings;
        private MyRenderDeviceSettings? m_newSettings;
        private int m_newQuality = -1;

        public int CurrentAdapter
        {
            get { return m_settings.AdapterOrdinal; }
        }

        public MyRenderDeviceSettings CurrentSettings { get { return m_settings; } }

        MyConcurrentQueue<Action> m_invokeQueue = new MyConcurrentQueue<Action>(16);

        public readonly Thread SystemThread;

        public event Action BeforeDraw;
        public event SizeChangedHandler SizeChanged;
        private readonly bool m_separateThread;

        private MyRenderThread(MyGameTimer timer, bool separateThread)
        {
            m_timer = timer;
            m_separateThread = separateThread;

            if (separateThread)
            {
                SystemThread = new Thread(new ParameterizedThreadStart(RenderThreadStart));
                //RenderThread.Priority = ThreadPriority.AboveNormal;
                SystemThread.IsBackground = true; // Do not prevent app from terminating
                SystemThread.Name = "Render thread";
                SystemThread.CurrentCulture = CultureInfo.InvariantCulture;
                SystemThread.CurrentUICulture = CultureInfo.InvariantCulture;
            }
            else
            {
                SystemThread = Thread.CurrentThread;
            }
        }

        public static MyRenderThread Start(MyGameTimer timer, InitHandler initHandler, MyRenderDeviceSettings? settingsToTry, MyRenderQualityEnum renderQuality)
        {
            var result = new MyRenderThread(timer, true);
            result.SystemThread.Start(new StartParams() { InitHandler = initHandler, SettingsToTry = settingsToTry, RenderQuality = renderQuality });
            return result;
        }

        public static MyRenderThread StartSync(MyGameTimer timer, IMyRenderWindow renderWindow, MyRenderDeviceSettings? settingsToTry, MyRenderQualityEnum renderQuality)
        {
            var result = new MyRenderThread(timer, false);
            result.m_renderWindow = renderWindow;
            result.m_settings = MyRenderProxy.CreateDevice(result, renderWindow.Handle, settingsToTry);
            MyRenderProxy.SendCreatedDeviceSettings(result.m_settings);

            result.m_currentQuality = renderQuality;
            result.m_form = System.Windows.Forms.Control.FromHandle(renderWindow.Handle);

            result.LoadContent();
            result.UpdateSize();
            return result;
        }

        public void TickSync()
        {
            if (MyRenderProxy.EnableAppEventsCall)
            {
                Application.DoEvents();
            }
            RenderCallback();
        }

        public void Invoke(Action action)
        {
            m_invokeQueue.Enqueue(action);
        }

        public void SwitchSettings(MyRenderDeviceSettings settings)
        {
            Debug.Assert(Thread.CurrentThread == SystemThread);
            m_newSettings = settings;
        }

        public void SwitchQuality(MyRenderQualityEnum quality)
        {
            Debug.Assert(Thread.CurrentThread == SystemThread);
            m_newQuality = (int)quality;
        }

        /// <summary>
        /// Signals the thread to exit and waits until it does so
        /// </summary>
        public void Exit()
        {
            Debug.Assert(!m_stopped, "Already stopped");
            m_stopped = true;

            if (SystemThread != null)
            {
                // TODO: OP! Should be done better
                try
                {
                    if (!m_form.IsDisposed)
                        m_form.Invoke(new Action(OnExit));
                }
                catch
                {
                    // Racing condition, form can be disposed after check and before Invoke
                }

                // TODO: OP! Should be done better
                if (Thread.CurrentThread != SystemThread)
                    SystemThread.Join();
            }
            else
            {
                UnloadContent();
                MyRenderProxy.DisposeDevice();
            }
        }

        private void OnExit()
        {
            m_form.Dispose();
        }

        private void RenderThreadStart(object param)
        {
            // TODO: OP! Initialize render log file

            ProfilerShort.Autocommit = false;

            var startParams = (StartParams)param;

            m_renderWindow = startParams.InitHandler();
            var control = System.Windows.Forms.Control.FromHandle(m_renderWindow.Handle);

            m_settings = MyRenderProxy.CreateDevice(this, m_renderWindow.Handle, startParams.SettingsToTry);
            MyRenderProxy.SendCreatedDeviceSettings(m_settings);
            m_currentQuality = startParams.RenderQuality;
            m_form = control;

            LoadContent();
            UpdateSize();

            //RenderLoop.UseCustomDoEvents = true;
            RenderLoop.Run(m_form, RenderCallback);

            UnloadContent();

            MyRenderProxy.DisposeDevice();
        }

        private void RenderCallback()
        {
            if (m_messageProcessingStart != MyTimeSpan.Zero)
            {
                ProfilerShort.CustomValue("MessageQueue", 0, m_timer.Elapsed - m_messageProcessingStart);
            }

            ProfilerShort.Begin("PrepareDraw");

            ProfilerShort.Begin("ProcessInvoke");
            Action action;
            while (m_invokeQueue.TryDequeue(out action))
            {
                action();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("ApplyModeChanges");
            ApplySettingsChanges();
            ProfilerShort.End();

            ProfilerShort.Begin("BeforeRender");
            MyRenderStats.Generic.WriteFormat("Available GPU memory: {0} MB", (float)MyRenderProxy.GetAvailableTextureMemory() / 1024 / 1024, MyStatTypeEnum.CurrentValue, 300, 2);
            var drawTime = m_timer.Elapsed;
            MyRenderProxy.BeforeRender(drawTime);
            ProfilerShort.End();

            ProfilerShort.Begin("RenderWindow.BeforeDraw");
            m_renderWindow.BeforeDraw();
            ProfilerShort.End();

            ProfilerShort.Begin("BeforeDraw(event)");
            var handler = BeforeDraw;
            if (handler != null) handler();
            ProfilerShort.End();

            ProfilerShort.End();

            ProfilerShort.Begin("TestCooperativeLevel");
            var deviceResult = MyRenderProxy.TestDeviceCooperativeLevel();
            ProfilerShort.End();

            if (!m_renderWindow.DrawEnabled)
            {
                ProfilerShort.Begin("ProcessMessages");
                MyRenderProxy.ProcessMessages();
                ProfilerShort.End();
            }
            else if (deviceResult == MyRenderDeviceCooperativeLevel.Ok)
            {
                Draw();
            }
            else
            {
                ProfilerShort.Begin("WaitForReset");

                ProfilerShort.Begin("ProcessMessages");
                MyRenderProxy.ProcessMessages();
                ProfilerShort.End();

                if (deviceResult == MyRenderDeviceCooperativeLevel.Lost)
                {
                    ProfilerShort.Begin("DeviceLost");
                    Thread.Sleep(20);
                    ProfilerShort.End();
                }
                else if (deviceResult == MyRenderDeviceCooperativeLevel.NotReset)
                {
                    ProfilerShort.Begin("DeviceReset");
                    Thread.Sleep(20);
                    DeviceReset();
                    ProfilerShort.End();
                }
                else
                {
                    // TODO: OP! Log error code
                }
                ProfilerShort.End();
            }

            ProfilerShort.Begin("AfterRender");
            MyRenderProxy.AfterRender();
            ProfilerShort.End();

            ProfilerShort.Begin("Present");
            if (deviceResult == MyRenderDeviceCooperativeLevel.Ok && m_renderWindow.DrawEnabled)
            {
                try
                {
                    MyRenderProxy.Present();
                }
                catch (MyDeviceLostException)
                {
                }
            }
            ProfilerShort.End();

            m_messageProcessingStart = m_timer.Elapsed;
        }

        private void ApplySettingsChanges()
        {
            if(MyRenderProxy.TestDeviceCooperativeLevel() == MyRenderDeviceCooperativeLevel.Ok)
            {
                var quality = Interlocked.Exchange(ref m_newQuality, -1);
                if (quality != -1)
                {
                    m_currentQuality = (MyRenderQualityEnum)quality;
                }

                if (m_newSettings.HasValue && MyRenderProxy.SettingsChanged(m_newSettings.Value))
                {
                    m_settings = m_newSettings.Value;
                    m_newSettings = null;
                    UnloadContent();
                    MyRenderProxy.ApplySettings(m_settings);
                    LoadContent();
                    UpdateSize();
                }
                else if (quality != -1)
                {
                    // Quality has changed, but not settings
                    ProfilerShort.Begin("ReloadContent");
                    MyRenderProxy.ReloadContent(m_currentQuality);
                    ProfilerShort.End();
                }
            }
        }

        private void LoadContent()
        {
            MyRenderProxy.LoadContent(m_currentQuality);
        }

        public void UpdateSize(MyWindowModeEnum ? customMode = null)
        {
            ProfilerShort.Begin("UpdateSize");

            switch (customMode.HasValue ? customMode.Value : m_settings.WindowMode)
            {
                case MyWindowModeEnum.Fullscreen:
                    m_renderWindow.OnModeChanged(MyWindowModeEnum.Fullscreen, m_settings.BackBufferWidth, m_settings.BackBufferHeight);
                    break;

                case MyWindowModeEnum.FullscreenWindow:
                    {
                        WinApi.DEVMODE mode = new WinApi.DEVMODE();
                        WinApi.EnumDisplaySettings(null, WinApi.ENUM_REGISTRY_SETTINGS, ref mode);
                        VRage.Trace.MyTrace.Watch("Registry display settings", string.Format("{0}x{1}", mode.dmPelsWidth, mode.dmPelsHeight));
                        m_renderWindow.OnModeChanged(MyWindowModeEnum.FullscreenWindow, mode.dmPelsWidth, mode.dmPelsHeight);
                        break;
                    }

                case MyWindowModeEnum.Window:
                    m_renderWindow.OnModeChanged(MyWindowModeEnum.Window, m_settings.BackBufferWidth, m_settings.BackBufferHeight);
                    break;
            }

            var handler = SizeChanged;
            if (handler != null) handler(MyRenderProxy.BackBufferResolution.X, MyRenderProxy.BackBufferResolution.Y, MyRenderProxy.MainViewport);

            ProfilerShort.End();
        }

        private void UnloadContent()
        {
            MyRenderProxy.UnloadContent();
        }

        private void DeviceReset()
        {
            UnloadContent();
            if (MyRenderProxy.ResetDevice())
            {
                LoadContent();
            }
        }

        private void Draw()
        {
            ProfilerShort.Begin("BeginScene");
            MyRenderProxy.DrawBegin();
            ProfilerShort.End();

            ProfilerShort.Begin("Clear");
            // TODO: OP! This should be done only to prevent weird things on screen, not every frame
            MyRenderProxy.ClearBackbuffer(new ColorBGRA(0.0f));
            MyRenderProxy.ClearLargeMessages();
            ProfilerShort.End();

            ProfilerShort.Begin("MyRenderProxy.Draw");
            MyRenderProxy.Draw();
            ProfilerShort.End();

            if (m_separateThread)
            {
                MyRenderProxy.GetRenderProfiler().Commit();
            }

            MyRenderProxy.GetRenderProfiler().Draw();

            ProfilerShort.Begin("EndScene");
            MyRenderProxy.DrawEnd();
            ProfilerShort.End();
        }
    }
}
