using SharpDX;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VRageMath;
using Rectangle = VRageMath.Rectangle;
using Color = VRageMath.Color;
using Vector2 = VRageMath.Vector2;
using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageRender.Messages;

#if XB1
using XB1Interface;
#endif

namespace VRageRender
{
#if XB1
#if !XB1
	class MyMemory
    {
		[DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
		public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    }
#endif // !XB1

    class MyVideoPlayer
	{
		public VideoState CurrentState
		{
            get
            { 
                return m_wrapper.HasFinished() ? VideoState.Stopped : VideoState.Playing; 
            }
		}

		float m_volume;
        VideoState videoState;

		public float Volume { get { return m_volume; } set { m_volume = value; } }

		public void Stop()
        {
            //videoState = VideoState.Stopped;
        }

        public void Play()
        {
            //videoState = VideoState.Playing;
        }

        public void Dispose()
        {
            if (m_texture != RwTexId.NULL)
            {
                MyRwTextures.Destroy(m_texture);
                m_texture = RwTexId.NULL;
            }

            m_wrapper.Close();
        }

		public void Update()
        {
            //Debug.Assert(false, "Video Update Not Supported yet on XB1!");
        }

        protected unsafe void process_frame(byte[] frameData, int w, int h)
        {
            var mapping = MyMapping.MapDiscard(m_texture.Resource);

            int lineSize = SharpDX.DXGI.FormatHelper.SizeOfInBytes(VideoFormat) * w;
            int frameDataPos = 0;

            for (int y = 0; y < h; y++)
            {
                mapping.WriteAndPositionByRow(frameData, frameDataPos, lineSize);
                frameDataPos += lineSize;
            }

            mapping.Unmap();
        }

        internal void Draw(Rectangle rect, Color color, MyVideoRectangleFitMode fitMode)
        {
            int w = 0, h = 0;
            byte [] data = m_wrapper.GetTextureData(ref w, ref h);
            process_frame(data, w, h);

            Rectangle dst = rect;
            Rectangle src = new Rectangle(0, 0, w, h);
            var videoSize = new Vector2(w, h);
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


            VRageMath.RectangleF destination = new VRageMath.RectangleF(dst.X, dst.Y, dst.Width, -dst.Height);
            VRageMath.Rectangle? source = src;
            Vector2 origin = new Vector2(src.Width / 2 * 0, src.Height);

            MySpritesRenderer.AddSingleSprite(m_texture, videoSize, color, origin, Vector2.UnitX, source, destination);
        }


        const SharpDX.DXGI.Format VideoFormat = SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb;
        RwTexId m_texture = RwTexId.NULL;
        XB1Interface.XB1Interface.VideoPlayerWrapper m_wrapper = null;

        public MyVideoPlayer(string filename)
         //   : base(filename)
        {
            m_wrapper = XB1Interface.XB1Interface.CreateVideoPlayer(filename);
            //m_texture = MyRwTextures.CreateDynamicTexture(VideoWidth, VideoHeight, VideoFormat);
            videoState = VideoState.Stopped;

            int w = 0, h = 0;
            m_wrapper.GetVideoFrameSize(ref w,ref h);
            m_texture = MyRwTextures.CreateDynamicTexture(w, h, VideoFormat);
        }

	}

#else
    class MyMemory
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    }

    class MyVideoPlayer : DShowNET.VideoPlayer
    {
        ISrvTexture m_texture;

        const SharpDX.DXGI.Format VideoFormat = SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb;

        public MyVideoPlayer(string filename)
            : base(filename)
        {
            m_texture = MyManagers.RwTextures.CreateSrv("MyVideoPlayer.Texture", 
                VideoWidth, VideoHeight, VideoFormat,
                resourceUsage: ResourceUsage.Dynamic,
                cpuAccessFlags: CpuAccessFlags.Write);
        }

        protected override unsafe void OnFrame(byte[] frameData)
        {
            var mapping = MyMapping.MapDiscard(m_texture);

            int lineSize = SharpDX.DXGI.FormatHelper.SizeOfInBytes(VideoFormat) * VideoWidth;
            int frameDataPos = 0;

            for(int y=0; y<VideoHeight; y++)
            {
                mapping.WriteAndPositionByRow(frameData, lineSize, frameDataPos);
                frameDataPos += lineSize;
            }
            
            mapping.Unmap();
        }

        public override void Dispose()
        {
            MyManagers.RwTextures.DisposeTex(ref m_texture);
            base.Dispose();
        }

        internal void Draw(Rectangle rect, Color color, MyVideoRectangleFitMode fitMode)
        {
            Rectangle dst = rect;
            Rectangle src = new Rectangle(0, 0, VideoWidth, VideoHeight);
            var videoSize = new Vector2(VideoWidth, VideoHeight);
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


            VRageMath.RectangleF destination = new VRageMath.RectangleF(dst.X, dst.Y, dst.Width, -dst.Height);
            VRageMath.Rectangle? source = src;
            Vector2 origin = new Vector2(src.Width / 2 * 0, src.Height);
            
            MySpritesRenderer.AddSingleSprite(m_texture, videoSize, color, origin, Vector2.UnitX, source, destination);
        }
    }
#endif

    class MyVideoFactory
    {
        internal static Dictionary<uint, MyVideoPlayer> Videos = new Dictionary<uint, MyVideoPlayer>();
        internal static Mutex VideoMutex = new Mutex();

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCriticalAttribute]
        internal static void Create(uint id, string videoFile)
        {
            VideoMutex.WaitOne();

            if (Videos.ContainsKey(id))
            {
                Videos[id].Stop();
                Videos[id].Dispose();
                Videos.Remove(id);
            }

            try
            {
                var video = Videos[id] = new MyVideoPlayer(videoFile);
                video.Play();
            }
            catch (Exception e)
            {
                MyRender11.Log.WriteLine(e);
            }

            VideoMutex.ReleaseMutex();
        }
        internal static void Remove(uint GID)
        {
            var video = Videos.Get(GID);
            if (video != null)
            {
                video.Stop();
                video.Dispose();
                Videos.Remove(GID);
            }
            else MyRenderProxy.Assert(false);
        }
    }
}
