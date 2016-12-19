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
using Sandbox.Game.Screens;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Sandbox.Game.GUI.DebugInputComponents;
using VRage;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Utils;
using VRage.Win32;
using Color = VRageMath.Color;
using Vector2 = VRageMath.Vector2;
using VRage.Game;
using VRage.Profiler;
using VRageRender;

#endregion

namespace Sandbox.Graphics.GUI
{
    public class MyDX9Gui : IMyGuiSandbox
    {
#if !XB1
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
#endif // !XB1

        public static int TotalGamePlayTimeInMilliseconds;

        //  Current debug screens 
        static MyGuiScreenDebugBase m_currentDebugScreen;
        MyGuiScreenMessageBox m_currentModErrorsMessageBox;
        MyGuiScreenDebugBase m_currentStatisticsScreen;

        bool m_debugScreensEnabled = true;

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
            UserDebugInputComponents.Add(new MyPlanetsDebugInputComponent());
            UserDebugInputComponents.Add(new MyRenderDebugInputComponent());
            UserDebugInputComponents.Add(new MyComponentsDebugInputComponent());
            UserDebugInputComponents.Add(new MyVoxelDebugInputComponent());
#if !XB1 // XB1_NOOPENVRWRAPPER
            UserDebugInputComponents.Add(new MyVRDebugInputComponent());
#endif // !XB1
            UserDebugInputComponents.Add(new MyResearchDebugInputComponent());
            UserDebugInputComponents.Add(new MyVisualScriptingDebugInputComponent());
            UserDebugInputComponents.Add(new MyAIDebugInputComponent());
            UserDebugInputComponents.Add(new MyAlesDebugInputComponent());
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
#if !XB1
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
#endif // !XB1
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

                if (MySession.Static != null && MySession.Static.CreativeMode
                      || MyInput.Static.ENABLE_DEVELOPER_KEYS)
                    F12Handling();

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
                            MyDefinitionManager.Static.ReloadDecalMaterials();
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
#if !XB1
                        WinApi.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
#endif // !XB1
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
        private void F12Handling()
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.F12))
            {
                if (MyInput.Static.ENABLE_DEVELOPER_KEYS)
                    ShowDeveloperDebugScreen();
                else
                {
                    if (m_currentDebugScreen is MyGuiScreenDebugDeveloper)
                    {
                        RemoveScreen(m_currentDebugScreen);
                        m_currentDebugScreen = null;
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                           messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxF12Question),
                           messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextF12Question),
                           buttonType: MyMessageBoxButtonsType.YES_NO,
                           callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                           {
                               if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                                   ShowDeveloperDebugScreen();
                           }));
                    }
                }
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

            var errorMessage = MyTexts.Get(MyCommonTexts.MessageBoxErrorModLoadingFailure);
            errorMessage.Append("\n");

            foreach (var error in errors)
            {
                if (error.Severity == TErrorSeverity.Critical && error.ModName != null)
                {
                    errorMessage.Append("\n");
                    errorMessage.Append(error.ModName);
                }
            }
            errorMessage.Append("\n");

            m_currentModErrorsMessageBox = MyGuiSandbox.CreateMessageBox(messageText: errorMessage, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
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
            if (MySession.Static != null)
            {
                bool cameraControllerMovementAllowed = MyScreenManager.GetScreenWithFocus() == MyGuiScreenGamePlay.Static && MyGuiScreenGamePlay.Static != null && !MyScreenManager.InputToNonFocusedScreens;
                bool lookAroundEnabled = MyInput.Static.IsGameControlPressed(MyControlsSpace.LOOKAROUND) || (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.PrimaryLookaround);

                //After respawn, the controlled object might be null
                bool shouldStopControlledObject = MySession.Static.ControlledEntity != null && (!cameraControllerMovementAllowed && m_cameraControllerMovementAllowed != cameraControllerMovementAllowed);

                bool movementAllowedInPause = MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator ||
                                              MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.SpectatorDelta ||
                                              MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.SpectatorFixed ||
                                              MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.SpectatorOrbit;
                bool rotationAllowedInPause = movementAllowedInPause;   //GK: consider removing if in the future is not different from movementAllowed
                bool devScreenFlag = MyScreenManager.GetScreenWithFocus() is MyGuiScreenDebugBase && !MyInput.Static.IsAnyAltKeyPressed();
                MyCameraControllerEnum cce = MySession.Static.GetCameraControllerEnum();

                float rollIndicator = MyInput.Static.GetRoll();
                Vector2 rotationIndicator = MyInput.Static.GetRotation();
                VRageMath.Vector3 moveIndicator = MyInput.Static.GetPositionDelta();

                var focusScreen = MyScreenManager.GetScreenWithFocus();

                if (MySandboxGame.IsPaused && focusScreen is MyGuiScreenGamePlay)
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
                            MySession.Static.ControlledEntity.MoveAndRotateStopped();
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

                MyScreenManager.HandleInputAfterSimulation();
                
                if (shouldStopControlledObject)
                {
                    MySession.Static.ControlledEntity.MoveAndRotateStopped();
                }

                m_cameraControllerMovementAllowed = cameraControllerMovementAllowed;
                m_lookAroundEnabled = lookAroundEnabled;
            }
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
                Debug.Assert(MyRenderProxy.DrawRenderStats != MyRenderProxy.MyStatsState.NoDraw);
                if (MyRenderProxy.DrawRenderStats == MyRenderProxy.MyStatsState.ShouldFinish)
                {
                    // We finished cycling through stat groups
                    RemoveScreen(m_currentStatisticsScreen);
                    m_currentStatisticsScreen = null;
                }
                else
                {
                    MyRenderProxy.DrawRenderStats = MyRenderProxy.MyStatsState.MoveNext;
                }
            }
        }

        private void SwitchStatisticsScreen()
        {
#if !XB1
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
#else // XB1
            System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
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

#if !XB1
            bool gameFocused = (MySandboxGame.Static.IsActive == true
                &&
                ((Sandbox.AppCode.MyExternalAppBase.Static == null && MySandboxGame.Static.WindowHandle == GetForegroundWindow())
                ||
                (Sandbox.AppCode.MyExternalAppBase.Static != null && !Sandbox.AppCode.MyExternalAppBase.IsEditorActive))
                );
#else // XB1
            bool gameFocused = (MySandboxGame.Static.IsActive == true
                &&
                ((Sandbox.AppCode.MyExternalAppBase.Static == null)
                ||
                (Sandbox.AppCode.MyExternalAppBase.Static != null && !Sandbox.AppCode.MyExternalAppBase.IsEditorActive))
                );
#endif // XB1

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("MyGuiSandbox::Update3");
            //We have to know current focus screen because of centerize mouse
            MyInput.Static.Update(gameFocused);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("MyGuiSandbox::Update4");
            MyGuiManager.Update(totalTimeInMS);
            MyGuiManager.MouseCursorPosition = MouseCursorPosition;


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
                        if (MySession.Static != null)
                            userInputComponent.DispatchUpdate();
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
#if XB1
                SetMouseCursorVisibility(false, false);
                DrawMouseCursor(GetMouseOverTexture(screenWithFocus));
#else
                SetMouseCursorVisibility(hwCursor, false);

                if (!hwCursor || MyFakes.FORCE_SOFTWARE_MOUSE_DRAW)
                    DrawMouseCursor(GetMouseOverTexture(screenWithFocus));
#endif
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
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.MainMenu));
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

            var inputs = MySandboxGame.Config.DebugInputComponents;
            foreach (var debugInput in UserDebugInputComponents)
            {
                var name = debugInput.GetName();

                MyConfig.MyDebugInputData data;
                inputs.Dictionary.TryGetValue(name, out data);
                data.Enabled = debugInput.Enabled;
                data.Data = debugInput.InputData;

                inputs[name] = data;
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
                        UserDebugInputComponents[i].Enabled = pair.Value.Enabled;
                        try
                        {
                            UserDebugInputComponents[i].InputData = pair.Value.Data;
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        #endregion
    }
}