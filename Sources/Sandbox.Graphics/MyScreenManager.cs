#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Profiler;
using VRage.Utils;



#endregion

//  This is static manager for all GUI screens. Use it when adding new screens.

//  Normalized coordinates - is in interval <0..1> on horizontal axis, and now I am not 100% but vertical is maybe also <0..1> or <0..something> where
//  'something' is defined by aspect ratio.

//  Screen coordinates - standard pixel coordinate on interval e.g. <0..1280>

//  IMPORTANT FOR RENDERING:
//  We call Begin on default sprite batch as first thing in MyGuiManager.Draw method. It's OK for most of the screens and controls, but 
//  we have to call End and then again Begin inside GamePlay screen - because it does a lot of 3D rendering and state changes.
//  Same applies for controls that do stencil-mask, they need to restart our sprite batch again.
//  Advantage is that almost all screens and controls are batched and rendered in just one draw call (they are deferred)

namespace Sandbox.Graphics.GUI
{
    public static class MyScreenManager
    {
#if !XB1
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
#endif // !XB1

        public static int TotalGamePlayTimeInMilliseconds;

        static MyGuiScreenBase m_lastScreenWithFocus;
        public static MyGuiScreenBase LastScreenWithFocus { get { return m_lastScreenWithFocus; } }

        //  List of screens - works like stack, on the top is screen that has focus
        static List<MyGuiScreenBase> m_screens;

        //  Used only when adding / removing screens - because we can't alter m_screens during iterator looping
        static List<MyGuiScreenBase> m_screensToRemove;
        static List<MyGuiScreenBase> m_screensToAdd;

        public static MyGuiControlBase FocusedControl
        {
            get
            {
                var focusedScreen = GetScreenWithFocus();
                return (focusedScreen != null) ? focusedScreen.FocusedControl : null;
            }
        }

        // If true, all screen without focus handles input
        static bool m_inputToNonFocusedScreens = false;
        static bool m_wasInputToNonFocusedScreens = false;
        public static bool InputToNonFocusedScreens
        {
            get
            {
                return m_inputToNonFocusedScreens;
            }
            set
            {
                m_inputToNonFocusedScreens = value;
            }
        }

        public static event Action<MyGuiScreenBase> ScreenAdded;
        public static event Action<MyGuiScreenBase> ScreenRemoved;

        /// <summary>
        /// Corrently active screens.
        /// </summary>
        public static IEnumerable<MyGuiScreenBase> Screens
        {
            get { return m_screens; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        static MyScreenManager()
        {
            MyLog.Default.WriteLine("MyScreenManager()");

            m_screens = new List<MyGuiScreenBase>();
            m_screensToRemove = new List<MyGuiScreenBase>();
            m_screensToAdd = new List<MyGuiScreenBase>();
        }

        /// <summary>
        /// Loads the data.
        /// </summary>
        public static void LoadData()
        {
            m_screens.Clear();
            m_screensToRemove.Clear();
            m_screensToAdd.Clear();

            //if (MyFakes.SHOW_AUDIO_DEV_SCREEN)
            //{
            //    MyGuiScreenDebugAudio audioDebug = new MyGuiScreenDebugAudio();
            //    AddScreen(audioDebug);
            //}
        }
        
        public static void LoadContent()
        {
            MyLog.Default.WriteLine("MyGuiManager.LoadContent() - START");
            MyLog.Default.IncreaseIndent();

            //load/reload content of all screens in first call any screens should not exist
            foreach (MyGuiScreenBase screen in m_screens)
            {
                screen.LoadContent();
            }

            MyLog.Default.DecreaseIndent();
            MyLog.Default.WriteLine("MyGuiManager.LoadContent() - END");
        }

        public static void RecreateControls()
        {
            //  GUI probably not initialized yet
            if (m_screens == null) return;

            for (int i = 0; i < m_screens.Count; i++)
            {
                m_screens[i].RecreateControls(false);
            }
        }

        //  Close screen of specified type with fade-out effect (ignores inheritance, base class, derived classes)
        public static void CloseScreen(Type screenType)
        {
            //  GUI probably not initialized yet
            if (m_screens == null) return;

            for (int i = 0; i < m_screens.Count; i++)
            {
                if (m_screens[i].GetType() == screenType)
                {
                    m_screens[i].CloseScreen();
                }
            }
        }

        //  Close screen of specified type - instantly, without fade-out effect
        public static void CloseScreenNow(Type screenType)
        {
            //  GUI probably not initialized yet
            if (m_screens == null) return;

            for (int i = 0; i < m_screens.Count; i++)
            {
                if (m_screens[i].GetType() == screenType)
                {
                    m_screens[i].CloseScreenNow();
                }
            }
        }

        /// <summary>
        /// Clears the old focus, this gets around an issue where the input does not always get cleared between frames, causing screens to handle input when they shouldn't.
        /// </summary>
        public static void ClearLastScreenWithFocus()
        {
            m_lastScreenWithFocus = null;
        }

        public static int GetScreensCount()
        {
            return m_screens.Count;
        }

        public static void GetControlsUnderMouseCursor(List<MyGuiControlBase> outControls, bool visibleOnly)
        {
            foreach (var screen in m_screens)
            {
                if (screen.State == MyGuiScreenState.OPENED)
                {
                    screen.GetControlsUnderMouseCursor(MyGuiManager.MouseCursorPosition, outControls, visibleOnly);
                }
            }
        }
        public static void UnloadContent()
        {
            foreach (MyGuiScreenBase screen in m_screens)
            {
                if (screen.IsFirstForUnload())
                {
                    screen.UnloadContent();
                }
            }

            foreach (MyGuiScreenBase screen in m_screens)
            {
                if (!screen.IsFirstForUnload())
                {
                    screen.UnloadContent();
                }
            }
        }


        //  Add screen to top of the screens stack, so it becomes active (will have focus)
        public static void AddScreen(MyGuiScreenBase screen)
        {
            Debug.Assert(screen != null);

            screen.Closed +=
                delegate(MyGuiScreenBase sender)
                {
                    RemoveScreen(sender);
                };

            // Hide tooltips
            var screenWithFocus = GetScreenWithFocus();
            if (screenWithFocus != null)
            {
                screenWithFocus.HideTooltips();
            }

            //  When adding new screen and previous screen is configured to hide(not close), find it and hide it now
            MyGuiScreenBase previousCanHideScreen = null;
            if (screen.CanHideOthers)
            {
                previousCanHideScreen = GetPreviousScreen(null, x => x.CanBeHidden, x => x.CanHideOthers);
            }

            if (previousCanHideScreen != null && previousCanHideScreen.State != MyGuiScreenState.CLOSING)
            {
                previousCanHideScreen.HideScreen();
            }

            MyInput.Static.JoystickAsMouse = screen.JoystickAsMouse;

            m_screensToAdd.Add(screen);
        }

        public static void AddScreenNow(MyGuiScreenBase screen)
        {
            Debug.Assert(screen != null);

            screen.Closed +=
                delegate(MyGuiScreenBase sender)
                {
                    RemoveScreen(sender);
                };

            // Hide tooltips
            var screenWithFocus = GetScreenWithFocus();
            if (screenWithFocus != null)
            {
                screenWithFocus.HideTooltips();
            }

            //  When adding new screen and previous screen is configured to hide(not close), find it and hide it now
            MyGuiScreenBase previousCanHideScreen = null;
            if (screen.CanHideOthers)
            {
                previousCanHideScreen = GetPreviousScreen(null, x => x.CanBeHidden, x => x.CanHideOthers);
            }

            if (previousCanHideScreen != null && previousCanHideScreen.State != MyGuiScreenState.CLOSING)
            {
                previousCanHideScreen.HideScreen();
            }


            if (screen.IsLoaded == false)
            {
                screen.State = MyGuiScreenState.OPENING;
                screen.LoadData();
                screen.LoadContent();
            }

            if (screen.IsAlwaysFirst())
            {
                m_screens.Insert(0, screen);
            }
            else 
            {
                m_screens.Insert(GetIndexOfLastNonTopScreen(), screen);  
            }
        }

        //  Remove screen from the stack
        public static void RemoveScreen(MyGuiScreenBase screen)
        {
            Debug.Assert(screen != null);

            if (IsAnyScreenOpening() == false)
            {
                MyGuiScreenBase previousCanHideScreen = GetPreviousScreen(screen, x => x.CanBeHidden, x => x.CanHideOthers);
                if (previousCanHideScreen != null &&
                    (previousCanHideScreen.State == MyGuiScreenState.HIDDEN || previousCanHideScreen.State == MyGuiScreenState.HIDING))
                {
                    previousCanHideScreen.UnhideScreen();
                    MyInput.Static.JoystickAsMouse = previousCanHideScreen.JoystickAsMouse;
                }
            }

            m_screensToRemove.Add(screen);
        }

        //  Find screen on top of screens, that has status HIDDEN or HIDING
        public static MyGuiScreenBase GetTopHiddenScreen()
        {
            MyGuiScreenBase hiddenScreen = null;
            for (int i = GetScreensCount() - 1; i > 0; i--)
            {
                MyGuiScreenBase screen = m_screens[i];
                if (screen.State == MyGuiScreenState.HIDDEN || screen.State == MyGuiScreenState.HIDING)
                {
                    hiddenScreen = screen;
                    break;
                }
            }
            return hiddenScreen;
        }

        //  Find previous screen to screen in screens stack
        public static MyGuiScreenBase GetPreviousScreen(MyGuiScreenBase screen, Predicate<MyGuiScreenBase> condition, Predicate<MyGuiScreenBase> terminatingCondition)
        {
            MyGuiScreenBase previousScreen = null;
            int currentScreenIndex = -1;
            if (screen == null)
                currentScreenIndex = GetScreensCount();
            for (int i = GetScreensCount() - 1; i > 0; i--)
            {
                MyGuiScreenBase tempScreen = m_screens[i];
                if (screen == tempScreen)
                {
                    currentScreenIndex = i;
                }
                if (i < currentScreenIndex)
                {
                    if (condition(tempScreen))
                    {
                        previousScreen = tempScreen;
                        break;
                    }

                    if (terminatingCondition(tempScreen))
                    {
                        break;
                    }
                }
            }
            return previousScreen;
        }

        //  Remove all screens except the one!
        public static void RemoveAllScreensExcept(MyGuiScreenBase dontRemove)
        {
            foreach (MyGuiScreenBase screen in m_screens)
            {
                if (screen != dontRemove) RemoveScreen(screen);
            }
        }

        //  Remove screens that are of type 'screenType', or those derived from 'screenType'
        //  IMPORTANT: I am not sure how will IsAssignableFrom() behave if you use class inherited from another and then another, so make sure 
        //  you understand it before you start using it and counting on this.
        public static void RemoveScreenByType(Type screenType)
        {
            foreach (MyGuiScreenBase screen in m_screens)
            {
                if (screenType.IsAssignableFrom(screen.GetType())) 
                    RemoveScreen(screen);
            }
        }

        //  Close all screens except the one!
        //  Difference against RemoveAllScreensExcept is that this one closes using CloseScreen, and RemoveAllScreensExcept just removes from the list
        public static void CloseAllScreensExcept(MyGuiScreenBase dontRemove)
        {
            for (int i = m_screens.Count - 1; i >= 0; --i)
            {
                var screen = m_screens[i];
                if ((screen != dontRemove) && (screen.CanCloseInCloseAllScreenCalls() == true)) 
                    screen.CloseScreen();
            }
        }

        //  Close all screens except the one NOW!
        //  Difference against RemoveAllScreensExcept is that this one closes using CloseScreen, and RemoveAllScreensExcept just removes from the list
        public static void CloseAllScreensNowExcept(MyGuiScreenBase dontRemove)
        {
            for (int i = m_screens.Count - 1; i >= 0; --i)
            {
                var screen = m_screens[i];
                if ((screen != dontRemove) && (screen.CanCloseInCloseAllScreenCalls() == true)) 
                    screen.CloseScreenNow();
            }

            foreach (var screen in m_screensToAdd)
            {
                screen.UnloadContent();
            }
            m_screensToAdd.Clear();
        }

        //  Close all screens except one specified and all that are marked as "topmost"
        //  Difference against RemoveAllScreensExcept is that this one closes using CloseScreen, and RemoveAllScreensExcept just removes from the list
        public static void CloseAllScreensExceptThisOneAndAllTopMost(MyGuiScreenBase dontRemove)
        {
            foreach (MyGuiScreenBase screen in m_screens)
            {
                if ((((screen == dontRemove) || (screen.IsTopMostScreen())) == false) && (screen.CanCloseInCloseAllScreenCalls() == true)) screen.CloseScreen();
            }
        }


        //  Sends input (keyboard/mouse) to screen which has focus (top-most)
        public static void HandleInput()
        {
            ProfilerShort.Begin("MyScreenManager.HandleInput");
            try
            {
                //  Forward input to screens only if there are screens and if game has focus (is active)
                if (m_screens.Count <= 0)
                    return;

                //  Get screen from top of the stack - that one has focus
                MyGuiScreenBase screenWithFocus = GetScreenWithFocus();

                if (m_inputToNonFocusedScreens)
                {
                    bool inputIsShared = false;

                    for (int i = (m_screens.Count - 1); i >= 0; i--)
                    {
                        if(m_screens.Count <= i)
                        {
                            continue;
                        }
                        MyGuiScreenBase screen = m_screens[i];
                        ProfilerShort.Begin(screen.GetType().Name);
                        if (screen.CanShareInput())
                        {
                            screen.HandleInput(m_lastScreenWithFocus != screenWithFocus);
                            inputIsShared = true;
                        }
                        else if (!inputIsShared && screen == screenWithFocus)
                            screen.HandleInput(m_lastScreenWithFocus != screenWithFocus);
                        ProfilerShort.End();
                    }

                    m_inputToNonFocusedScreens &= inputIsShared;
                }
                else
                {
                    foreach (var screen in m_screens)
                    {
                        if (screen != screenWithFocus)
                            screen.InputLost();
                    }

                    if (screenWithFocus != null)
                    {
                        switch (screenWithFocus.State)
                        {
                            case MyGuiScreenState.OPENED:
                            case MyGuiScreenState.OPENING:
                            case MyGuiScreenState.UNHIDING:
                                ProfilerShort.Begin(screenWithFocus.GetType().Name);
                                screenWithFocus.HandleInput(m_lastScreenWithFocus != screenWithFocus);
                                ProfilerShort.End();
                                break;

                            case MyGuiScreenState.CLOSING:
                            case MyGuiScreenState.HIDING:
                                break;

                            default:
                                Debug.Fail(string.Format("Focused screen in state {0}.", screenWithFocus.State));
                                break;
                        }
                    }
                }

                m_lastScreenWithFocus = screenWithFocus;
            }
            finally { ProfilerShort.End(); }
        }


        public static void HandleInputAfterSimulation()
        {
            for (int i = (m_screens.Count - 1); i >= 0; i--)
            {
                MyGuiScreenBase screen = m_screens[i];

                screen.HandleInputAfterSimulation();
            }
        }
        
        static bool IsAnyScreenInTransition()
        {
            bool isTransitioning = false;
            if (m_screens.Count > 0)
            {
                //  Get screen from top of the stack - that one has focus
                //  But it can't be closed. If yes, then look for other.
                for (int i = (m_screens.Count - 1); i >= 0; i--)
                {
                    MyGuiScreenBase screen = m_screens[i];

                    isTransitioning = IsScreenTransitioning(screen);
                    if (isTransitioning) break;
                }
            }
            return isTransitioning;
        }

        public static bool IsAnyScreenOpening()
        {
            bool isOpening = false;
            if (m_screens.Count > 0)
            {
                //  Get screen from top of the stack - that one has focus
                //  But it can't be closed. If yes, then look for other.
                for (int i = (m_screens.Count - 1); i >= 0; i--)
                {
                    MyGuiScreenBase screen = m_screens[i];

                    isOpening = screen.State == MyGuiScreenState.OPENING;
                    if (isOpening) break;
                }
            }
            return isOpening;
        }

        private static bool IsScreenTransitioning(MyGuiScreenBase screen)
        {
            return (screen.State == MyGuiScreenState.CLOSING || screen.State == MyGuiScreenState.OPENING) ||
                (screen.State == MyGuiScreenState.HIDING || screen.State == MyGuiScreenState.UNHIDING);
        }

        public static bool IsScreenOfTypeOpen(Type screenType)
        {
            foreach (MyGuiScreenBase screen in m_screens)
            {
                if ((screen.GetType() == screenType) && (screen.State == MyGuiScreenState.OPENED)) return true;
            }
            return false;
        }

        //  Update all screens
        public static void Update(int totalTimeInMS)
        {
            TotalGamePlayTimeInMilliseconds = totalTimeInMS;

            //  We remove, add, remove because sometimes in ADD when calling LoadContent some screen can be marked for remove, so we
            //  need to make sure it's really removed before we enter UPDATE or DRAW loop
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GuiManager-RemoveScreens");
            RemoveScreens();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GuiManager-AddScreens");
            AddScreens();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GuiManager-RemoveScreens2");
            RemoveScreens();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            MyGuiScreenBase screenWithFocus = GetScreenWithFocus();

            //  Update screens
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GuiManager-Update screens");

            for (int i = 0; i < m_screens.Count; i++)
            {
                MyGuiScreenBase screen = m_screens[i];

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update : " + screen.GetFriendlyName());
                screen.Update(screen == screenWithFocus);
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }


        static int GetIndexOfLastNonTopScreen()
        {
            int max = 0;
            for (int i = 0; i < m_screens.Count; i++)
            {
                MyGuiScreenBase screen = m_screens[i];
                if (screen.IsTopMostScreen() || screen.IsTopScreen())
                {
                    break;
                }
                max = i + 1;
            }
            return max;
        }
       
        //  Add screens - if during update-loop some screen was marked 'for add'
        static void AddScreens()
        {
            // Changed from foreach to for, to allow add screens during enumeration
            for (int i = 0; i < m_screensToAdd.Count; i++)
            {
                MyGuiScreenBase screenToAdd = m_screensToAdd[i];

                if (screenToAdd.IsLoaded == false)
                {
                    screenToAdd.State = MyGuiScreenState.OPENING;
                    screenToAdd.LoadData();
                    screenToAdd.LoadContent();
                }

                // I have enough of screens hidden behind gui screen gameplay
                if (screenToAdd.IsAlwaysFirst())
                    m_screens.Insert(0, screenToAdd);
                else
                    m_screens.Insert(GetIndexOfLastNonTopScreen(), screenToAdd);

                NotifyScreenAdded(screenToAdd);

            }
            m_screensToAdd.Clear();
        }

        public static bool IsScreenOnTop(MyGuiScreenBase screen)
        {
            int index = GetIndexOfLastNonTopScreen() - 1;
            if (index < 0 || index >= m_screens.Count) return false;
            if (m_screensToAdd.Count > 0) return false;

            return m_screens[index] == screen;
        }

        //  Remove screens - if during update-loop some screen was marked 'for remove'
        static void RemoveScreens()
        {
            foreach (MyGuiScreenBase screenToRemove in m_screensToRemove)
            {
                if (screenToRemove.IsLoaded == true)
                {
                    screenToRemove.UnloadContent();
                    screenToRemove.UnloadData();
                }
                screenToRemove.OnRemoved();
                m_screens.Remove(screenToRemove);

                // if we remove screen which is marked as screen to add, then we must remove it from screens to add
                int screenIndex = m_screensToAdd.Count - 1;
                while (screenIndex >= 0)
                {
                    if (m_screensToAdd[screenIndex] == screenToRemove)
                    {
                        m_screensToAdd.RemoveAt(screenIndex);
                    }
                    screenIndex--;
                }

                NotifyScreenRemoved(screenToRemove);
            }
            m_screensToRemove.Clear();
        }

       
        public static MyGuiScreenBase GetScreenWithFocus()
        {
            ProfilerShort.Begin("MyGuiManager.GetScreenWithFocus");

            MyGuiScreenBase screenWithFocus = null;

            if (m_screens.Count > 0)
            {
                //  Get screen from top of the stack - that one has focus
                //  But it can't be closed. If yes, then look for other.
                for (int i = (m_screens.Count - 1); i >= 0; i--)
                {
                    MyGuiScreenBase screen = m_screens[i];

                    bool isOpened = (screen.State == MyGuiScreenState.OPENED) || IsScreenTransitioning(screen);
                    if (isOpened && screen.CanHaveFocus)
                    {
                        screenWithFocus = screen;
                        break;
                    }
                }
            }

            ProfilerShort.End();
            return screenWithFocus;
        }

        //  Draw all screens
        public static void Draw()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyScreenManager::Draw");

            //  Find screen with focus
            MyGuiScreenBase screenWithFocus = GetScreenWithFocus();

            //  Find top screen that has background fade
            MyGuiScreenBase screenFade = null;
            bool previousCanHideOthers = false;
            for (int i = (m_screens.Count - 1); i >= 0; i--)
            {
                MyGuiScreenBase screen = m_screens[i];
                bool screenGetEnableBackgroundFade = screen.EnabledBackgroundFade;
                bool isScreenFade = false;
                if (screenWithFocus == screen || screen.GetDrawScreenEvenWithoutFocus() || !previousCanHideOthers)
                {
                    if ((screen.State != MyGuiScreenState.CLOSED) && (screenGetEnableBackgroundFade))
                    {
                        isScreenFade = true;
                    }
                }
                else if (IsScreenTransitioning(screen) && screenGetEnableBackgroundFade)
                {
                    isScreenFade = true;
                }

                if (isScreenFade)
                {
                    screenFade = screen;
                    break;
                }
                previousCanHideOthers = screen.CanHideOthers;
            }



            //  Draw all screen, from bottom to top, dragndrop last
            for (int i = 0; i < m_screens.Count; i++)
            {
                MyGuiScreenBase screen = m_screens[i];

                bool drawScreen = false;
                if (screenWithFocus == screen || screen.GetDrawScreenEvenWithoutFocus() || !previousCanHideOthers)
                {
                    if (screen.State != MyGuiScreenState.CLOSED && screen.State != MyGuiScreenState.HIDDEN)
                    {
                        drawScreen = true;
                    }
                }
                else if (!screen.CanBeHidden)
                {
                    drawScreen = true;
                }
                else if (IsScreenTransitioning(screen))
                {
                    drawScreen = true;
                }

                if (drawScreen)
                {
                    // Draw background fade before drawing first screen that has it enabled.
                    if (screen == screenFade)
                        MyGuiManager.DrawSpriteBatch(MyGuiConstants.TEXTURE_BACKGROUND_FADE, MyGuiManager.GetFullscreenRectangle(), screen.BackgroundFadeColor);

                    screen.Draw();
                }
            }

            // draw tooltips only when screen has focus
            if (screenWithFocus != null)
            {
                //  Draw tooltips
                foreach (var control in screenWithFocus.Controls.GetVisibleControls())
                {
                    control.ShowToolTip();
                }
            }


            //VRageRender.MyRenderProxy.GetRenderProfiler().ProfileCustomValue("Drawcalls", MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls);

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private static void NotifyScreenAdded(MyGuiScreenBase screen)
        {
            if (ScreenAdded != null)
            {
                ScreenAdded(screen);
            }
        }

        private static void NotifyScreenRemoved(MyGuiScreenBase screen)
        {
            if (ScreenRemoved != null)
            {
                ScreenRemoved(screen);
            }
        }

        //public static MyGuiScreenMainMenu GetMainMenuScreen()
        //{
        //    return m_screens.OfType<MyGuiScreenMainMenu>().FirstOrDefault();
        //}

        static StringBuilder m_sb = new StringBuilder(512);

        //  Only for displaying list of active GUI screens in debug console
        public static StringBuilder GetGuiScreensForDebug()
        {
            m_sb.Clear();
            m_sb.ConcatFormat("{0}{1}{2}", "GUI screens: [", m_screens.Count, "]: ");
            var screenWithFocus = GetScreenWithFocus();
            for (int i = 0; i < m_screens.Count; i++)
            {
                MyGuiScreenBase screen = m_screens[i];
                if (screenWithFocus == screen)
                    m_sb.Append("[F]");
                m_sb.Append(screen.GetFriendlyName());
                //m_sb.Replace("MyGuiScreen", ""); //This is doing allocations
                m_sb.Append(i < (m_screens.Count - 1) ? ", " : "");
                //                string[] stateString = { "o", "O", "c", "C", "h", "u", "H" };  // debug: show opening/closing state of screens
                //                sb.Append(screen.GetFriendlyName().Replace("MyGuiScreen", "") + "(" + stateString[(int)(screen.GetState())] + ")" + (i < (m_screens.Count - 1) ? ", " : ""));
            }
            return m_sb;
        }

        public static T GetFirstScreenOfType<T>() where T: MyGuiScreenBase
        {
            return m_screens.OfType<T>().FirstOrDefault();
        }

    }
}
