#if !XB1
using System;
using System.Reflection;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    ///     All programmable block scripts derive from this class, meaning that all properties in this
    ///     class are directly available for use in your scripts.
    ///     If you use Visual Studio or other external editors to write your scripts, you can derive
    ///     directly from this class and have a compatible script template.
    /// </summary>
    /// <example>
    ///     <code>
    /// public void Main()
    /// {
    ///     // Print out the time elapsed since the currently running programmable block was run
    ///     // the last time.
    ///     Echo(Me.CustomName + " was last run " + Runtime.TimeSinceLastRun.TotalSeconds + " seconds ago.");
    /// }
    /// </code>
    /// </example>
    public abstract class MyGridProgram : IMyGridProgram
    {
        /*
        * Note from the author (Malware):
        *
        * This class is intended to serve not only as the base class of the "real" ingame-scripts, but also when
        * used externally, for instance when writing scripts in Visual Studio. It is also designed to be unit-testable
        * which means it should never rely on anything that requires the game to run. Everything should be configurable
        * via the IMyGridProgram interface, which is not available in-game.
        * 
        * Finally, as any member of this class becomes a root member of the script, try to avoid overcrowding it. If 
        * possible, compartmentalize (like for instance the Runtime Info).
        */

        // WARNING: Do not autoinitialize any of these fields, or the grid program initialization process
        // will fail.
        private string m_storage;
        private readonly Action<string> m_main;
        private readonly Action m_save;

        protected MyGridProgram()
        {
            // First try to get the main method with a string argument. If this fails, try to get one without.
            var type = this.GetType();
            var mainMethod = type.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] {typeof(string)}, null);
            if (mainMethod != null)
            {
                this.m_main = mainMethod.CreateDelegate<Action<string>>(this);
            }
            else
            {
                mainMethod = type.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (mainMethod != null)
                {
                    var mainWithoutArgument = mainMethod.CreateDelegate<Action>(this);
                    this.m_main = arg => mainWithoutArgument();
                }
            }

            var saveMethod = type.GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (saveMethod != null)
            {
                this.m_save = saveMethod.CreateDelegate<Action>(this);
            }
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
        [Obsolete("Use Runtime.TimeSinceLastRun instead")]
        public virtual TimeSpan ElapsedTime { get; protected set; }

        /// <summary>
        /// Gets runtime information for the running grid program.
        /// </summary>
        public virtual IMyGridProgramRuntimeInfo Runtime { get; protected set; }

        /// <summary>
        ///     Allows you to store data between game sessions.
        /// </summary>
        public virtual string Storage
        {
            get { return this.m_storage ?? ""; }
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

        IMyGridProgramRuntimeInfo IMyGridProgram.Runtime
        {
            get { return Runtime; }
            set { Runtime = value; }
        }

        bool IMyGridProgram.HasMainMethod
        {
            get { return m_main != null; }
        }

        void IMyGridProgram.Main(string argument)
        {
            if (m_main == null)
                throw new InvalidOperationException("No Main method available");
            m_main(argument ?? string.Empty);
        }

        bool IMyGridProgram.HasSaveMethod
        {
            get { return this.m_save != null; }
        }

        void IMyGridProgram.Save()
        {
            if (m_save == null)
                return;
            m_save();
        }
    }
}
#endif // !XB1
