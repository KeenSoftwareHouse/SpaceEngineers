using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using System;
using System.Text;
using VRage.Game;
using VRage.Utils;

//using SharpDX.Direct3D9;

//  This screen is special because it is drawn during we load some other screen - LoadContent - in another thread.
//  Player sees this "...Loading..." screen.

namespace Sandbox.Game.Gui
{
    using Sandbox.Common;
    using Sandbox.Game.Localization;
    using Sandbox.Game.Screens;
    using Sandbox.Game.Screens.Helpers;
    using Sandbox.Game.World;
    using Sandbox.Graphics;
    using System.Diagnostics;
    using VRage;
    using VRage.Audio;
    using VRage.Input;
    using Color = VRageMath.Color;
    using Rectangle = VRageMath.Rectangle;
    using Vector2 = VRageMath.Vector2;
    using Vector4 = VRageMath.Vector4;
    
    public class MyGuiScreenLoading : MyGuiScreenBase
    {
        //We have to ensure there is always only one loading screen instance
        public static MyGuiScreenLoading Static;

        MyGuiScreenGamePlay m_screenToLoad;
        readonly MyGuiScreenGamePlay m_screenToUnload;

        string m_backgroundScreenTexture;
        string m_backgroundTextureFromConstructor;
        string m_customTextFromConstructor;
        string m_rotatingWheelTexture;
        string m_gameLogoTexture;
        private MyLoadingScreenQuote m_currentQuote;
        private MyGuiControlMultilineText m_quoteTextControl;
        private StringBuilder m_authorWithDash;

        private MyGuiControlRotatingWheel m_wheel;
        private bool m_exceptionDuringLoad;

        public static string LastBackgroundTexture;

        /// <summary>
        /// Event created once the screen has been loaded and added to gui manager.
        /// </summary>
        public event Action OnScreenLoadingFinished;

        public static int m_currentQuoteIdx = 0;

        volatile bool m_loadInDrawFinished;

        bool m_loadFinished;

        private string m_font = MyFontEnum.LoadingScreen;

        public MyGuiScreenLoading(MyGuiScreenGamePlay screenToLoad, MyGuiScreenGamePlay screenToUnload, string textureFromConstructor, string customText = null)
            : base(Vector2.Zero, null, null)
        {
            MyLoadingPerformance.Instance.StartTiming();

            System.Diagnostics.Debug.Assert(Static == null);
            Static = this;

            m_screenToLoad = screenToLoad;
            m_screenToUnload = screenToUnload;
            m_closeOnEsc = false;
            DrawMouseCursor = false;
            m_loadInDrawFinished = false;
            m_drawEvenWithoutFocus = true;
            m_currentQuote = MyLoadingScreenQuote.GetRandomQuote();
            m_isFirstForUnload = true;

            // Has to be done because of HW Cursor
            MyGuiSandbox.SetMouseCursorVisibility(false);

            m_rotatingWheelTexture = MyGuiConstants.LOADING_TEXTURE_LOADING_SCREEN;
            m_backgroundTextureFromConstructor = textureFromConstructor;
            m_customTextFromConstructor = customText;

            m_loadFinished = false;

            //MyAudio.Static.Mute = true;

            if (m_screenToLoad != null)
            {
                MySandboxGame.IsUpdateReady = false;
                MySandboxGame.AreClipmapsReady = MySandboxGame.IsDedicated;
            }

            m_authorWithDash = new StringBuilder();

            RecreateControls(true);
        }

        public MyGuiScreenLoading(MyGuiScreenGamePlay screenToLoad, MyGuiScreenGamePlay screenToUnload)
            : this(screenToLoad, screenToUnload, null)
        {
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            Vector2 loadingTextSize = MyGuiManager.MeasureString(m_font,
                MyTexts.Get(MyCommonTexts.LoadingPleaseWaitUppercase), MyGuiConstants.LOADING_PLEASE_WAIT_SCALE);
            m_wheel = new MyGuiControlRotatingWheel(
                MyGuiConstants.LOADING_PLEASE_WAIT_POSITION - new Vector2(0, 0.06f + loadingTextSize.Y),
                MyGuiConstants.ROTATING_WHEEL_COLOR,
                MyGuiConstants.ROTATING_WHEEL_DEFAULT_SCALE,
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                m_rotatingWheelTexture,
                false,
                MyPerGameSettings.GUI.MultipleSpinningWheels);

            StringBuilder contents;

            if(!string.IsNullOrEmpty(m_customTextFromConstructor))
                contents = new StringBuilder(m_customTextFromConstructor);
            else
                contents = MyTexts.Get(m_currentQuote.Text);

            m_quoteTextControl = new MyGuiControlMultilineText(
                position: Vector2.One * 0.5f,
                size: new Vector2(0.9f, 0.2f),
                backgroundColor: Vector4.One,
                font: m_font,
                textScale: 1.0f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
                contents: contents,
                drawScrollbar: false);
            m_quoteTextControl.BorderEnabled = false;
            m_quoteTextControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM;
            m_quoteTextControl.TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM;

            Controls.Add(m_wheel);
            RefreshQuote();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenLoading";
        }

        public override void LoadContent()
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenLoading.LoadContent - START");
            MySandboxGame.Log.IncreaseIndent();
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGuiScreenLoading::LoadContent");

            m_backgroundScreenTexture = m_backgroundTextureFromConstructor ?? GetRandomBackgroundTexture();
            m_gameLogoTexture = "Textures\\GUI\\GameLogoLarge.dds";

            if (m_screenToUnload != null)
            {
                //  If there is existing screen we are trying to replace (e.g. gameplay screen), we will mark this one as unloaded, so
                //  then remove screen method won't try to unload it and we can do it in our thread. This is also the reason why we do it
                //  here, becasue changing IsLoaded in paralel thread can't tell us when it happens - and this must be serial.
                m_screenToUnload.IsLoaded = false;
                m_screenToUnload.CloseScreenNow();
            }

            //  Base load content must be called after child's load content
            base.LoadContent();

            VRageRender.MyRenderProxy.LimitMaxQueueSize = true;

            if (m_screenToLoad != null && !m_loadInDrawFinished && m_loadFinished)
            {
                m_screenToLoad.State = MyGuiScreenState.OPENING;
                m_screenToLoad.LoadContent();

                //MyGuiInput.SetMouseToScreenCenter();
            }
            else
                m_loadFinished = false;

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyGuiScreenLoading.LoadContent - END");
        }

        static string GetRandomBackgroundTexture()
        {
            int randomNumber = MyUtils.GetRandomInt(MyPerGameSettings.GUI.LoadingScreenIndexRange.X, MyPerGameSettings.GUI.LoadingScreenIndexRange.Y + 1);
            string paddedNumber = randomNumber.ToString().PadLeft(3, '0');
            return "Textures\\GUI\\Screens\\loading_background_" + paddedNumber + ".dds";
        }

        public override void UnloadContent()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGuiScreenLoading::UnloadContent");
            //  This is just for case that whole application is quiting after Alt+F4 or something
            //  Don't try to unload content in that thread or something - we don't know in what state it is. Just abort it.

            if (m_backgroundScreenTexture != null) VRageRender.MyRenderProxy.UnloadTexture(m_backgroundScreenTexture);
            if (m_backgroundTextureFromConstructor != null) VRageRender.MyRenderProxy.UnloadTexture(m_backgroundTextureFromConstructor);
            if (m_backgroundScreenTexture != null) VRageRender.MyRenderProxy.UnloadTexture(m_rotatingWheelTexture);

            if (m_screenToLoad != null && !m_loadFinished && m_loadInDrawFinished)
            {
                //  Call unload because there might be running precalc threads and we need to stop them
                //m_screenToLoad.UnloadObjects();
                m_screenToLoad.UnloadContent();
                m_screenToLoad.UnloadData();

                //m_screenToLoad.UnloadData();
                m_screenToLoad = null;
            }


            if (m_screenToLoad != null && !m_loadInDrawFinished)
            {
                m_screenToLoad.UnloadContent();
            }

            VRageRender.MyRenderProxy.LimitMaxQueueSize = false;

            base.UnloadContent();

            System.Diagnostics.Debug.Assert(Static == this);
            Static = null;

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGuiScreenLoading::Update()");
            if (this.State == MyGuiScreenState.OPENED)
            {
                if (!m_loadFinished)
                {
                    MyHud.ScreenEffects.FadeScreen(0f, 0f);
                    MyAudio.Static.Mute = true;
                    MyAudio.Static.StopMusic();
                    MyAudio.Static.ChangeGlobalVolume(0, 0);
                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("LoadInBackgroundThread");

                    DrawLoading();

                    if (m_screenToLoad != null)
                    {
                        MySandboxGame.Log.WriteLine("RunLoadingAction - START");
                        RunLoad();
                        MySandboxGame.Log.WriteLine("RunLoadingAction - END");
                    }
                    if (m_screenToLoad != null)
                    {
                        //  Screen is loaded so now we can add it to other thread
                        MyScreenManager.AddScreenNow(m_screenToLoad);
                        m_screenToLoad.Update(false);
                    }

                    m_screenToLoad = null;

                    m_loadFinished = true;
                    m_wheel.ManualRotationUpdate = true;

                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                }
            }

            if (m_loadFinished && MySandboxGame.IsGameReady)
            {
                MyHud.ScreenEffects.FadeScreen(1f, 5f);
                if (!m_exceptionDuringLoad && OnScreenLoadingFinished != null)
                {
                    OnScreenLoadingFinished();
                    OnScreenLoadingFinished = null;
                }
                CloseScreenNow();
                DrawLoading();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            return true;
        }

        private void RunLoad()
        {
            m_exceptionDuringLoad = false;
            if (MyFakes.THROW_LOADING_ERRORS)
            {
                m_screenToLoad.RunLoadingAction();
            }
            else
            {
                try
                {
                    m_screenToLoad.RunLoadingAction();
                }
                catch (MyLoadingException e)
                {
                    OnLoadException(e, new StringBuilder(e.Message), 1.5f);
                    m_exceptionDuringLoad = true;
                }
                catch (Exception e)
                {
                    OnLoadException(e, MyTexts.Get(MyCommonTexts.WorldFileIsCorruptedAndCouldNotBeLoaded));
                    m_exceptionDuringLoad = true;
                    Debug.Fail("Exception raised during Session loading: " + e.ToString());
                }
            }
        }

        private void OnLoadException(Exception e, StringBuilder errorText, float heightMultiplier = 1.0f)
        {
            MySandboxGame.Log.WriteLine("ERROR: Loading screen failed");
            MySandboxGame.Log.WriteLine(e);
            m_screenToLoad = null;

            // GamePlayScreen might not have been unloaded, so check this here
            if (MyGuiScreenGamePlay.Static != null)
            {
                MyGuiScreenGamePlay.Static.UnloadData();
                MyGuiScreenGamePlay.Static.UnloadContent();
            }
            // Reset this to true so we have sounds
            MySandboxGame.IsUpdateReady = true;
            MySandboxGame.AreClipmapsReady = true;

            try
            {
                MySessionLoader.UnloadAndExitToMenu();
            }
            catch (Exception ex)
            {
                MySession.Static = null;
                MySandboxGame.Log.WriteLine("ERROR: failed unload after exception in loading !");
                MySandboxGame.Log.WriteLine(ex);
            }

            var errorScreen = MyGuiSandbox.CreateMessageBox(
                messageText: errorText,
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));

            var size = errorScreen.Size.Value;
            size.Y *= heightMultiplier;
            errorScreen.Size = size;
            errorScreen.RecreateControls(false);

            MyGuiSandbox.AddScreen(errorScreen);
        }
        
        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.ENABLE_DEVELOPER_KEYS)
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Add))
                {
                    m_currentQuote = MyLoadingScreenQuote.GetQuote(++m_currentQuoteIdx);
                    RefreshQuote();
                }
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Subtract))
                {
                    m_currentQuote = MyLoadingScreenQuote.GetQuote(--m_currentQuoteIdx);
                    RefreshQuote();
                }
            }
        }

        static long lastEnvWorkingSet = 0;
        static long lastGc = 0;
        static long lastVid = 0;

        private bool DrawLoading()
        {
            VRageRender.MyRenderProxy.AfterUpdate(null);
            VRageRender.MyRenderProxy.BeforeUpdate();

            DrawInternal();
            bool res = base.Draw();

            VRageRender.MyRenderProxy.AfterUpdate(null);
            VRageRender.MyRenderProxy.BeforeUpdate();

            return res;
        }

        private void DrawInternal()
        {
            Color colorQuote = new Color(255, 255, 255, 250);     //  White
            colorQuote.A = (byte)(colorQuote.A * m_transitionAlpha);
            {
                //////////////////////////////////////////////////////////////////////
                //  Normal loading screen
                //////////////////////////////////////////////////////////////////////

                //  Random background texture
                Rectangle backgroundRectangle;
                MyGuiManager.GetSafeHeightFullScreenPictureSize(MyGuiConstants.LOADING_BACKGROUND_TEXTURE_REAL_SIZE, out backgroundRectangle);
                MyGuiManager.DrawSpriteBatch(m_backgroundScreenTexture, backgroundRectangle, new Color(new Vector4(1f, 1f, 1f, m_transitionAlpha)));
                MyGuiManager.DrawSpriteBatch(MyGuiConstants.TEXTURE_BACKGROUND_FADE, backgroundRectangle, new Color(new Vector4(1f, 1f, 1f, m_transitionAlpha)));

                //  Game logo
                MyGuiSandbox.DrawGameLogo(m_transitionAlpha);
            }

            LastBackgroundTexture = m_backgroundScreenTexture;

            //  Loading Please Wait
            MyGuiManager.DrawString(m_font, MyTexts.Get(MyCommonTexts.LoadingPleaseWaitUppercase),
                MyGuiConstants.LOADING_PLEASE_WAIT_POSITION, MyGuiSandbox.GetDefaultTextScaleWithLanguage() * MyGuiConstants.LOADING_PLEASE_WAIT_SCALE, new Color(MyGuiConstants.LOADING_PLEASE_WAIT_COLOR * m_transitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);

            // Draw quote
            {
                if (string.IsNullOrEmpty(m_customTextFromConstructor))
                {
                    var font = m_font;
                    var controlBottomLeft = m_quoteTextControl.GetPositionAbsoluteBottomLeft();
                    var textSize = m_quoteTextControl.TextSize;
                    var controlSize = m_quoteTextControl.Size;
                    var authorTopLeft = controlBottomLeft +
                                        new Vector2((controlSize.X - textSize.X)*0.5f + 0.025f, 0.025f);
                    MyGuiManager.DrawString(font, m_authorWithDash, authorTopLeft,
                        MyGuiSandbox.GetDefaultTextScaleWithLanguage());
                }
                m_quoteTextControl.Draw(1, 1);
            }
        }

        public override bool Draw()
        {
            DrawInternal();
            return base.Draw();
        }

        private void RefreshQuote()
        {
            if(string.IsNullOrEmpty(m_customTextFromConstructor))
            {
                m_quoteTextControl.TextEnum = m_currentQuote.Text;
                m_authorWithDash.Clear().Append("- ").AppendStringBuilder(MyTexts.Get(m_currentQuote.Author)).Append(" -");
                
            }
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
        }
    }
}
