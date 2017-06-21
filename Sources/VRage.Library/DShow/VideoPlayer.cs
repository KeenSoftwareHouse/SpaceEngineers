using System;
using System.Runtime.InteropServices;
using System.Threading;
using VRage.Collections;

namespace DShowNET
{
#if !XB1
    /// <summary>
    /// Describes the state of a video player
    /// </summary>
    public enum VideoState
    {
        Playing,
        Paused,
        Stopped
    }

    /// <summary>
    /// Enables Video Playback in Microsoft XNA
    /// </summary>
    public abstract class VideoPlayer : ISampleGrabberCB, IDisposable
    {
        #region Media Type GUIDs
        private Guid MEDIATYPE_Video = new Guid(0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
        private Guid MEDIASUBTYPE_RGB24 = new Guid(0xe436eb7d, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);
        private Guid MEDIASUBTYPE_RGB32 = new Guid(0xe436eb7e, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70);

        private Guid FORMAT_VideoInfo = new Guid(0x05589f80, 0xc356, 0x11ce, 0xbf, 0x01, 0x00, 0xaa, 0x00, 0x55, 0x59, 0x5a);
        #endregion

        #region Private Fields

        private object m_comObject = null;
        /// <summary>
        /// The GraphBuilder interface ref
        /// </summary>
        private IGraphBuilder m_graphBuilder = null;

        /// <summary>
        /// The MediaControl interface ref
        /// </summary>
        private IMediaControl m_mediaControl = null;

        /// <summary>
        /// The MediaEvent interface ref
        /// </summary>
        private IMediaEventEx m_mediaEvent = null;

        /// <summary>
        /// The MediaPosition interface ref
        /// </summary>
        private IMediaPosition m_mediaPosition = null;

        private IBasicAudio m_basicAudio = null;

        /// <summary>
        /// The MediaSeeking interface ref
        /// </summary>
        private IMediaSeeking m_mediaSeeking = null;

        /// <summary>
        /// The Video File to play
        /// </summary>
        private string filename;

        /// <summary>
        /// The RGBA frame bytes used to set the data in the Texture2D Output Frame
        /// </summary>
        private MySwapQueue<byte[]> m_videoDataRgba;

        /// <summary>
        /// Private Video Width
        /// </summary>
        private int videoWidth = 0;

        /// <summary>
        /// Private Video Height
        /// </summary>
        private int videoHeight = 0;

        /// <summary>
        /// Average Time per Frame in milliseconds
        /// </summary>
        private long avgTimePerFrame;

        /// <summary>
        /// BitRate of the currently loaded video
        /// </summary>
        private int bitRate;

        /// <summary>
        /// Current state of the video player
        /// </summary>
        private VideoState currentState;

        /// <summary>
        /// Is Disposed?
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Current time position
        /// </summary>
        private long currentPosition;

        /// <summary>
        /// Video duration
        /// </summary>
        private long videoDuration;

        /// <summary>
        /// How transparent the video frame is.
        /// Takes effect on the next frame after this is updated
        /// Max Value: 255 - Opaque
        /// Min Value: 0   - Transparent
        /// </summary>
        private byte alphaTransparency = 255;
        #endregion

        #region Public Properties
        /// <summary>
        /// Width of the loaded video
        /// </summary>
        public int VideoWidth
        {
            get
            {
                return videoWidth;
            }
        }

        /// <summary>
        /// Height of the loaded video
        /// </summary>
        public int VideoHeight
        {
            get
            {
                return videoHeight;
            }
        }

        /// <summary>
        /// Gets or Sets the current position of playback in seconds
        /// </summary>
        public double CurrentPosition
        {
            get
            {
                return (double)currentPosition / 10000000;
            }
            set
            {
                if (value < 0)
                    value = 0;

                if (value > Duration)
                    value = Duration;

                m_mediaPosition.put_CurrentPosition(value);
                currentPosition = (long)value * 10000000;
            }
        }

        /// <summary>
        /// Returns the current position of playback, formatted as a time string (HH:MM:SS)
        /// </summary>
        public string CurrentPositionAsTimeString
        {
            get
            {
                double seconds = (double)currentPosition / 10000000;

                double minutes = seconds / 60;
                double hours = minutes / 60;

                int realHours = (int)Math.Floor(hours);
                minutes -= realHours * 60;

                int realMinutes = (int)Math.Floor(minutes);
                seconds -= realMinutes * 60;

                int realSeconds = (int)Math.Floor(seconds);

                return (realHours < 10 ? "0" + realHours.ToString() : realHours.ToString()) + ":" + (realMinutes < 10 ? "0" + realMinutes.ToString() : realMinutes.ToString()) + ":" + (realSeconds < 10 ? "0" + realSeconds.ToString() : realSeconds.ToString());
            }
        }

        /// <summary>
        /// Total duration in seconds
        /// </summary>
        public double Duration
        {
            get
            {
                return (double)videoDuration / 10000000;
            }
        }

        /// <summary>
        /// Returns the duration of the video, formatted as a time string (HH:MM:SS)
        /// </summary>
        public string DurationAsTimeString
        {
            get
            {
                double seconds = (double)videoDuration / 10000000;

                double minutes = seconds / 60;
                double hours = minutes / 60;

                int realHours = (int)Math.Floor(hours);
                minutes -= realHours * 60;

                int realMinutes = (int)Math.Floor(minutes);
                seconds -= realMinutes * 60;

                int realSeconds = (int)Math.Floor(seconds);

                return (realHours < 10 ? "0" + realHours.ToString() : realHours.ToString()) + ":" + (realMinutes < 10 ? "0" + realMinutes.ToString() : realMinutes.ToString()) + ":" + (realSeconds < 10 ? "0" + realSeconds.ToString() : realSeconds.ToString());
            }
        }

        /// <summary>
        /// Currently Loaded Video File
        /// </summary>
        public string FileName
        {
            get
            {
                return filename;
            }
        }

        /// <summary>
        /// Gets or Sets the current state of the video player
        /// </summary>
        public VideoState CurrentState
        {
            get
            {
                return currentState;
            }
            set
            {
                switch (value)
                {
                    case VideoState.Playing:
                        Play();
                        break;
                    case VideoState.Paused:
                        Pause();
                        break;
                    case VideoState.Stopped:
                        Stop();
                        break;
                }
            }
        }

        /// <summary>
        /// Is Disposed?
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return isDisposed;
            }
        }

        /// <summary>
        /// Number of Frames Per Second in the video file.
        /// Returns -1 if this cannot be calculated.
        /// </summary>
        public int FramesPerSecond
        {
            get
            {
                if (avgTimePerFrame == 0)
                    return -1;

                float frameTime = (float)avgTimePerFrame / 10000000.0f;
                float framesPS = 1.0f / frameTime;
                return (int)Math.Round(framesPS, 0, MidpointRounding.ToEven);
            }
        }

        /// <summary>
        /// The number of milliseconds between each frame
        /// Returns -1 if this cannot be calculated
        /// </summary>
        public float MillisecondsPerFrame
        {
            get
            {
                if (avgTimePerFrame == 0)
                    return -1;

                return (float)avgTimePerFrame / 10000.0f;
            }
        }

        /// <summary>
        /// Gets or sets how transparent the video frame is.
        /// Takes effect on the next frame after this is updated
        /// Max Value: 255 - Opaque
        /// Min Value: 0   - Transparent
        /// </summary>
        public byte AlphaTransparency
        {
            get
            {
                return alphaTransparency;
            }
            set
            {
                alphaTransparency = value;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new Video Player. Automatically creates the required Texture2D on the specificied GraphicsDevice.
        /// </summary>
        /// <param name="FileName">The video file to open</param>
        /// <param name="graphicsDevice">XNA Graphics Device</param>
        protected VideoPlayer(string FileName)
        {
            try
            {
                // Set video state
                currentState = VideoState.Stopped;

                // Store Filename
                filename = FileName;

                // Open DirectShow Interfaces
                InitInterfaces();

                // Create a SampleGrabber Filter and add it to the FilterGraph
                //SampleGrabber sg = new SampleGrabber();
                var comtype = Type.GetTypeFromCLSID(Clsid.SampleGrabber);
                if (comtype == null)
                    throw new NotSupportedException("DirectX (8.1 or higher) not installed?");
                m_comObject = Activator.CreateInstance(comtype);

                ISampleGrabber sampleGrabber = (ISampleGrabber)m_comObject;
                m_graphBuilder.AddFilter((IBaseFilter)m_comObject, "Grabber");

                // Setup Media type info for the SampleGrabber
                AMMediaType mt = new AMMediaType();
                mt.majorType = MEDIATYPE_Video;     // Video
                mt.subType = MEDIASUBTYPE_RGB32;    // RGB32
                mt.formatType = FORMAT_VideoInfo;   // VideoInfo
                sampleGrabber.SetMediaType(mt);

                // Construct the rest of the FilterGraph
                m_graphBuilder.RenderFile(filename, null);

                // Set SampleGrabber Properties
                sampleGrabber.SetBufferSamples(true);
                sampleGrabber.SetOneShot(false);
                sampleGrabber.SetCallback((ISampleGrabberCB)this, 1);

                // Hide Default Video Window
                IVideoWindow pVideoWindow = (IVideoWindow)m_graphBuilder;
                //pVideoWindow.put_AutoShow(OABool.False);
                pVideoWindow.put_AutoShow(0);

                // Create AMMediaType to capture video information
                AMMediaType MediaType = new AMMediaType();
                sampleGrabber.GetConnectedMediaType(MediaType);
                VideoInfoHeader pVideoHeader = new VideoInfoHeader();
                Marshal.PtrToStructure(MediaType.formatPtr, pVideoHeader);

                // Store video information
                videoHeight = pVideoHeader.BmiHeader.Height;
                videoWidth = pVideoHeader.BmiHeader.Width;
                avgTimePerFrame = pVideoHeader.AvgTimePerFrame;
                bitRate = pVideoHeader.BitRate;
                m_mediaSeeking.GetDuration(out videoDuration);

                // Create byte arrays to hold video data
                m_videoDataRgba = new MySwapQueue<byte[]>(() => new byte[(videoHeight * videoWidth) * 4]); // RGBA format (4 bytes per pixel)
            }
            catch (Exception e)
            {
                throw new Exception("Unable to Load or Play the video file", e);
            }
        }
        #endregion

        #region DirectShow Interface Management
        /// <summary>
        /// Initialises DirectShow interfaces
        /// </summary>
        private void InitInterfaces()
        {
            var comtype = Type.GetTypeFromCLSID(Clsid.FilterGraph);
            if (comtype == null)
                throw new NotSupportedException("DirectX (8.1 or higher) not installed?");
            var fg = Activator.CreateInstance(comtype);

            m_graphBuilder = (IGraphBuilder)fg;
            m_mediaControl = (IMediaControl)fg;
            m_mediaEvent = (IMediaEventEx)fg;
            m_mediaSeeking = (IMediaSeeking)fg;
            m_mediaPosition = (IMediaPosition)fg;
            m_basicAudio = (IBasicAudio)fg;

            fg = null;
        }

        /// <summary>
        /// Closes DirectShow interfaces
        /// </summary>
        private void CloseInterfaces()
        {
            if (m_mediaEvent != null)
            {
                m_mediaControl.Stop();
                //0x00008001 = WM_GRAPHNOTIFY
                m_mediaEvent.SetNotifyWindow(IntPtr.Zero, 0x00008001, IntPtr.Zero);
            }
            m_mediaControl = null;
            m_mediaEvent = null;
            m_graphBuilder = null;
            m_mediaSeeking = null;
            m_mediaPosition = null;
            m_basicAudio = null;

            if (m_comObject != null)
                Marshal.ReleaseComObject(m_comObject);
            m_comObject = null;
        }
        #endregion

        #region Update and Media Control
        /// <summary>
        /// Updates the Output Frame data using data from the video stream. Call this in Game.Update().
        /// </summary>
        public void Update()
        {
            //using (MyRenderStats.Measure("VideoUpdate-CopyTexture", MyStatTypeEnum.Max))
            {
                // Set video data into the Output Frame
                if (m_videoDataRgba.RefreshRead())
                {

                }
                // now for some reason after changing to fullscreen it's not refreshed, so we will always call it
                {
                    OnFrame(m_videoDataRgba.Read);
                }
                // Update current position read-out
                m_mediaSeeking.GetCurrentPosition(out currentPosition);
                if (currentPosition >= videoDuration)
                {
                    currentState = VideoState.Stopped;
                }
            }
        }

        protected abstract void OnFrame(byte[] frameData);

        /// <summary>
        /// Starts playing the video
        /// </summary>
        public void Play()
        {
            if (currentState != VideoState.Playing)
            {
                // Start the FilterGraph
                m_mediaControl.Run();

                // Update VideoState
                currentState = VideoState.Playing;
            }
        }

        /// <summary>
        /// Pauses the video
        /// </summary>
        public void Pause()
        {
            // Stop the FilterGraph (but remembers the current position)
            m_mediaControl.Stop();

            // Update VideoState
            currentState = VideoState.Paused;
        }

        /// <summary>
        /// Stops playing the video
        /// </summary>
        public void Stop()
        {
            // Stop the FilterGraph
            m_mediaControl.Stop();

            // Reset the current position
            m_mediaSeeking.SetPositions(new DsOptInt64(0), SeekingFlags.AbsolutePositioning, new DsOptInt64(0), SeekingFlags.NoPositioning);

            // Update VideoState
            currentState = VideoState.Stopped;
        }

        /// <summary>
        /// Rewinds the video to the start and plays it again
        /// </summary>
        public void Rewind()
        {
            Stop();
            Play();
        }
        #endregion

        #region ISampleGrabberCB Members and Helpers
        /// <summary>
        /// Required public callback from DirectShow SampleGrabber. Do not call this method.
        /// </summary>
        public int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            //using (MyRenderStats.Measure("Video grab frame", MyStatTypeEnum.Max))
            {
                byte[] write = m_videoDataRgba.Write;
                byte alpha = alphaTransparency;

                Marshal.Copy(pBuffer, write, 0, BufferLen);

                // This takes around 5ms for 1080p on i5-2500 CPU
                for (int i = 3; i < BufferLen; i += 4)
                {
                    write[i] = alpha;
                }

                m_videoDataRgba.CommitWrite();
            }

            // Return S_OK
            return 0;
        }

        /// <summary>
        /// Required public callback from DirectShow SampleGrabber. Do not call this method.
        /// </summary>
        public int SampleCB(double SampleTime, IMediaSample pSample)
        {
            // Return S_OK
            return 0;
        }

        //convert from 0..1f to -10000..0i
        public float Volume
        {
            get
            {
                int volume;
                m_basicAudio.get_Volume(out volume);
                return (volume / 10000.0f) + 1.0f;
            }
            set
            {
                m_basicAudio.put_Volume((int)((value - 1.0f) * 10000.0f));
            }
        }


        #endregion

        #region IDisposable Members
        /// <summary>
        /// Cleans up the Video Player. Must be called when finished with the player.
        /// </summary>
        public virtual void Dispose()
        {
            isDisposed = true;

            Stop();
            CloseInterfaces();

            m_videoDataRgba = null;
        }
        #endregion
    }
#endif
}