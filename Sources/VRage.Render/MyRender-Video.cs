using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Import;
using SharpDX.Direct3D9;
using VRageRender.Effects;
using VRageRender.Graphics;
using System.Diagnostics;
using SharpDX;
using VRageRender.Textures;
using VRageRender.Lights;

using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using Rectangle = VRageMath.Rectangle;
using Matrix = VRageMath.Matrix;
using Color = VRageMath.Color;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingFrustum = VRageMath.BoundingFrustum;

using DShowNET;

namespace VRageRender
{
    internal static partial class MyRender
    {
        static Dictionary<uint, Tuple<string, float>> m_reloadVideos = new Dictionary<uint, Tuple<string, float>>();
#if XB1_TMP
        static void CloseVideo(uint id)
		{
            Debug.Assert(false);
		}
        static void DrawVideo(uint id, Rectangle rect, Color color, MyVideoRectangleFitMode fitMode)
		{
            Debug.Assert(false);
		}
        internal static VideoState GetVideoState(uint id)
        {
            Debug.Assert(false);
			return VideoState.Stopped;
        }
        internal static bool IsVideoValid(uint id)
        {
            Debug.Assert(false);
            return false;
        }
        static void SetVideoVolume(uint id, float volume)
        {
            Debug.Assert(false);
        }
        static void UpdateVideo(uint id)
        {
            Debug.Assert(false);
        }
        static void PlayVideo(uint id, string fileName, float volume)
        {
            Debug.Assert(false);
        }
        static void UnloadContent_Video()
        {
            Debug.Assert(false);
        }
        static void LoadContent_Video()
        {
            Debug.Assert(false);
        }

#else
        static Dictionary<uint, MyVideoPlayerDx9> m_videos = new Dictionary<uint, MyVideoPlayerDx9>();

        static void LoadContent_Video()
        {
            foreach (var v in m_reloadVideos)
            {
                PlayVideo(v.Key, v.Value.Item1, v.Value.Item2);
            }

            m_reloadVideos.Clear();
        }

        static void UnloadContent_Video()
        {
            m_reloadVideos.Clear();

            foreach (var v in m_videos)
            {
                m_reloadVideos.Add(v.Key, new Tuple<string, float>(v.Value.FileName, v.Value.Volume));

                v.Value.Stop();
                v.Value.Dispose();
            }

            m_videos.Clear();
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        static void PlayVideo(uint id, string fileName, float volume)
        {
            CloseVideo(id);

            try
            {
                //  Start playback
                var video = new MyVideoPlayerDx9(fileName, MyRender.GraphicsDevice);
                video.Play();
                video.Volume = volume;
                m_videos.Add(id, video);
            }
            catch (Exception ex)
            {
                //  Log this exception but then ignore it
                MyRender.Log.WriteLine(ex);
            }
        }

        static void CloseVideo(uint id)
        {
            MyVideoPlayerDx9 video;
            if (m_videos.TryGetValue(id, out video))
            {
                video.Stop();
                video.Dispose();
                m_videos.Remove(id);
            }
        }

        static void UpdateVideo(uint id)
        {
            MyVideoPlayerDx9 video;
            if (m_videos.TryGetValue(id, out video))
            {
                video.Update();
            }
        }

        static void DrawVideo(uint id, Rectangle rect, Color color, MyVideoRectangleFitMode fitMode)
        {
            MyVideoPlayerDx9 video;
            if (m_videos.TryGetValue(id, out video))
            {
                Rectangle dst = rect;
                Rectangle src = new Rectangle(0, 0, video.VideoWidth, video.VideoHeight);
                var videoSize = new Vector2(video.VideoWidth, video.VideoHeight);
                float videoAspect = videoSize.X / videoSize.Y;
                float rectAspect = (float)rect.Width / (float)rect.Height;

                // Automatic decision based on ratios.
                if (fitMode == MyVideoRectangleFitMode.AutoFit)
                    fitMode = (videoAspect > rectAspect) ? MyVideoRectangleFitMode.FitHeight : MyVideoRectangleFitMode.FitWidth;

                float scaleRatio = 0.0f;
                switch (fitMode)
                {
                    case MyVideoRectangleFitMode.None:
                        break;

                    case MyVideoRectangleFitMode.FitWidth:
                        scaleRatio = (float)dst.Width / videoSize.X;
                        dst.Height = (int)(scaleRatio * videoSize.Y);
                        if (dst.Height > rect.Height)
                        {
                            var diff = dst.Height - rect.Height;
                            dst.Height = rect.Height;
                            diff = (int)(diff / scaleRatio);
                            src.Y += (int)(diff * 0.5f);
                            src.Height -= diff;
                        }
                        break;

                    case MyVideoRectangleFitMode.FitHeight:
                        scaleRatio = (float)dst.Height / videoSize.Y;
                        dst.Width = (int)(scaleRatio * videoSize.X);
                        if (dst.Width > rect.Width)
                        {
                            var diff = dst.Width - rect.Width;
                            dst.Width = rect.Width;
                            diff = (int)(diff / scaleRatio);
                            src.X += (int)(diff * 0.5f);
                            src.Width -= diff;
                        }
                        break;
                }
                dst.X = rect.Left + (rect.Width - dst.Width) / 2;
                dst.Y = rect.Top + (rect.Height - dst.Height) / 2;

                Texture texture = video.OutputFrame;

                // Draw upside down
                VRageMath.RectangleF destination = new VRageMath.RectangleF(dst.X, dst.Y, dst.Width, -dst.Height);
                VRageMath.Rectangle? source = src;
                Vector2 origin = new Vector2(src.Width / 2 * 0, src.Height);
                MyRender.DrawSprite(texture, null, ref destination, false, ref source, color, Vector2.UnitX, ref origin, VRageRender.Graphics.SpriteEffects.None, 0f);
            }
        }

        static void SetVideoVolume(uint id, float volume)
        {
            MyVideoPlayerDx9 video;
            if (m_videos.TryGetValue(id, out video))
            {
                video.Volume = volume;
            }
        }

        internal static bool IsVideoValid(uint id)
        {
            return m_videos.ContainsKey(id);
        }

        internal static VideoState GetVideoState(uint id)
        {
            MyVideoPlayerDx9 video;
            if (m_videos.TryGetValue(id, out video))
            {
                return ((VRageRender.VideoState)video.CurrentState);
            }

            return VideoState.Stopped;
        }
#endif
    }
}
