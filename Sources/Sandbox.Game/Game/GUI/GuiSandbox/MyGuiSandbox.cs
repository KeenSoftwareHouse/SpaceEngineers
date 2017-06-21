#region Using

using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Plugins;
using VRage.Profiler;
using VRage.Utils;
using Vector2 = VRageMath.Vector2;

#endregion

namespace Sandbox.Graphics.GUI
{
    public static class MyGuiSandbox
    {
        internal static IMyGuiSandbox Gui = new MyNullGui();

        private static Dictionary<Type, Type> m_createdScreenTypes = new Dictionary<Type, Type>();

        public static int TotalGamePlayTimeInMilliseconds;

        static public void SetMouseCursorVisibility(bool visible, bool changePosition = true)
        {
            Gui.SetMouseCursorVisibility(visible, changePosition);
        }

        public static Vector2 MouseCursorPosition
        {
            get { return Gui.MouseCursorPosition; }
        }

        /// <summary>
        /// Loads the data.
        /// </summary>
        public static void LoadData(bool nullGui)
        {
            ProfilerShort.Begin("Create MyDX9Gui");
            if (!nullGui)
                Gui = new MyDX9Gui();
            ProfilerShort.End();

            ProfilerShort.Begin("Gui.LoadData");
            Gui.LoadData();
            ProfilerShort.End();
        }

        public static void LoadContent(MyFontDescription[] fonts)
        {
            Gui.LoadContent(fonts);
        }

        /// <summary>
        /// Event triggered on gui control created.
        /// </summary>
        public static Action<object> GuiControlCreated;

        /// <summary>
        /// Event triggered on gui control removed.
        /// </summary>
        public static Action<object> GuiControlRemoved;

#if XB1
        //TODO for XB1
#else // !XB1
        //when changing sites, change WwwLinkNotAllowed accordingly. Also, when using whitelists, consider using WwwLinkNotAllowed to inform user that link is not available
        private static Regex[] WWW_WHITELIST = {   new Regex(@"^(http[s]{0,1}://){0,1}[^/]*youtube.com/.*", RegexOptions.IgnoreCase),
                                              new Regex(@"^(http[s]{0,1}://){0,1}[^/]*youtu.be/.*", RegexOptions.IgnoreCase),
                                              new Regex(@"^(http[s]{0,1}://){0,1}[^/]*steamcommunity.com/.*", RegexOptions.IgnoreCase),
                                              new Regex(@"^(http[s]{0,1}://){0,1}[^/]*forum[s]{0,1}.keenswh.com/.*", RegexOptions.IgnoreCase),
                                          };
#endif // !XB1

        public static bool IsUrlWhitelisted(string wwwLink)
        {
#if XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
            foreach (var r in WWW_WHITELIST)
                if (r.IsMatch(wwwLink))
                    return true;
#endif // !XB1
            return false;
        }

        /// <summary>
        /// Opens URL in Steam overlay or external browser.
        /// </summary>
        /// <param name="url">Url to open.</param>
        /// <param name="urlFriendlyName">Friendly name of URL to show in confirmation screen, e.g. Steam Workshop</param>
        public static void OpenUrlWithFallback(string url, string urlFriendlyName, bool useWhitelist=false)
        {
            if (useWhitelist && !IsUrlWhitelisted(url))
            {
                MySandboxGame.Log.WriteLine("URL NOT ALLOWED: " + url);//gameplay may not be running yet, so no message box :-(
                return;
            }
            var confirmMessage = MyTexts.AppendFormat(new StringBuilder(), MyCommonTexts.MessageBoxTextOpenUrlOverlayNotEnabled, urlFriendlyName);
            OpenUrl(url, UrlOpenMode.SteamOrExternalWithConfirm, confirmMessage);
        }

        /// <summary>
        /// Opens URL in Steam overlay or external browser.
        /// </summary>
        /// <param name="url">Url to open.</param>
        /// <param name="openMode">How to open the url.</param>
        public static void OpenUrl(string url, UrlOpenMode openMode, StringBuilder confirmMessage = null)
        {
            bool tryOverlay = (openMode & UrlOpenMode.SteamOverlay) != 0;
            bool tryExternal = (openMode & UrlOpenMode.ExternalBrowser) != 0;
            bool confirm = (openMode & UrlOpenMode.ConfirmExternal) != 0;

            bool steamOverlayShown = tryOverlay && Gui.OpenSteamOverlay(url);

            if (MyFakes.XBOX_PREVIEW)
            {
                MyGuiSandbox.Show(MyCommonTexts.MessageBoxTextErrorFeatureNotAvailableYet, MyCommonTexts.MessageBoxCaptionError);
            }
            else
            {
                if (!steamOverlayShown && tryExternal)
                {
                    if (confirm)
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.YES_NO,
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                            messageText: confirmMessage ?? MyTexts.AppendFormat(new StringBuilder(), MyCommonTexts.MessageBoxTextOpenBrowser, url),
                            callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                            {
                                if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                                {
                                    OpenExternalBrowser(url);
                                }
                            }));
                    }
                    else
                    {
                        OpenExternalBrowser(url);
                    }
                }
            }
        }

        public static void OpenExternalBrowser(string url)
        {
            if (!MyBrowserHelper.OpenInternetBrowser(url))
            {
                StringBuilder text = MyTexts.Get(MyCommonTexts.TitleFailedToStartInternetBrowser);
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: text));
            }
        }

        public static void UnloadContent()
        {
            Gui.UnloadContent();
        }

        public static void SwitchDebugScreensEnabled()
        {
            Gui.SwitchDebugScreensEnabled();
        }

        public static void ShowModErrors()
        {
            Gui.ShowModErrors();
        }

        public static bool IsDebugScreenEnabled()
        {
            return Gui.IsDebugScreenEnabled();
        }

        public static void HandleRenderProfilerInput()
        {
            Gui.HandleRenderProfilerInput();
        }

        public static MyGuiScreenBase CreateScreen(Type screenType, params object[] args)
        {
            return Activator.CreateInstance(screenType, args) as MyGuiScreenBase;
        }

        public static T CreateScreen<T>(params object[] args) where T : MyGuiScreenBase
        {
            Type createdType = null;
            if (!m_createdScreenTypes.TryGetValue(typeof(T), out createdType))
            {
                var resultType = typeof(T);
                createdType = resultType;
#if XB1 // XB1_ALLINONEASSEMBLY
                ChooseScreenType<T>(ref createdType, MyAssembly.AllInOneAssembly);
#else // !XB1
                ChooseScreenType<T>(ref createdType, MyPlugins.GameAssembly);
                ChooseScreenType<T>(ref createdType, MyPlugins.SandboxAssembly);
                ChooseScreenType<T>(ref createdType, MyPlugins.UserAssembly);
#endif // !XB1
                m_createdScreenTypes[resultType] = createdType;
            }

            return Activator.CreateInstance(createdType, args) as T;
        }

        private static void ChooseScreenType<T>(ref Type createdType, Assembly assembly) where T : MyGuiScreenBase
        {
            if (assembly == null)
                return;

#if XB1 // XB1_ALLINONEASSEMBLY
            foreach (var type in MyAssembly.GetTypes())
#else // !XB1
            foreach (var type in assembly.GetTypes())
#endif // !XB1
            {
                if (typeof(T).IsAssignableFrom(type))
                {
                    createdType = type;
                    break;
                }
            }
        }

        public static void AddScreen(MyGuiScreenBase screen)
        {
            Gui.AddScreen(screen);
            if (GuiControlCreated != null)
                GuiControlCreated(screen);
            if ( MyAPIGateway.GuiControlCreated != null )
                MyAPIGateway.GuiControlCreated( screen );
        }

        public static void RemoveScreen(MyGuiScreenBase screen)
        {
            Gui.RemoveScreen(screen);
            if ( GuiControlRemoved != null )
                GuiControlRemoved( screen );
        }

        //  Sends input (keyboard/mouse) to screen which has focus (top-most)
        public static void HandleInput()
        {
            Gui.HandleInput();
        }

        //  Sends input (keyboard/mouse) to screen which has focus (top-most)
        public static void HandleInputAfterSimulation()
        {
            Gui.HandleInputAfterSimulation();
        }

        //  Update all screens
        public static void Update(int totalTimeInMS)
        {
            Gui.Update(totalTimeInMS);
        }

        //  Draw all screens
        public static void Draw()
        {
            Gui.Draw();
        }

        public static void BackToIntroLogos(Action afterLogosAction)
        {
            Gui.BackToIntroLogos(afterLogosAction);
        }

        public static void BackToMainMenu()
        {
            Gui.BackToMainMenu();
        }

        public static float GetDefaultTextScaleWithLanguage()
        {
            return Gui.GetDefaultTextScaleWithLanguage();
        }

        public static void TakeScreenshot(int width, int height, string saveToPath = null, bool ignoreSprites = false, bool showNotification = true)
        {
            Gui.TakeScreenshot(width, height, saveToPath, ignoreSprites, showNotification);
        }

        public static MyGuiScreenMessageBox CreateMessageBox(
            MyMessageBoxStyleEnum styleEnum = MyMessageBoxStyleEnum.Error,
            MyMessageBoxButtonsType buttonType = MyMessageBoxButtonsType.OK,
            StringBuilder messageText = null,
            StringBuilder messageCaption = null,
            MyStringId? okButtonText = null,
            MyStringId? cancelButtonText = null,
            MyStringId? yesButtonText = null,
            MyStringId? noButtonText = null,
            Action<MyGuiScreenMessageBox.ResultEnum> callback = null,
            int timeoutInMiliseconds = 0,
            MyGuiScreenMessageBox.ResultEnum focusedResult = MyGuiScreenMessageBox.ResultEnum.YES,
            bool canHideOthers = true,
            Vector2? size = null
            )
        {
            return new MyGuiScreenMessageBox(
                styleEnum, buttonType, messageText, messageCaption,
                okButtonText ?? MyCommonTexts.Ok,
                cancelButtonText ?? MyCommonTexts.Cancel,
                yesButtonText ?? MyCommonTexts.Yes,
                noButtonText ?? MyCommonTexts.No,
                callback, timeoutInMiliseconds, focusedResult, canHideOthers, size);
        }

        public static void Show(StringBuilder text, MyStringId caption = default(MyStringId), MyMessageBoxStyleEnum type = MyMessageBoxStyleEnum.Error)
        {
            AddScreen(
                CreateMessageBox(
                    styleEnum: type,
                    messageText: text,
                    messageCaption: MyTexts.Get(caption)));
        }

        public static void Show(
            MyStringId text,
            MyStringId caption = default(MyStringId),
            MyMessageBoxStyleEnum type = MyMessageBoxStyleEnum.Error)
        {
            AddScreen(
                CreateMessageBox(
                    styleEnum: type,
                    messageText: MyTexts.Get(text),
                    messageCaption: MyTexts.Get(caption)));
        }

        public static void DrawGameLogo(float transitionAlpha)
        {
            Gui.DrawGameLogo(transitionAlpha);
        }

        public static string GetKeyName(MyStringId control)
        {
            var controls = MyInput.Static.GetGameControl(control);
            if (controls != null)
                return controls.GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
            else return "";
        }
    }
}