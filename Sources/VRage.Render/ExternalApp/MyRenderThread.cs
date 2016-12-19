using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using SharpDX.Windows;
using VRage;
using VRage.Collections;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Stats;
using VRage.Win32;
using VRageRender.Utils;

#if !XB1
#endif

namespace VRageRender.ExternalApp
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
        readonly WaitForTargetFrameRate m_waiter;
        MyTimeSpan m_messageProcessingStart; // Used for profiling message queue
        MyTimeSpan m_frameStart;
        MyTimeSpan m_appEventsTime;

        int m_stopped = 0; // Integer because interlocked doesn't work with bools

        IMyRenderWindow m_renderWindow;
        MyRenderQualityEnum m_currentQuality;

#if !XB1
		System.Windows.Forms.Control m_form;
#else
		RenderForm m_form;
#endif

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

        private readonly MyConcurrentQueue<EventWaitHandle> m_debugWaitForPresentHandles = new MyConcurrentQueue<EventWaitHandle>(16);
        private int m_debugWaitForPresentHandleCount = 0;

        private MyRenderThread(MyGameTimer timer, bool separateThread, float maxFrameRate)
        {
            m_timer = timer;
            m_waiter = new WaitForTargetFrameRate(timer, maxFrameRate);
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

        public static MyRenderThread Start(MyGameTimer timer, InitHandler initHandler, MyRenderDeviceSettings? settingsToTry, MyRenderQualityEnum renderQuality, float maxFrameRate)
        {
            var result = new MyRenderThread(timer, true, maxFrameRate);
            result.SystemThread.Start(new StartParams() { InitHandler = initHandler, SettingsToTry = settingsToTry, RenderQuality = renderQuality });
            return result;
        }

        public static MyRenderThread StartSync(MyGameTimer timer, IMyRenderWindow renderWindow, MyRenderDeviceSettings? settingsToTry, MyRenderQualityEnum renderQuality, float maxFrameRate)
        {
            var result = new MyRenderThread(timer, false, maxFrameRate);
            result.m_renderWindow = renderWindow;
            result.m_settings = MyRenderProxy.CreateDevice(result, renderWindow.Handle, settingsToTry);
            MyRenderProxy.SendCreatedDeviceSettings(result.m_settings);

            result.m_currentQuality = renderQuality;
#if XB1
            Debug.Assert(false);
#else
            result.m_form = Control.FromHandle(renderWindow.Handle);
#endif

            result.LoadContent();
            result.UpdateSize();
            return result;
        }

        public void TickSync()
        {
            if (MyRenderProxy.EnableAppEventsCall)
            {
                if ((m_timer.Elapsed - m_appEventsTime).Milliseconds > 10)
                {
#if !XB1
                    Application.DoEvents();
#endif
                    m_appEventsTime = m_timer.Elapsed;
                }
#if !XB1
                Application.DoEvents();
#endif
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
            if (Interlocked.Exchange(ref m_stopped, 1) == 1)
                return;

            if (SystemThread != null)
            {
                // TODO: OP! Should be done better
                try
                {
#if !XB1
                    if (!m_form.IsDisposed)
                        m_form.Invoke(new Action(OnExit));
#endif
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
#if !XB1
			var control = System.Windows.Forms.Control.FromHandle(m_renderWindow.Handle);
#endif

            m_settings = MyRenderProxy.CreateDevice(this, m_renderWindow.Handle, startParams.SettingsToTry);
            if (m_settings.AdapterOrdinal == -1)
                return;
            MyRenderProxy.SendCreatedDeviceSettings(m_settings);
            m_currentQuality = startParams.RenderQuality;
#if !XB1
			m_form = control;
#else
			m_form = m_renderWindow as RenderForm;
#endif

            LoadContent();
            UpdateSize();
            
            //RenderLoop.UseCustomDoEvents = true;
            //RenderLoop.Run(m_form, RenderCallback);

            m_form.Show();
            while (m_form.Visible)
            {
                Application.DoEvents();
                if (m_form.Visible)
                    RenderCallback();
            }

            UnloadContent();

            MyRenderProxy.DisposeDevice();
        }

        private void RenderCallback()
        {
            if (m_messageProcessingStart != MyTimeSpan.Zero)
            {
                MyTimeSpan messageQueueDuration = m_timer.Elapsed - m_messageProcessingStart;
                ProfilerShort.CustomValue("MessageQueue", 0, messageQueueDuration);
            }
            ProfilerShort.Begin("Wait");
            m_waiter.Wait();
            ProfilerShort.End();
            
            m_frameStart = m_timer.Elapsed;

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
            MyRenderProxy.BeforeRender(m_frameStart);
            ProfilerShort.End();

            ProfilerShort.Begin("RenderWindow.BeforeDraw");
            m_renderWindow.BeforeDraw();
            ProfilerShort.End();

            ProfilerShort.Begin("BeforeDraw(event)");
			if (BeforeDraw != null)
				BeforeDraw();
            ProfilerShort.End();

            ProfilerShort.End();

            ProfilerShort.Begin("Draw");

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
            ProfilerShort.End();

            ProfilerShort.Begin("AfterRender");
            MyRenderProxy.AfterRender();
            ProfilerShort.End();

            ProfilerShort.Begin("Present");
            if (deviceResult == MyRenderDeviceCooperativeLevel.Ok && m_renderWindow.DrawEnabled)
            {
                this.DoBeforePresent();
                try
                {
                    MyRenderProxy.Present();
                }
                catch (MyDeviceErrorException e)
                {
                    // Present() ended up with an error -- don't try to recover
                    MyRenderProxy.Error(e.Message, shouldTerminate: true);
                    Exit();
                }
                this.DoAfterPresent();
            }
            ProfilerShort.End();

            if (m_separateThread)
            {
                MyRenderProxy.GetRenderProfiler().Commit();
            }

            m_messageProcessingStart = m_timer.Elapsed;

            if (MyRenderProxy.Settings.ForceSlowCPU)
                Thread.Sleep(200);
        }

        private void DoBeforePresent()
        {
            // store number of waiting before present (newcomers lmust wait for next frame and will not be dequeued now)
            m_debugWaitForPresentHandleCount = m_debugWaitForPresentHandles.Count;
        }

        private void DoAfterPresent()
        {
            for (int i = 0; i < m_debugWaitForPresentHandleCount; i++)
            {
                EventWaitHandle handle;
                if (m_debugWaitForPresentHandles.TryDequeue(out handle) && handle != null)
                    handle.Set(); // release threads waiting for present
            }
            m_debugWaitForPresentHandleCount = 0;
        }

        public void DebugAddWaitingForPresent(EventWaitHandle handle)
        {
            m_debugWaitForPresentHandles.Enqueue(handle);
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
#if XB1
                    System.Diagnostics.Debug.Assert(false, "XB1 form not support fullscreen yet");
                    m_renderWindow.OnModeChanged(MyWindowModeEnum.Window, m_settings.BackBufferWidth, m_settings.BackBufferHeight);
#else
                    m_renderWindow.OnModeChanged(MyWindowModeEnum.Fullscreen, m_settings.BackBufferWidth, m_settings.BackBufferHeight);
#endif
                    break;

                case MyWindowModeEnum.FullscreenWindow:
					{
#if XB1
                        m_renderWindow.OnModeChanged(MyWindowModeEnum.FullscreenWindow, m_settings.BackBufferWidth, m_settings.BackBufferHeight);
#else
                        WinApi.DEVMODE mode = new WinApi.DEVMODE();
                        WinApi.EnumDisplaySettings(null, WinApi.ENUM_REGISTRY_SETTINGS, ref mode);
                        VRage.Trace.MyTrace.Watch("Registry display settings", string.Format("{0}x{1}", mode.dmPelsWidth, mode.dmPelsHeight));
						m_renderWindow.OnModeChanged(MyWindowModeEnum.FullscreenWindow, mode.dmPelsWidth, mode.dmPelsHeight);
#endif

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
            //MyRenderProxy.ClearBackbuffer(new ColorBGRA(0.0f));
            MyRenderProxy.ClearLargeMessages();
            ProfilerShort.End();

            ProfilerShort.Begin("MyRenderProxy.Draw");
            MyRenderProxy.Draw();
            ProfilerShort.End();

            MyRenderProxy.GetRenderProfiler().Draw();

            ProfilerShort.Begin("EndScene");
            MyRenderProxy.DrawEnd();
            ProfilerShort.End();
        }

        public void SetMouseCapture(bool capture)
        {
            m_renderWindow.SetMouseCapture(capture);
        }
    }
}
