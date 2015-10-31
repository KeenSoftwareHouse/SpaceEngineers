#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Utils;
using VRage.Win32;
using Color = VRageMath.Color;
using Vector2 = VRageMath.Vector2;


#endregion

namespace Sandbox.Graphics.GUI
{
    public class MyDX9Gui : IMyGuiSandbox
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public static int TotalGamePlayTimeInMilliseconds;

        //  Current debug screens 
        static MyGuiScreenDebugBase m_currentDebugScreen;
        MyGuiScreenMessageBox m_currentModErrorsMessageBox;
        MyGuiScreenDebugBase m_currentStatisticsScreen;

        bool m_debugScreensEnabled   = true;

        StringBuilder m_debugText = new StringBuilder();

        public readonly string GameLogoTexture = "Textures\\GUI\\GameLogoLarge.dds";

        public bool IsDebugScreenEnabled() { return m_debugScreensEnabled; }

        internal List<MyDebugComponent> UserDebugInputComponents = new List<MyDebugComponent>();


        public class MyScreenShot
        {
            public bool IgnoreSprites;
            public VRageMath.Vector2 SizeMultiplier;
            public string Path;
            public bool ShowNotification;

            public MyScreenShot(VRageMath.Vector2 sizeMultiplier, string path, bool ignoreSprites, bool showNotification)
            {
                IgnoreSprites = ignoreSprites;
                Path = path;
                SizeMultiplier = sizeMultiplier;
                ShowNotification = showNotification;
            }
        }

        Vector2 m_oldVisPos;
        Vector2 m_oldNonVisPos;
        bool m_oldMouseVisibilityState;
        public void SetMouseCursorVisibility(bool visible, bool changePosition = true)
        {
            if (m_oldMouseVisibilityState && visible != m_oldMouseVisibilityState)
            {
                //VRage.Trace.Trace.SendMsgLastCall(p.ToString());
                //VRage.Trace.Trace.SendMsgLastCall(m_oldNonVisPos.ToString());
                //VRage.Trace.Trace.SendMsgLastCall(m_oldVisPos.ToString());

                m_oldVisPos = MyInput.Static.GetMousePosition();
                m_oldMouseVisibilityState = visible;
                //m_oldNonVisPos = new Vector2(MySandboxGame.ScreenSizeHalf.X, MySandboxGame.ScreenSizeHalf.Y);

                // if (changePosition)
                //   MyGuiInput.SetMousePosition((int)m_oldNonVisPos.X, (int)m_oldNonVisPos.Y);
                //MyGuiInput.SetMouseToScreenCenter();
            }

            if (!m_oldMouseVisibilityState && visible != m_oldMouseVisibilityState)
            {
                //VRage.Trace.Trace.SendMsgLastCall(p.ToString());
                //VRage.Trace.Trace.SendMsgLastCall(m_oldNonVisPos.ToString());
                //VRage.Trace.Trace.SendMsgLastCall(m_oldVisPos.ToString());

                m_oldNonVisPos = MyInput.Static.GetMousePosition();
                m_oldMouseVisibilityState = visible;

                if (changePosition)
                    MyInput.Static.SetMousePosition((int)m_oldVisPos.X, (int)m_oldVisPos.Y);
            }
            MySandboxGame.Static.SetMouseVisible(visible);
        }

        //  This one cas be public and not-readonly because we may want to change it from other screens or controls
        public Vector2 MouseCursorPosition
        {
            get
            {
                return MyGuiManager.GetNormalizedMousePosition(MyInput.Static.GetMousePosition(),
                                                               MyInput.Static.GetMouseAreaSize());
            }
        }

        // If true, all screen without focus handles input
        bool m_wasInputToNonFocusedScreens = false;
        //StringBuilder m_inputSharingText = new StringBuilder("WARNING: Sharing input enabled (release ALT to disable it)");
        StringBuilder m_inputSharingText;
        StringBuilder m_renderOverloadedText = new StringBuilder("WARNING: Render is overloaded, optimize your scene!");

        bool m_shapeRenderingMessageBoxShown = false;

        List<Type> m_pausingScreenTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public MyDX9Gui()
        {
            MySandboxGame.Log.WriteLine("MyGuiManager()");

            if (MyFakes.ALT_AS_DEBUG_KEY)
            {
                m_inputSharingText = new StringBuilder("WARNING: Sharing input enabled (release ALT to disable it)");
            }
            else
            {
                m_inputSharingText = new StringBuilder("WARNING: Sharing input enabled (release Scroll Lock to disable it)");
            }

            UserDebugInputComponents.Add(new MyGlobalInputComponent());
            UserDebugInputComponents.Add(new MyCharacterInputComponent());
            UserDebugInputComponents.Add(new MyOndraInputComponent());
            UserDebugInputComponents.Add(new MyPetaInputComponent());
            UserDebugInputComponents.Add(new MyMartinInputComponent());
            UserDebugInputComponents.Add(new MyTomasInputComponent());
            UserDebugInputComponents.Add(new MyTestersInputComponent());
            UserDebugInputComponents.Add(new MyHonzaInputComponent());
            UserDebugInputComponents.Add(new MyCestmirDebugInputComponent());
            UserDebugInputComponents.Add(new MyAlexDebugInputComponent());
            UserDebugInputComponents.Add(new MyMichalDebugInputComponent());
            UserDebugInputComponents.Add(new MyAsteroidsDebugInputComponent());
            UserDebugInputComponents.Add(new MyRendererStatsComponent());
            UserDebugInputComponents.Add(new MyDanielDebugInputComponent());
            UserDebugInputComponents.Add(new MyRenderDebugInputComponent());
            LoadDebugInputsFromConfig();
        }

        /// <summary>
        /// Loads the data.
        /// </summary>
        public void LoadData()
        {
            ProfilerShort.Begin("MyScreenManager.LoadData");
            MyScreenManager.LoadData();
            ProfilerShort.BeginNextBlock("MyGuiManager.LoadData");
            MyGuiManager.LoadData();
            ProfilerShort.End();

            ProfilerShort.Begin("MyLanguage.CurrentLanguage set");
            MyLanguage.CurrentLanguage = MySandboxGame.Config.Language;
            ProfilerShort.End();

            if (MyFakes.SHOW_AUDIO_DEV_SCREEN)
            {
                ProfilerShort.Begin("MyGuiScreenDebugAudio");
                MyGuiScreenDebugAudio audioDebug = new MyGuiScreenDebugAudio();
                AddScreen(audioDebug);
                ProfilerShort.End();
            }
        }

        public void LoadContent(MyFontDescription[] fonts)
        {
            MySandboxGame.Log.WriteLine("MyGuiManager.LoadContent() - START");
            MySandboxGame.Log.IncreaseIndent();

            MyGuiManager.SetMouseCursorTexture(MyGuiConstants.CURSOR_ARROW);

            MyGuiManager.LoadContent(fonts);
            MyScreenManager.LoadContent();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyGuiManager.LoadContent() - END");
        }

        private void EnableSoundsBasedOnWindowFocus()
        {
            if (MySandboxGame.Static.WindowHandle == GetForegroundWindow() && MyScreenManager.GetScreenWithFocus() != null)
            { // allow
                // this works bad (	0007128: BUG B - audio sliders are broken)
                //MyAudio.Static.SetAllVolume(MyConfig.GameVolume, MyConfig.MusicVolume);         
                MyAudio.Static.Mute = false;
            }
            else // mute
            {
                // this works bad (	0007128: BUG B - audio sliders are broken)
                //MyAudio.Static.SetAllVolume(0,0);
                MyAudio.Static.Mute = true;
            }
        }

        public bool OpenSteamOverlay(string url)
        {
            if (MySteam.IsOverlayEnabled)
            {
                MySteam.OpenOverlayUrl(url);
                return true;
            }
            return false;
        }

        public void UnloadContent()
        {
            MyScreenManager.UnloadContent();
        }

        public void SwitchDebugScreensEnabled()
        {
            m_debugScreensEnabled = !m_debugScreensEnabled;
        }

        public void HandleRenderProfilerInput()
        {
            MyRenderProfiler.HandleInput();
        }

        public void AddScreen(MyGuiScreenBase screen)
        {
            MyScreenManager.AddScreen(screen);
        }

        public void RemoveScreen(MyGuiScreenBase screen)
        {
            MyScreenManager.RemoveScreen(screen);
        }

        //  Sends input (keyboard/mouse) to screen which has focus (top-most)
        public void HandleInput()
        {
            ProfilerShort.Begin("MyGuiManager.HandleInput");
            try
            {
                if (MyInput.Static.IsAnyAltKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.F4))
                {
                    MyAnalyticsTracker.SendGameEnd("Alt+F4", MySandboxGame.TotalTimeInMilliseconds / 1000);

                    //  Exit application
                    MySandboxGame.ExitThreadSafe();
                    return;
                }

                //  Screenshot(s)
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SCREENSHOT))
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    TakeScreenshot();
                }

                bool newPressf12 = MyInput.Static.IsNewKeyPressed(MyKeys.F12);
                bool newPressf2 = MyInput.Static.IsNewKeyPressed(MyKeys.F2);
                if ((newPressf2 || newPressf12) && MyInput.Static.IsAnyShiftKeyPressed() && MyInput.Static.IsAnyAltKeyPressed())
                {
                    if (MySession.Static != null && MySession.Static.CreativeMode)
                    {
                        if (newPressf12)
                        {
                            MyDebugDrawSettings.DEBUG_DRAW_PHYSICS = !MyDebugDrawSettings.DEBUG_DRAW_PHYSICS;
                            if (!m_shapeRenderingMessageBoxShown)
                            {
                                m_shapeRenderingMessageBoxShown = true;
                                AddScreen(MyGuiSandbox.CreateMessageBox(
                                    messageCaption: new StringBuilder("PHYSICS SHAPES"),
                                    messageText: new StringBuilder("Enabled physics shapes rendering. This feature is for modders only and is not part of the gameplay.")));
                            }
                        }
                    }
                    else
                    {
                        AddScreen(MyGuiSandbox.CreateMessageBox(
                            messageCaption: new StringBuilder("MODDING HELPER KEYS"),
                            messageText: new StringBuilder("Use of helper key combinations for modders is only allowed in creative mode.")));
                    }
                    return;
                }

                if (MyInput.Static.IsNewKeyPressed(MyKeys.H) && MyInput.Static.IsAnyCtrlKeyPressed())
                {
                    if (MyFakes.ENABLE_NETGRAPH)
                    {
                        MyHud.IsNetgraphVisible = !MyHud.IsNetgraphVisible;
                    }
                }

                if (MyInput.Static.IsNewKeyPressed(MyKeys.F11))
                {
                    if (MyInput.Static.IsAnyShiftKeyPressed() && !MyInput.Static.IsAnyCtrlKeyPressed())
                    {
                        SwitchTimingScreen();
                    }
                }

                if (MyFakes.ENABLE_MISSION_SCREEN && MyInput.Static.IsNewKeyPressed(MyKeys.U))
                {
                    MyScreenManager.AddScreen(new MyGuiScreenMission());
                }

                if (!MyInput.Static.ENABLE_DEVELOPER_KEYS && Sync.MultiplayerActive && m_currentDebugScreen is MyGuiScreenDebugOfficial)
                {
                    RemoveScreen(m_currentDebugScreen);
                    m_currentDebugScreen = null;
                }

                bool inputHandled = false;

                if (MyInput.Static.ENABLE_DEVELOPER_KEYS)
                {
                    //  Statistics screen
                    if (MyInput.Static.IsNewKeyPressed(MyKeys.F11) && !MyInput.Static.IsAnyShiftKeyPressed() && MyInput.Static.IsAnyCtrlKeyPressed())
                    {
                        SwitchStatisticsScreen();
                    }

                    if (MyInput.Static.IsAnyShiftKeyPressed() && MyInput.Static.IsAnyAltKeyPressed() && MyInput.Static.IsAnyCtrlKeyPressed()
                        && MyInput.Static.IsNewKeyPressed(MyKeys.Home))
                    {
                        throw new InvalidOperationException("Controlled crash");
                    }

                    // Forge GC to run
                    if (MyInput.Static.IsNewKeyPressed(MyKeys.Pause) && MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        GC.Collect(GC.MaxGeneration);
                    }

                    if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.F2))
                    {
                        //Reload textures
                        if (MyInput.Static.IsKeyPress(MyKeys.LeftShift))
                        {
                            VRageRender.MyRenderProxy.ReloadTextures();
                        }
                        else
                            if (MyInput.Static.IsKeyPress(MyKeys.LeftAlt))
                            {
                                VRageRender.MyRenderProxy.ReloadModels();
                            }
                            else
                            {
                                VRageRender.MyRenderProxy.ReloadEffects();
                            }
                    }

                    //WS size
                    if (MyInput.Static.IsNewKeyPressed(MyKeys.F3) && MyInput.Static.IsKeyPress(MyKeys.LeftShift))
                    {
                        WinApi.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                    }

                    if (MyInput.Static.IsNewKeyPressed(MyKeys.F12))
                    {
                        ShowDeveloperDebugScreen();
                    }

                    if (MyFakes.ALT_AS_DEBUG_KEY)
                    {
                        MyScreenManager.InputToNonFocusedScreens = MyInput.Static.IsAnyAltKeyPressed() && !MyInput.Static.IsKeyPress(MyKeys.Tab);
                    }
                    else
                    {
                        MyScreenManager.InputToNonFocusedScreens = MyInput.Static.IsKeyPress(MyKeys.ScrollLock) && !MyInput.Static.IsKeyPress(MyKeys.Tab);
                    }

                    if (MyScreenManager.InputToNonFocusedScreens != m_wasInputToNonFocusedScreens)
                    {
                        if (MyScreenManager.InputToNonFocusedScreens && m_currentDebugScreen != null)
                        {
                            SetMouseCursorVisibility(MyScreenManager.InputToNonFocusedScreens);
                        }

                        m_wasInputToNonFocusedScreens = MyScreenManager.InputToNonFocusedScreens;
                    }

                    inputHandled = HandleDebugInput();
                }

                if (!inputHandled)
                    MyScreenManager.HandleInput();
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public static void SwitchModDebugScreen()
        {
            if (MyInput.Static.ENABLE_DEVELOPER_KEYS || !Sync.MultiplayerActive)
            {
                if (m_currentDebugScreen != null)
                {
                    if (m_currentDebugScreen is MyGuiScreenDebugOfficial)
                    {
                        m_currentDebugScreen.CloseScreen();
                        m_currentDebugScreen = null;
                    }
                }
                else
                {
                    ShowModDebugScreen();
                }
            }
        }

        private static void ShowModDebugScreen()
        {
            if (m_currentDebugScreen == null)
            {
                MyScreenManager.AddScreen(m_currentDebugScreen = new MyGuiScreenDebugOfficial());
                m_currentDebugScreen.Closed += (screen) => m_currentDebugScreen = null;
            }
            else if (m_currentDebugScreen is MyGuiScreenDebugOfficial)
            {
                m_currentDebugScreen.RecreateControls(false);
            }
        }

        private void ShowModErrorsMessageBox()
        {
            var errors = MyDefinitionErrors.GetErrors();

            if (m_currentModErrorsMessageBox != null)
            {
                RemoveScreen(m_currentModErrorsMessageBox);
            }

            var errorMessage = MyTexts.Get(MySpaceTexts.MessageBoxErrorModLoadingFailure);
            errorMessage.Append("\n");

            foreach (var error in errors)
            {
                if (error.Severity == ErrorSeverity.Critical && error.ModName != null)
                {
                    errorMessage.Append("\n");
                    errorMessage.Append(error.ModName);
                }
            }
            errorMessage.Append("\n");

            m_currentModErrorsMessageBox = MyGuiSandbox.CreateMessageBox(messageText: errorMessage, messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError));
            AddScreen(m_currentModErrorsMessageBox);
        }

        public void ShowModErrors()
        {
            if (MyInput.Static.ENABLE_DEVELOPER_KEYS || !Sync.MultiplayerActive)
            {
                ShowModDebugScreen();
            }
            else
            {
                ShowModErrorsMessageBox();
            }
        }

        private void ShowDeveloperDebugScreen()
        {
            if (!(m_currentDebugScreen is MyGuiScreenDebugOfficial) && !(m_currentDebugScreen is MyGuiScreenDebugDeveloper))
            {
                if (m_currentDebugScreen != null)
                    RemoveScreen(m_currentDebugScreen);
                var devScreen = new MyGuiScreenDebugDeveloper();

                AddScreen(m_currentDebugScreen = devScreen);
                m_currentDebugScreen.Closed += (screen) => m_currentDebugScreen = null;
            }
        }

        bool m_cameraControllerMovementAllowed;
        bool m_lookAroundEnabled;

        //  Sends input (keyboard/mouse) to screen which has focus (top-most)
        public void HandleInputAfterSimulation()
        {
            bool cameraControllerMovementAllowed = MyScreenManager.GetScreenWithFocus() == MyGuiScreenGamePlay.Static && MyGuiScreenGamePlay.Static != null && !MyScreenManager.InputToNonFocusedScreens;
            bool lookAroundEnabled = MyInput.Static.IsGameControlPressed(MyControlsSpace.LOOKAROUND) || (MySession.ControlledEntity != null && MySession.ControlledEntity.PrimaryLookaround);
            
            //After respawn, the controlled object might be null
            bool shouldStopControlledObject = MySession.ControlledEntity != null && (!cameraControllerMovementAllowed && m_cameraControllerMovementAllowed != cameraControllerMovementAllowed);

            if (MySession.Static != null)
            {
                bool movementAllowedInPause = MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator ||
                                              MySession.GetCameraControllerEnum() == MyCameraControllerEnum.SpectatorDelta ||
                                              MySession.GetCameraControllerEnum() == MyCameraControllerEnum.SpectatorFixed;
                bool rotationAllowedInPause = movementAllowedInPause || MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator;
				bool devScreenFlag = MyScreenManager.GetScreenWithFocus() is MyGuiScreenDebugBase && !MyInput.Static.IsAnyAltKeyPressed();
                MyCameraControllerEnum cce = MySession.GetCameraControllerEnum();

                float rollIndicator = MyInput.Static.GetRoll();
                Vector2 rotationIndicator = MyInput.Static.GetRotation();
                VRageMath.Vector3 moveIndicator = MyInput.Static.GetPositionDelta();

                if (MySandboxGame.IsPaused)
                {
                    if (!movementAllowedInPause && !rotationAllowedInPause)
                        return;

                    if (!movementAllowedInPause)
                        moveIndicator = VRageMath.Vector3.Zero;
                    if (!rotationAllowedInPause || devScreenFlag)
                    {
                        rollIndicator = 0.0f;
                        rotationIndicator = Vector2.Zero;
                    }

                    MySession.Static.CameraController.Rotate(rotationIndicator, rollIndicator);
                }
                else if (lookAroundEnabled)
                {
                    if (cameraControllerMovementAllowed)
                    {
                        //Then move camera (because it can be dependent on control object)
                        MySession.Static.CameraController.Rotate(rotationIndicator, rollIndicator);

                        if (!m_lookAroundEnabled && shouldStopControlledObject)
                        {
                            MySession.ControlledEntity.MoveAndRotateStopped();
                        }
                    }

                    if (shouldStopControlledObject)
                    {
                        MySession.Static.CameraController.RotateStopped();
                    }
                }
                //Hack to make spectators work until they are made entities
                else if (MySession.Static.CameraController is MySpectatorCameraController && MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.ConstantDelta)
                {
                    if (cameraControllerMovementAllowed)
                    {
                        MySpectatorCameraController.Static.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
                    }
                }

                if (shouldStopControlledObject)
                {
                    MySession.ControlledEntity.MoveAndRotateStopped();
                }
            }

            m_cameraControllerMovementAllowed = cameraControllerMovementAllowed;
            m_lookAroundEnabled = lookAroundEnabled;
        }

        private void SwitchTimingScreen()
        {
            if (!(m_currentStatisticsScreen is MyGuiScreenDebugTiming))
            {
                if (m_currentStatisticsScreen != null)
                    RemoveScreen(m_currentStatisticsScreen);
                AddScreen(m_currentStatisticsScreen = new MyGuiScreenDebugTiming());
            }
            else
            {
                RemoveScreen(m_currentStatisticsScreen);
                m_currentStatisticsScreen = null;
            }
        }

        private void SwitchStatisticsScreen()
        {
            if (!(m_currentStatisticsScreen is MyGuiScreenDebugStatistics))
            {
                if (m_currentStatisticsScreen != null)
                    RemoveScreen(m_currentStatisticsScreen);
                AddScreen(m_currentStatisticsScreen = new MyGuiScreenDebugStatistics());
            }
            else
            {
                RemoveScreen(m_currentStatisticsScreen);
                m_currentStatisticsScreen = null;
            }
        }

        private void SwitchInputScreen()
        {
            if (!(m_currentStatisticsScreen is MyGuiScreenDebugInput))
            {
                if (m_currentStatisticsScreen != null)
                    RemoveScreen(m_currentStatisticsScreen);
                AddScreen(m_currentStatisticsScreen = new MyGuiScreenDebugInput());
            }
            else
            {
                RemoveScreen(m_currentStatisticsScreen);
                m_currentStatisticsScreen = null;
            }
        }

        //  Update all screens
        public void Update(int totalTimeInMS)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGuiSandbox::Update1");

            HandleRenderProfilerInput();
            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("MyGuiSandbox::Update2");

            TotalGamePlayTimeInMilliseconds = totalTimeInMS;

            MyScreenManager.Update(totalTimeInMS);

            MyGuiScreenBase screenWithFocus = MyScreenManager.GetScreenWithFocus();

            bool gameFocused = (MySandboxGame.Static.IsActive == true
                &&
                ((Sandbox.AppCode.MyExternalAppBase.Static == null && MySandboxGame.Static.WindowHandle == GetForegroundWindow())
                ||
                (Sandbox.AppCode.MyExternalAppBase.Static != null && !Sandbox.AppCode.MyExternalAppBase.IsEditorActive))
                );

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("MyGuiSandbox::Update3");
            //We have to know current focus screen because of centerize mouse
            MyInput.Static.Update(gameFocused);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("MyGuiSandbox::Update4");
            MyGuiManager.Update(totalTimeInMS);
            MyGuiManager.MouseCursorPosition = MouseCursorPosition;


            MyGuiManager.Camera = MySector.MainCamera != null ? MySector.MainCamera.WorldMatrix : VRageMath.MatrixD.Identity;
            MyGuiManager.CameraView = MySector.MainCamera != null ? MySector.MainCamera.ViewMatrix : VRageMath.MatrixD.Identity;
            MyGuiManager.TotalTimeInMilliseconds = MySandboxGame.TotalTimeInMilliseconds;

            //We should not need this call
            //EnableSoundsBasedOnWindowFocus();

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        void DrawMouseCursor(string mouseCursorTexture)
        {
            if (mouseCursorTexture == null)
                return;

            Vector2 cursorSize = MyGuiManager.GetNormalizedSize(new Vector2(64), MyGuiConstants.MOUSE_CURSOR_SCALE);

            MyGuiManager.DrawSpriteBatch(mouseCursorTexture, MouseCursorPosition + (cursorSize / 2.0f), MyGuiConstants.MOUSE_CURSOR_SCALE, new Color(MyGuiConstants.MOUSE_CURSOR_COLOR),
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, 0, new Vector2(0.5f, 0.5f));
        }

        string GetMouseOverTexture(MyGuiScreenBase screen)
        {
            if (screen != null)
            {
                var mouseOverControl = screen.GetMouseOverControl();
                if (mouseOverControl != null)
                {
                    return mouseOverControl.GetMouseCursorTexture() ?? MyGuiManager.GetMouseCursorTexture();
                }
            }
            return MyGuiManager.GetMouseCursorTexture();
        }

        //  Draw all screens
        public void Draw()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGuiSandbox::Draw");

            MyGuiManager.Camera = MySector.MainCamera != null ? MySector.MainCamera.WorldMatrix : VRageMath.MatrixD.Identity;
            MyGuiManager.CameraView = MySector.MainCamera != null ? MySector.MainCamera.ViewMatrix : VRageMath.MatrixD.Identity;

            TransparentGeometry.MyTransparentGeometry.Camera = MyGuiManager.Camera;
            TransparentGeometry.MyTransparentGeometry.CameraView = MyGuiManager.CameraView; 

            ProfilerShort.Begin("ScreenManager.Draw");
            MyScreenManager.Draw();
            ProfilerShort.End();

            m_debugText.Clear();

            if (MyInput.Static.ENABLE_DEVELOPER_KEYS && MySandboxGame.Config.DebugComponentsInfo != MyDebugComponent.MyDebugComponentInfoState.NoInfo)
            {
                var h = 0f;
                var i = 0;
                bool drawBackground = false;

                MyDebugComponent.ResetFrame();

                foreach (var userInputComponent in UserDebugInputComponents)
                {
                    if (userInputComponent.Enabled)
                    {
                        if (h == 0)
                        {
                            m_debugText.AppendLine("Debug input:");
                            m_debugText.AppendLine();
                            h += 0.0630f;
                        }
                        m_debugText.ConcatFormat("{0} (Ctrl + numPad{1})", UserDebugInputComponents[i].GetName(), i);
                        m_debugText.AppendLine();
                        h += 0.0265f;
                        userInputComponent.Draw();
                        drawBackground = true;
                    }
                    ++i;
                }

                if (drawBackground)
                {
                    MyGuiManager.DrawSpriteBatch(@"Textures\GUI\Controls\rectangle_dark_center.dds",
                                                 new Vector2(MyGuiManager.GetMaxMouseCoord().X, 0f),
                                                 new Vector2(MyGuiManager.MeasureString(MyFontEnum.White, m_debugText, 1f).X + 0.012f, h),
                                                 new Color(0, 0, 0, 130),
                                                 MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
                    MyGuiManager.DrawString(MyFontEnum.White, m_debugText, new Vector2(MyGuiManager.GetMaxMouseCoord().X - 0.01f, 0f), 1f, Color.White, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
                }
            }

            bool hwCursor = MyVideoSettingsManager.IsHardwareCursorUsed();

            var screenWithFocus = MyScreenManager.GetScreenWithFocus();
            if (((screenWithFocus != null) && (screenWithFocus.GetDrawMouseCursor() == true)) || (MyScreenManager.InputToNonFocusedScreens && MyScreenManager.GetScreensCount() > 1))
            {
                SetMouseCursorVisibility(hwCursor, false);

                if (!hwCursor || MyFakes.FORCE_SOFTWARE_MOUSE_DRAW)
                    DrawMouseCursor(GetMouseOverTexture(screenWithFocus));
            }
            else
            {
                if (hwCursor)
                {
                    if (screenWithFocus != null)
                    {
                        SetMouseCursorVisibility(screenWithFocus.GetDrawMouseCursor());
                    }
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private void AddIntroScreen()
        {
            if (MyFakes.ENABLE_MENU_VIDEO_BACKGROUND && !MyCompilationSymbols.MemoryProfiling)
            {
                AddScreen(MyGuiScreenIntroVideo.CreateBackgroundScreen());
            }
        }

        public void BackToIntroLogos(Action afterLogosAction)
        {
            MyScreenManager.CloseAllScreensNowExcept(null);

            string[] logos = new string[]
            {
                //"Textures\\Logo\\keen_swh",
                //"Textures\\Logo\\game",
                //"Textures\\Logo\\vrage",
            };

            MyGuiScreenBase previousScreen = null;

            foreach (var logo in logos)
            {
                var logoScreen = new MyGuiScreenLogo(logo);
                if (previousScreen != null)
                    AddCloseHandler(previousScreen, logoScreen, afterLogosAction);
                else
                    AddScreen(logoScreen);

                previousScreen = logoScreen;
            }

            if (previousScreen != null)
                previousScreen.Closed += (screen) => afterLogosAction();
            else
                afterLogosAction();
        }

        private void AddCloseHandler(MyGuiScreenBase previousScreen, MyGuiScreenLogo logoScreen, Action afterLogosAction)
        {
            previousScreen.Closed += (screen) =>
            {
                if (!screen.Cancelled)
                    AddScreen(logoScreen);
                else
                    afterLogosAction();
            };
        }

        public void BackToMainMenu()
        {
            AddIntroScreen();
            MyGuiScreenMainMenu.AddMainMenu();
        }

        public float GetDefaultTextScaleWithLanguage()
        {
            return MyGuiConstants.DEFAULT_TEXT_SCALE * MyGuiManager.LanguageTextScale;
        }

        public void TakeScreenshot(int width, int height, string saveToPath = null, bool ignoreSprites = false, bool showNotification = true)
        {
            TakeScreenshot(saveToPath, ignoreSprites, new Vector2(width, height) / MySandboxGame.ScreenSize, showNotification);
        }

        public void TakeScreenshot(string saveToPath = null, bool ignoreSprites = false, Vector2? sizeMultiplier = null, bool showNotification = true)
        {
            //  Screenshot object survives only one DRAW after created. We delete it immediatelly. So if 'm_screenshot'
            //  is not null we know we have to take screenshot and set it to null.

            //if (m_screenshot != null)
            //    return;

            if (!sizeMultiplier.HasValue)
                sizeMultiplier = new Vector2(MySandboxGame.Config.ScreenshotSizeMultiplier);

            //var screenshot = new MyScreenShot(sizeMultiplier.Value, saveToPath, ignoreSprites, showNotification);

            VRageRender.MyRenderProxy.TakeScreenshot(sizeMultiplier.Value, saveToPath, false, ignoreSprites, showNotification);
        }

        public Vector2 GetNormalizedCoordsAndPreserveOriginalSize(int width, int height)
        {
            return new Vector2((float)width / (float)MySandboxGame.ScreenSize.X, (float)height / (float)MySandboxGame.ScreenSize.Y);
        }

        public void DrawGameLogo(float transitionAlpha)
        {
            Color colorForeground = new Color(1, 1, 1, transitionAlpha);
            MyGuiManager.DrawSpriteBatch(GameLogoTexture, new Vector2(0.5f, 0.15f),
                                         new Vector2(702 / 1600f, 237 / 1200f), colorForeground,
                                         MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }


        #region Debug

        private bool HandleDebugInput()
        {
            if (MyInput.Static.IsAnyCtrlKeyPressed())
            {
                int indexOfDebugComponent = -1;
                for (int i = 0; i < 10; i++)
                {
                    if (MyInput.Static.IsNewKeyPressed((MyKeys)((int)MyKeys.NumPad0 + i)))
                    {
                        indexOfDebugComponent = i;
                        if (MyInput.Static.IsAnyAltKeyPressed())
                        {
                            indexOfDebugComponent += 10;
                        }
                        break;
                    }

                }

                if (indexOfDebugComponent > -1 && indexOfDebugComponent < UserDebugInputComponents.Count)
                {
                    var debugComponent = UserDebugInputComponents[indexOfDebugComponent];
                    debugComponent.Enabled = !debugComponent.Enabled;

                    SaveDebugInputsToConfig();

                    return false;
                }
            }

            bool handled = false;

            foreach (var userInputComponent in UserDebugInputComponents)
            {
                if (userInputComponent.Enabled && !MyInput.Static.IsAnyAltKeyPressed())
                {
                    handled = userInputComponent.HandleInput() || handled;
                }

                if (handled)
                    break;
            }

            return handled;
        }

        void SaveDebugInputsToConfig()
        {
            MySandboxGame.Config.DebugInputComponents.Dictionary.Clear();

            foreach (var debugInput in UserDebugInputComponents)
            {
                MySandboxGame.Config.DebugInputComponents[debugInput.GetName()] = debugInput.Enabled.ToString();
            }

            MySandboxGame.Config.Save();
        }

        void LoadDebugInputsFromConfig()
        {
            foreach (var pair in MySandboxGame.Config.DebugInputComponents.Dictionary)
            {
                for (int i = 0; i < UserDebugInputComponents.Count; i++)
                {
                    if (UserDebugInputComponents[i].GetName() == pair.Key)
                    {
                        UserDebugInputComponents[i].Enabled = bool.Parse(pair.Value.ToString());
                    }
                }
            }
        }

        #endregion
    }
}