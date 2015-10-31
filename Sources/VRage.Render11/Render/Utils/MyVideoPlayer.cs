using SharpDX;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VRageMath;
using Rectangle = VRageMath.Rectangle;
using Color = VRageMath.Color;
using Vector2 = VRageMath.Vector2;
using System;
using VRageRender.Resources;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;

namespace VRageRender
{
    class MyMemory
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    }

    class MyVideoPlayer : DShowNET.VideoPlayer
    {
        RwTexId m_texture = RwTexId.NULL;

        const SharpDX.DXGI.Format VideoFormat = SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb;

        public MyVideoPlayer(string filename)
            : base(filename)
        {
            m_texture = MyRwTextures.CreateDynamicTexture(VideoWidth, VideoHeight, VideoFormat);
        }

        protected override unsafe void OnFrame(byte[] frameData)
        {
            var mapping = MyMapping.MapDiscard(m_texture.Resource);

            var lineSize = (uint)(SharpDX.DXGI.FormatHelper.SizeOfInBytes(VideoFormat) * VideoWidth);
            var rowPitch = mapping.dataBox.RowPitch;

            fixed(byte *ptr = frameData)
            {
                for(int y=0; y<VideoHeight; y++)
                {
                    var dst = new IntPtr((byte*)mapping.dataBox.DataPointer.ToPointer() + rowPitch * y);
                    var src = new IntPtr((byte*)ptr + lineSize * y);
                    MyMemory.CopyMemory(dst, src, lineSize);
                }
            }
            
            mapping.Unmap();
        }

        public override void Dispose()
        {
            if (m_texture != RwTexId.NULL)
            {
                MyRwTextures.Destroy(m_texture);
                m_texture = RwTexId.NULL;
            }

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
            
            MySpritesRenderer.AddSingleSprite(m_texture.ShaderView, videoSize, color, origin, Vector2.UnitX, source, destination);
        }
    }

    class MyVideoFactory
    {
        internal static Dictionary<uint, MyVideoPlayer> Videos = new Dictionary<uint, MyVideoPlayer>();
        internal static Mutex VideoMutex = new Mutex();

        internal static void Create(uint id, string videoFile)
        {
            VideoMutex.WaitOne();

            if(Videos.ContainsKey(id))
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
            catch(Exception e)
            {
                MyRender11.Log.WriteLine(e);
            }

            VideoMutex.ReleaseMutex();
        }
    }
}
