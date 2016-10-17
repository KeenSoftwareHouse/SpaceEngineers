#if !XB1
using Sandbox.Engine.Utils;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
#if !XB1
using System.Windows.Forms;
#endif
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.ExternalApp;

namespace Sandbox
{
    public class MySandboxExternal : MySandboxGame
    {
        public readonly IExternalApp ExternalApp;
        MyRenderDeviceSettings m_currentSettings;
#if !XB1
        Control m_control;
#endif

        public MySandboxExternal(IExternalApp externalApp, VRageGameServices services, string[] commandlineArgs, IntPtr windowHandle)
            : base(services, commandlineArgs)
        {
            WindowHandle = windowHandle;
            ExternalApp = externalApp;
#if !XB1
            m_control = Control.FromHandle(windowHandle);
#endif
        }

        public override void SwitchSettings(MyRenderDeviceSettings settings)
        {
            m_currentSettings = settings;
            m_currentSettings.WindowMode = MyWindowModeEnum.Window;
            base.SwitchSettings(m_currentSettings);
        }

        protected override void StartRenderComponent(MyRenderDeviceSettings? settings)
        {
            DrawThread = Thread.CurrentThread;

            MyRenderWindow wnd = new MyRenderWindow();
#if XB1
			System.Diagnostics.Debug.Assert(false);
#else
            wnd.Control = Control.FromHandle(WindowHandle);            
            wnd.TopLevelForm = (Form)wnd.Control.TopLevelControl;
            wnd.TopLevelForm.Tag = wnd;

            m_bufferedInputSource = wnd;

            wnd.TopLevelForm.KeyPress += TopLevelForm_KeyPress;

            m_windowCreatedEvent.Set();
            ((Form)wnd.TopLevelForm).FormClosed += (o, e) => ExitThreadSafe();
#endif

			Action showCursor = () =>
            {
                //if (!wnd.TopLevelForm.IsDisposed)
                //wnd.TopLevelForm.ShowCursor = true;
            };
            Action hideCursor = () =>
            {
                //if (!wnd.TopLevelForm.IsDisposed)
                //wnd.TopLevelForm.ShowCursor = false;
            };
            m_setMouseVisible = (b) =>
            {
                // In case of crash, this may be null, don't want subsequent crash
                var component = GameRenderComponent;
                if (component != null)
                {
                    var renderThread = component.RenderThread;
                    if (renderThread != null)
                    {
                        renderThread.Invoke(b ? showCursor : hideCursor);
                    }
                }
            };

            if (settings == null)
            {
                settings = new MyRenderDeviceSettings(0, MyWindowModeEnum.Window, wnd.Control.ClientSize.Width, wnd.Control.ClientSize.Height, 0, false, MyCompilationSymbols.DX11ForceStereo, false);
            }

            GameRenderComponent.StartSync(m_gameTimer, wnd, settings, MyRenderQualityEnum.NORMAL, MyPerGameSettings.MaxFrameRate);
            GameRenderComponent.RenderThread.SizeChanged += RenderThread_SizeChanged;
            GameRenderComponent.RenderThread.BeforeDraw += RenderThread_BeforeDraw;

            VRageRender.MyViewport vp = new MyViewport(0, 0, wnd.Control.ClientSize.Width, wnd.Control.ClientSize.Height);
            RenderThread_SizeChanged(wnd.Control.ClientSize.Width, wnd.Control.ClientSize.Height, vp);
        }

        void TopLevelForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            ((MyRenderWindow)((Form)sender).Tag).AddChar(e.KeyChar);
        }

        //protected override void Draw()
        //{
        //    ExternalApp.Draw();
        //    base.Draw();
        //}

        protected override void Update()
        {
#if XB1
			System.Diagnostics.Debug.Assert(false);
#else
            if (GameRenderComponent.RenderThread != null)
            {
                var size = m_control.ClientSize;
                if ((m_currentSettings.BackBufferWidth != size.Width || m_currentSettings.BackBufferHeight != size.Height) && size.Height > 0 && size.Width > 0)
                {
                    MyRenderDeviceSettings settings = new MyRenderDeviceSettings();
                    settings.AdapterOrdinal = m_currentSettings.AdapterOrdinal;
                    settings.RefreshRate = m_currentSettings.RefreshRate;
                    settings.VSync = m_currentSettings.VSync;
                    settings.WindowMode = m_currentSettings.WindowMode;

                    settings.BackBufferHeight = size.Height;
                    settings.BackBufferWidth = size.Width;
                    SwitchSettings(settings);
                }
                GameRenderComponent.RenderThread.TickSync();
            }

            ExternalApp.Update();
            base.Update();
#endif
        }
    }
}
#endif // !XB1
