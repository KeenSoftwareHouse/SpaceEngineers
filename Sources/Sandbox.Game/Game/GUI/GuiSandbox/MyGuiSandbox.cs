﻿#region Using

using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Plugins;
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
            if (!nullGui)
                Gui = new MyDX9Gui();

            Gui.LoadData();
        }

        public static void LoadContent(MyFontDescription[] fonts)
        {
            Gui.LoadContent(fonts);
        }


        //when changing sites, change WwwLinkNotAllowed accordingly. Also, when using whitelists, consider using WwwLinkNotAllowed to inform user that link is not available
        private static Regex[] WWW_WHITELIST = {   new Regex(@"^(http[s]{0,1}://){0,1}[^/]*youtube.com/.*", RegexOptions.IgnoreCase),
                                              new Regex(@"^(http[s]{0,1}://){0,1}[^/]*youtu.be/.*", RegexOptions.IgnoreCase),
                                              new Regex(@"^(http[s]{0,1}://){0,1}[^/]*steamcommunity.com/.*", RegexOptions.IgnoreCase),
                                              new Regex(@"^(http[s]{0,1}://){0,1}[^/]*forum[s]{0,1}.keenswh.com/.*", RegexOptions.IgnoreCase),
                                          };

        public static bool IsUrlWhitelisted(string wwwLink)
        {
            foreach (var r in WWW_WHITELIST)
                if (r.IsMatch(wwwLink))
                    return true;
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
            var confirmMessage = MyTexts.AppendFormat(new StringBuilder(), MySpaceTexts.MessageBoxTextOpenUrlOverlayNotEnabled, urlFriendlyName);
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
                MyGuiSandbox.Show(MySpaceTexts.MessageBoxTextErrorFeatureNotAvailableYet, MySpaceTexts.MessageBoxCaptionError);
            }
            else
            {
                if (!steamOverlayShown && tryExternal)
                {
                    if (confirm)
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.YES_NO,
                            messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                            messageText: confirmMessage ?? MyTexts.AppendFormat(new StringBuilder(), MySpaceTexts.MessageBoxTextOpenBrowser, url),
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
                StringBuilder text = MyTexts.Get(MySpaceTexts.TitleFailedToStartInternetBrowser);
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
                ChooseScreenType<T>(ref createdType, MyPlugins.GameAssembly);
                ChooseScreenType<T>(ref createdType, MyPlugins.SandboxAssembly);
                ChooseScreenType<T>(ref createdType, MyPlugins.UserAssembly);
                m_createdScreenTypes[resultType] = createdType;
            }

            return Activator.CreateInstance(createdType, args) as T;
        }

        private static void ChooseScreenType<T>(ref Type createdType, Assembly assembly) where T : MyGuiScreenBase
        {
            if (assembly == null)
                return;

            foreach (var type in assembly.GetTypes())
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
        }

        public static void RemoveScreen(MyGuiScreenBase screen)
        {
            Gui.RemoveScreen(screen);
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
                okButtonText ?? MySpaceTexts.Ok,
                cancelButtonText ?? MySpaceTexts.Cancel,
                yesButtonText ?? MySpaceTexts.Yes,
                noButtonText ?? MySpaceTexts.No,
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
            return MyInput.Static.GetGameControl(control).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
        }
    }
}