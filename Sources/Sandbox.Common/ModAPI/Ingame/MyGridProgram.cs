using System;
using System.Reflection;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    ///     All programmable block scripts derive from this class, meaning that all properties in this
    ///     class are directly available for use in your scripts.
    ///     If you use Visual Studio or other external editors to write your scripts, you can derive
    ///     directly from this class and have a compatible template.
    /// </summary>
    /// <example>
    ///     <code>
    /// public void Main()
    /// {
    ///     // Print out the time elapsed since the currently running programmable block was run
    ///     // the last time.
    ///     Echo(Me.CustomName + " was last run " + ElapsedTime.TotalSeconds + " seconds ago.");
    /// }
    /// </code>
    /// </example>
    public abstract class MyGridProgram : IMyGridProgram
    {
        private string m_storage = "";
        private MethodInfo m_mainMethod = null;
        private bool m_mainMethodSupportsArgument;
        private object[] m_argumentArray;

        protected MyGridProgram()
        {
            // First try to get the main method with a string argument. If this fails, try to get one without.
            var type = this.GetType();
            m_mainMethod = type.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            m_mainMethodSupportsArgument = m_mainMethod != null;

            if (m_mainMethodSupportsArgument)
                m_argumentArray = new object[1];
            else
                m_mainMethod = type.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        ///     Provides access to the grid terminal system as viewed from this programmable block.
        /// </summary>
        public virtual IMyGridTerminalSystem GridTerminalSystem { get; protected set; }

        /// <summary>
        ///     Gets a reference to the currently running programmable block.
        /// </summary>
        public virtual IMyProgrammableBlock Me { get; protected set; }

        /// <summary>
        ///     Gets the amount of in-game time elapsed from the previous run.
        /// </summary>
        public virtual TimeSpan ElapsedTime { get; protected set; }

        /// <summary>
        ///     Allows you to store data between game sessions.
        /// </summary>
        public virtual string Storage
        {
            get { return this.m_storage; }
            protected set { this.m_storage = value ?? ""; }
        }

        /// <summary>
        ///     Prints out text onto the currently running programmable block's detail info area.
        /// </summary>
        public Action<string> Echo { get; protected set; }

        IMyGridTerminalSystem IMyGridProgram.GridTerminalSystem
        {
            get { return GridTerminalSystem; }
            set { GridTerminalSystem = value; }
        }

        IMyProgrammableBlock IMyGridProgram.Me
        {
            get { return Me; }
            set { Me = value; }
        }

        TimeSpan IMyGridProgram.ElapsedTime
        {
            get { return ElapsedTime; }
            set { ElapsedTime = value; }
        }

        string IMyGridProgram.Storage
        {
            get { return Storage; }
            set { Storage = value; }
        }

        Action<string> IMyGridProgram.Echo
        {
            get { return Echo; }
            set { Echo = value; }
        }

        bool IMyGridProgram.HasMainMethod
        {
            get { return m_mainMethod != null; }
        }

        void IMyGridProgram.Main(string argument)
        {
            if (m_mainMethodSupportsArgument)
            {
                // Don't know if it's really necessary to predefine this argument array, I suspect not
                // due to the cleverness of the compiler, but I do it this way just in case. 
                // Obviously if programmable block execution becomes asynchronous at some point this 
                // must be reworked, or the program must be blocked to avoid multiple simultaneous runs.
                m_argumentArray[0] = argument ?? string.Empty;
                m_mainMethod.Invoke(this, m_argumentArray);
            }
            else
            {
                m_mainMethod.Invoke(this, null);
            }
        }
    }
}