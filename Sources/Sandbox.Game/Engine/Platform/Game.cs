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

        [Obsolete("Remove asap, it is here only because of main menu music..")]
        protected readonly MyGameTimer m_gameTimer = new MyGameTimer();

        private MyTimeSpan m_drawTime;
        private MyTimeSpan m_updateTime;

        /// <summary>
        /// This should be never called, it's here only for temporal compatibility
        /// </summary>
        public MyTimeSpan GetNewTimestamp()
        {
            // TODO: OP! Remove, this is hack for temporal compatibility
            return m_gameTimer.Elapsed;
        }

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
               // Debug.Assert(Thread.CurrentThread == UpdateThread);
                return m_updateTime;
            }
        }

        private bool isFirstUpdateDone;

        private bool isMouseVisible;

        public Thread UpdateThread { get; protected set; }
        public Thread DrawThread { get; protected set; }

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Game" /> class.
        /// </summary>
        public Game()
        {
            IsActive = true;
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

	    #endregion

        #region Public Methods and Operators

        FixedLoop m_renderLoop = new FixedLoop(Stats.Generic, "WaitForUpdate");

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
            MyLog.Default.WriteLine("Timer Frequency: " + MyGameTimer.Frequency);
            MyLog.Default.WriteLine("Ticks per frame: " + m_renderLoop.TickPerFrame);
            m_renderLoop.Run(RunSingleFrame);
        }

        public void RunSingleFrame()
        {
            UpdateInternal();
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