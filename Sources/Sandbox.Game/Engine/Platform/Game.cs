using System;
using System.Collections.Generic;

using SharpDX.Collections;

using System.Threading;
using System.Diagnostics;
using SharpDX;
using System.Globalization;

using Sandbox.Engine.Utils;
using Sandbox.Game.Debugging;
using System.Runtime.InteropServices;
using VRage.Win32;
using VRage;
using VRage.Utils;
using System.Net;
using VRage.Library.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using System.IO;
using VRage.Collections;
using VRage.Profiler;
using VRageRender.Utils;

namespace Sandbox.Engine.Platform
{
    /// <summary>
    /// The game.
    /// </summary>
    public abstract class Game
    {
        #region Fields

        public static bool IsDedicated = false;
        public static bool IsPirated = false;
        public static bool IgnoreLastSession = false;
        public static IPEndPoint ConnectToServer = null;
        public static bool EnableSimSpeedLocking = false;

        [Obsolete("Remove asap, it is here only because of main menu music..")]
        protected readonly MyGameTimer m_gameTimer = new MyGameTimer();

        private MyTimeSpan m_drawTime;
        private MyTimeSpan m_updateTime;
        private ulong m_updateCounter = 0;

        const double TARGET_MS_PER_FRAME = 1000 / 60.0;

        const int NUM_FRAMES_FOR_DROP = 5;

        const float NUM_MS_TO_INCREASE = 2000;

        const float PEAK_TRESHOLD_RATIO = 0.4f;

        const float RATIO_TO_INCREASE_INSTANTLY = 0.25f;

        float m_currentFrameIncreaseTime = 0;

        long m_currentMin = 0;

        long m_targetTicks = 0;

        MyQueue<long> m_lastFrameTiming = new MyQueue<long>(NUM_FRAMES_FOR_DROP);

        public MyTimeSpan DrawTime
        {
            get
            {
                Debug.Assert(Thread.CurrentThread == DrawThread);
                return m_drawTime;
            }
        }

        public MyTimeSpan UpdateTime
        {
            get
            {
                return m_updateTime;
            }
        }

        public MyTimeSpan SimulationTime
        {
            get
            {
                return MyTimeSpan.FromMilliseconds(m_updateCounter * TARGET_MS_PER_FRAME);
            }
        }

        public double TimerMultiplier
        {
            get { return m_gameTimer.Multiplier; }
            set { m_gameTimer.Multiplier = value; }
        }

        private bool isFirstUpdateDone;

        private bool isMouseVisible;

        public Thread UpdateThread { get; protected set; }
        public Thread DrawThread { get; protected set; }

        public long FrameTimeTicks;

        ManualResetEventSlim m_waiter;
        MyTimer.TimerEventHandler m_handler;


        public static float SimulationRatio { get { return (float)TARGET_MS_PER_FRAME / m_targetMs; } }

        static long m_lastFrameTime = 0;

        static float m_targetMs = 0.0f;
        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Game" /> class.
        /// </summary>
        public Game()
        {
            IsActive = true;
            m_waiter = new ManualResetEventSlim(false, 0);
            m_handler = new MyTimer.TimerEventHandler((a, b, c, d, e) =>
            {
                m_waiter.Set();
            });
        }

        #endregion

        #region Public Events
        
        public event Action OnGameExit;
        
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets a value indicating whether is running.
        /// </summary>
        public bool IsRunning { get; private set; }

	    public bool IsFirstUpdateDone { get { return isFirstUpdateDone; } }

        public bool EnableMaxSpeed
        {
            get { return m_renderLoop.EnableMaxSpeed; }
            set { m_renderLoop.EnableMaxSpeed = value; }
        }

	    #endregion

        #region Public Methods and Operators

        readonly FixedLoop m_renderLoop = new FixedLoop(Stats.Generic, "WaitForUpdate");

        public void SetNextFrameDelayDelta(int delta)
        {
            m_renderLoop.SetNextFrameDelayDelta(delta);
        }

        /// <summary>
        /// Exits the game.
        /// </summary>
        public void Exit()
        {
            var handler = OnGameExit;
            if (handler != null) handler();
            m_renderLoop.IsDone = true;
        }

        /// <summary>
        /// Updates the game's clock and calls Update
        /// </summary>
        protected void RunLoop()
        {
            try
            {
                m_targetTicks = m_renderLoop.TickPerFrame;
                MyLog.Default.WriteLine("Timer Frequency: " + MyGameTimer.Frequency);
                MyLog.Default.WriteLine("Ticks per frame: " + m_renderLoop.TickPerFrame);
                m_renderLoop.Run(RunSingleFrame);
            }
            catch (SEHException exception)
            {
#if !XB1
                MyLog.Default.WriteLine("SEHException caught. Error code: " + exception.ErrorCode.ToString());
#else // XB1
                System.Diagnostics.Debug.Assert(false, "System.Runtime.InteropServices.ExternalException.ErrorCode not supported on XB1");
#endif // XB1
                throw exception;
            }
        }

    
        public void RunSingleFrame()
        {
            long beforeUpdate = MyPerformanceCounter.ElapsedTicks;
           
            UpdateInternal();

            FrameTimeTicks = MyPerformanceCounter.ElapsedTicks - beforeUpdate;

            if (MyFakes.PRECISE_SIM_SPEED)
            {
                long currentTicks = Math.Min(Math.Max(m_renderLoop.TickPerFrame, UpdateCurrentFrame()), 10 * m_renderLoop.TickPerFrame);
                m_targetMs = (float)Math.Max(TARGET_MS_PER_FRAME,MyPerformanceCounter.TicksToMs(currentTicks));
            }

            if (EnableSimSpeedLocking && MyFakes.ENABLE_SIMSPEED_LOCKING)
            {
                Lock(beforeUpdate);
            }
        }

        private void Lock(long beforeUpdate)
        {
            //maximum sim speed can be 1.0 minimum 0.01, during loading there can be peaks more than 100 ms and we dont want to lock sim speed to such values
            long currentTicks = Math.Min(Math.Max(m_renderLoop.TickPerFrame, UpdateCurrentFrame()), 10 * m_renderLoop.TickPerFrame);

            m_currentMin = Math.Max(currentTicks, m_currentMin);
            m_currentFrameIncreaseTime += m_targetMs;

            if (currentTicks > m_targetTicks)
            {
                m_targetTicks = currentTicks;
                m_currentFrameIncreaseTime = 0;
                m_currentMin = 0;
                m_targetMs = (float)MyPerformanceCounter.TicksToMs(m_targetTicks);
            }
            else
            {
                //if there was spike that was longer than 5 frames that caused drop and after that sim speed increased more than 0.2 we want to get back to those values
                //no need to lock to lower values
                long difference = m_targetTicks - m_currentMin;
                bool increaseInstantly = difference > RATIO_TO_INCREASE_INSTANTLY * m_renderLoop.TickPerFrame;

                if (m_currentFrameIncreaseTime > NUM_MS_TO_INCREASE || increaseInstantly)
                {
                    m_targetTicks = m_currentMin;
                    m_currentFrameIncreaseTime = 0;
                    m_currentMin = 0;
                    m_targetMs = (float)MyPerformanceCounter.TicksToMs(m_targetTicks);
                }
            }

            long remainingTicksTowait = MyPerformanceCounter.ElapsedTicks - beforeUpdate;
            var remainingTimeToWait = MyTimeSpan.FromTicks(m_targetTicks - remainingTicksTowait);

            int waitMs = (int)(remainingTimeToWait.Milliseconds - 0.1);
            if (waitMs > 0 && !EnableMaxSpeed)
            {

                m_waiter.Reset();
                MyTimer.StartOneShot(waitMs, m_handler);
                m_waiter.Wait(waitMs + 1);
            }

            remainingTicksTowait = MyPerformanceCounter.ElapsedTicks - beforeUpdate;

            while (m_targetTicks > remainingTicksTowait)
            {
                remainingTicksTowait = MyPerformanceCounter.ElapsedTicks - beforeUpdate;
            }
        }

        long UpdateCurrentFrame()
        {
            if (m_lastFrameTiming.Count > NUM_FRAMES_FOR_DROP)
            {
                m_lastFrameTiming.Dequeue();
            }

            m_lastFrameTiming.Enqueue(FrameTimeTicks);

            long min = long.MaxValue;
            long max = 0;

            double average = 0.0;
            for (int i = 0; i < m_lastFrameTiming.Count; ++i)
            {
                min = Math.Min(min, m_lastFrameTiming[i]);
                max = Math.Max(max, m_lastFrameTiming[i]);
                average += m_lastFrameTiming[i];
            }
            average /= m_lastFrameTiming.Count;

            double spikeDelta = (max - min) * PEAK_TRESHOLD_RATIO;
            long noSpike = 0;
            for (int i = 0; i < m_lastFrameTiming.Count; ++i)
            {
                if (Math.Abs(m_lastFrameTiming[i] - average)< spikeDelta)
                {
                    noSpike = Math.Max(max, m_lastFrameTiming[i]);
                }
            }
            if (noSpike == 0)
            {
                return (long)average;
            }
            return noSpike;
        }
        #endregion

        #region Methods

        protected abstract void PrepareForDraw();

        protected abstract void LoadData_UpdateThread();

        protected abstract void UnloadData_UpdateThread();

        private void UpdateInternal()
        {
            using (Stats.Generic.Measure("BeforeUpdate"))
            {
                ProfilerShort.Begin("UpdateInternal::BeforeUpdate");
                VRageRender.MyRenderProxy.BeforeUpdate();
                ProfilerShort.End();
            }

            ProfilerShort.Begin("UpdateInternal::Update");
            //VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.Default, "Update Start");

            m_updateTime = m_gameTimer.Elapsed;
            m_updateCounter++;

            if (VRage.MyCompilationSymbols.EnableNetworkPacketTracking)
                System.Diagnostics.Debug.WriteLine("----- Tick # " + m_updateTime.Milliseconds);

            Update();
            ProfilerShort.End();

            if (!IsDedicated)
            {
                ProfilerShort.Begin("UpdateInternal::PrepareForDraw");
                PrepareForDraw();
                ProfilerShort.End();
            }

            using (Stats.Generic.Measure("AfterUpdate"))
            {
                ProfilerShort.Begin("UpdateInternal::AfterUpdate");
                VRageRender.MyRenderProxy.AfterUpdate(m_updateTime);
                ProfilerShort.End();
            }

            ProfilerShort.Commit();
            MySimpleProfiler.Commit();
            //VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.Default, "Update End");
        }

        /// <summary>
        /// Reference page contains links to related conceptual articles.
        /// </summary>
        /// <param name="gameTime">
        /// Time passed since the last call to Update.
        /// </param>
        protected virtual void Update()
        {
            isFirstUpdateDone = true;
        }

        #endregion
    }
}