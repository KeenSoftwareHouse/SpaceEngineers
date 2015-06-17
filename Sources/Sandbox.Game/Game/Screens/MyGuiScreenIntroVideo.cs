using Sandbox.Engine.Utils;
using Sandbox.Graphics;
//using DShowNET;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VRage;
using VRage.FileSystem;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRageRender;

//  IMPORTANT: It seems that even if my computer (Marek's) can handle 1280x720 in high bit-rate, a lot
//  of people has problem with it, even if they have high-end computer. Looks to be XNA/DirectX/Windows
//  problem. But by testing I have found that 

namespace Sandbox.Game.Gui
{
    class MyGuiScreenIntroVideo : MyGuiScreenBase
    {
        struct Subtitle
        {
            public Subtitle(int startMs, int lengthMs, MyStringId textEnum)
            {
                this.StartTime = TimeSpan.FromMilliseconds(startMs);
                this.Length = TimeSpan.FromMilliseconds(lengthMs);
                this.Text = MyTexts.Get(textEnum);
            }

            public TimeSpan StartTime;
            public TimeSpan Length;
            public StringBuilder Text;
        }

        //VideoPlayer m_videoPlayer;
        uint m_videoID = 0xFFFFFFFF;
        bool m_playbackStarted;

        string[] m_videos;
        string m_currentVideo = "";
        List<Subtitle> m_subtitles = new List<Subtitle>();
        int m_currentSubtitleIndex = 0;

        Vector4 m_colorMultiplier = Vector4.One;

        private static readonly string m_videoOverlay = "Textures\\GUI\\Screens\\main_menu_overlay.dds";
        public static bool VideoOverlayEnabled = true;

        private bool m_loop = true;

        public MyGuiScreenIntroVideo(string[] videos)
            : base(Vector2.Zero, null, null)
        {
            DrawMouseCursor = false;
            CanHaveFocus = false;
            m_closeOnEsc = false;
            m_drawEvenWithoutFocus = true;
            m_videos = videos;
        }

        public static MyGuiScreenIntroVideo CreateBackgroundScreen()
        {
            var result = new MyGuiScreenIntroVideo(MyPerGameSettings.GUI.MainMenuBackgroundVideos);
            result.m_colorMultiplier = new Vector4(0.5f, 0.5f, 0.5f, 1);
            return result;
        }

        private static void AddCloseEvent(Action onVideoFinished, MyGuiScreenIntroVideo result)
        {
            result.Closed += (screen) => onVideoFinished();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenIntroVideo";
        }

        private void LoadRandomVideo()
        {
            int index = MyUtils.GetRandomInt(0, m_videos.Length);
            if (m_videos.Length > 0)
            {
                m_currentVideo = m_videos[index];
            }
        }

        public override void LoadContent()
        {
            m_playbackStarted = false;

            LoadRandomVideo();

            //  Base load content must be called after child's load content
            base.LoadContent();
        }

        public override void UnloadContent()
        {
            //  Stop playback
            VRageRender.MyRenderProxy.CloseVideo(m_videoID);
            m_videoID = 0xFFFFFFFF;
                /*
            if (m_videoPlayer != null)
            {
                m_videoPlayer.Stop();
                m_videoPlayer.Dispose();
                m_videoPlayer = null;
            } */

            m_currentVideo = "";

            base.UnloadContent();

            //This one causes leaks in D3D
            //GC.Collect();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if ((MyInput.Static.IsAnyKeyPress() == true) || (MyInput.Static.IsAnyMousePressed() == true))
            {
                CloseScreen();
            }
        }

        void Loop()
        {
            // loop video
            m_currentSubtitleIndex = 0;
            LoadRandomVideo();
            TryPlayVideo();
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            /*
            // when playback started and player is null, video can't be played, so close screen
            if (m_playbackStarted && !VRageRender.MyRenderProxy.IsVideoValid(m_videoID))
            {
                CloseScreen();
            } */

            if (m_playbackStarted == false)
            {
                TryPlayVideo();

                m_playbackStarted = true;
            }
            else
            {
                if ((VRageRender.MyRenderProxy.IsVideoValid(m_videoID)) && (VRageRender.MyRenderProxy.GetVideoState(m_videoID) != VideoState.Playing))
                {
                    if (m_loop)
                    {
                        Loop();
                    }
                    else
                    {
                        CloseScreen();
                    }
                }
                  
                

                if (State == MyGuiScreenState.CLOSING)
                {
                    //  Update volume, so during exiting it's fading out as alpha
                    if (VRageRender.MyRenderProxy.IsVideoValid(m_videoID))
                    {
                        VRageRender.MyRenderProxy.SetVideoVolume(m_videoID, m_transitionAlpha);
                    }
                } 
            }

            return true;
        }

        void TryPlayVideo()
        {
            if (!MyFakes.ENABLE_VIDEO_PLAYER)
                return;

            if (m_videoID != 0xFFFFFFFF)
                VRageRender.MyRenderProxy.CloseVideo(m_videoID);

            var fsPath = Path.Combine(MyFileSystem.ContentPath, m_currentVideo);

            if (File.Exists(fsPath))
                m_videoID = VRageRender.MyRenderProxy.PlayVideo(fsPath, 0f);
        }

        public override bool CloseScreen()
        {
            bool ret = base.CloseScreen();

            if (ret)
            {
                VRageRender.MyRenderProxy.CloseVideo(m_videoID);
                /*
                if (m_videoPlayer != null)
                {
                    m_videoPlayer.Stop();
                    m_videoPlayer.Dispose();
                    m_videoPlayer = null;
                } */
            }

            return ret;
        }

        public override bool Draw()
        {
            if(!base.Draw()) return false;

            if (VRageRender.MyRenderProxy.IsVideoValid(m_videoID))
            {
                VRageRender.MyRenderProxy.UpdateVideo(m_videoID);
                Vector4 color = m_colorMultiplier * m_transitionAlpha;
                VRageRender.MyRenderProxy.DrawVideo(m_videoID, MyGuiManager.GetSafeFullscreenRectangle(), new Color(color), VRageRender.MyVideoRectangleFitMode.AutoFit);
            }

            if (VideoOverlayEnabled)
                DrawVideoOverlay();

            return true;
        }

        public static void DrawVideoOverlay()
        {
            //)  new Rectangle(0, 0, MySandboxGame.ScreenSize.X, MySandboxGame.ScreenSize.Y)
            MyGuiManager.DrawSpriteBatch(m_videoOverlay, MyGuiManager.GetSafeFullscreenRectangle(), Color.White);
        }
    }
}
