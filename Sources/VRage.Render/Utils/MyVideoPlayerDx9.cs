using SharpDX;
using SharpDX.Direct3D9;
using System.Runtime.InteropServices;

namespace VRageRender
{
#if !XB1
    class MyVideoPlayerDx9 : DShowNET.VideoPlayer
    {
        /// <summary>
        /// Private Texture2D to render video to. Created in the Video Player Constructor.
        /// </summary> 
        private Texture m_outputFrame;

        /// <summary>
        /// Automatically updated video frame. Render this to the screen using a SpriteBatch.
        /// </summary>
        public Texture OutputFrame
        {
            get { return m_outputFrame; }
        }

        public MyVideoPlayerDx9(string FileName, Device graphicsDevice) :
            base(FileName)
        {
            // Create Output Frame Texture2D with the height and width of the video
            m_outputFrame = new Texture(graphicsDevice, base.VideoWidth, base.VideoHeight, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);
        }

        protected override void OnFrame(byte[] frameData)
        {
            DataRectangle dr = m_outputFrame.LockRectangle(0, LockFlags.Discard);
            int bytesPerRow = base.VideoWidth * 4;

            for (int j = 0; j < base.VideoHeight; j++)
            {
                Marshal.Copy(frameData, bytesPerRow * j, dr.DataPointer + dr.Pitch * j, bytesPerRow);
            }
            m_outputFrame.UnlockRectangle(0);
        }

        public override void Dispose()
        {
            base.Dispose();

            m_outputFrame.Dispose();
            m_outputFrame = null;
        }

    }
#endif
}
