using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Compiler;

namespace Sandbox.Game.Entities.Blocks
{
    /// <summary>
    /// This class has the responsibility of running and maintaining a <see cref="MyGridProgram"/> instance.
    /// </summary>
    class MyGridProgramRuntime
    {
        [Flags]
        public enum RunFlags
        {
            None,

            ConstructionOnly = 0x1
        }

        private const int MAX_NUM_EXECUTED_INSTRUCTIONS = 50000;
        private static readonly double STOPWATCH_FREQUENCY = 1.0 / Stopwatch.Frequency;
        private const int MAX_ECHO_LENGTH = 8000; // 100 lines á 80 characters

        private readonly MyProgrammableBlock m_programmableBlock;
        private IMyGridProgram m_instance;
        private long m_previousRunTimestamp;
        private StringBuilder m_echoOutput = new StringBuilder();
        private bool m_isRunning;
        private string m_storage = "";
        private ConstructorInfo m_constructor;
        private string m_faultMessage;

        public MyGridProgramRuntime(MyProgrammableBlock programmableBlock)
        {
            if (programmableBlock == null)
            {
                throw new ArgumentNullException("programmableBlock");
            }
            m_programmableBlock = programmableBlock;

            // The default state of a program runtime is Faulted because no program has been loaded yet.
            m_faultMessage = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoMain);
        }

        /// <summary>
        /// Returns <c>true</c> if the currently loaded program needs to be constructed
        /// </summary>
        public bool NeedsConstruction
        {
            get { return m_constructor != null; }
        }

        /// <summary>
        /// Gets or sets the storage used by any instantiated grid program. This storage
        /// value is always kept up to date, but any changes to this value from the outside
        /// will only be applied on the next <see cref="TryLoad"/> call.
        /// </summary>
        public string Storage
        {
            get
            {
                // If there is an existing program instance without fault, make sure the current storage
                // is updated.
                if (m_instance != null && !IsFaulted)
                    m_storage = m_instance.Storage ?? "";
                return m_storage;
            }
            set { m_storage = value ?? ""; }
        }

        /// <summary>
        /// Determines whether the program is currently running.
        /// </summary>
        public bool IsRunning { get { return m_isRunning; } }

        /// <summary>
        /// Determines if this runtime is in a faulted state.
        /// </summary>
        public bool IsFaulted
        {
            get { return m_faultMessage != null; }
        }
        
        /// <summary>
        /// If <see cref="IsFaulted"/> is <c>true</c>, this property returns a human-readable reason why.
        /// </summary>
        public string FaultMessage
        {
            get { return m_faultMessage; }
        }

        /// <summary>
        /// Loads a new program and prepares it for execution.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="output">An output string which will contain the human-readable reason why the load failed.</param>
        public bool TryLoad(Type type, out string output)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            output = "";
            try
            {
                // TODO: Is this needed now that the constructor is not called during instantiation?
                IlInjector.RestartCountingInstructions(MAX_NUM_EXECUTED_INSTRUCTIONS);

                // Save the current storage now, to make sure any preexisting program has a chance to store
                // its state.
                var storage = Storage;
                
                // Create an instance of this object without invoking any constructor. We want to do so in
                // a controlled environment and after initializing all fields.
                m_instance = FormatterServices.GetUninitializedObject(type) as IMyGridProgram;
                if (m_instance == null)
                {
                    m_faultMessage = output = string.Format(MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NotAGridProgram), typeof(IMyGridProgram).Name);
                    return false;
                }

                // Get the default constructor of this type so we can invoke it in a controlled environment.
                // We don't bother creating a delegate of this one, because it's only run once in the entire
                // lifetime of the script.
                m_constructor = m_instance.GetType().GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (m_constructor == null)
                {
                    m_faultMessage = output = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_InvalidConstructor);
                    return false;
                }

                m_previousRunTimestamp = 0;
                m_faultMessage = null;
                m_instance.Storage = storage;
                m_instance.Me = m_programmableBlock;
                m_instance.Echo = Echo;
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    output = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.InnerException.Message;
                    return false;
                }
            }
            return true;
        }

        private void OnProgramTermination()
        {
            m_instance = null;
            m_previousRunTimestamp = 0;
            m_echoOutput.Clear();
        }

        private bool PrepareToRun(MyGridTerminalSystem terminalSystem, ref string output)
        {
            if (m_instance == null)
            {
                m_faultMessage = output = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoMain);
                OnProgramTermination();
                return false;
            }

            IlInjector.RestartCountingInstructions(MAX_NUM_EXECUTED_INSTRUCTIONS);
            m_echoOutput.Clear();

            if (m_previousRunTimestamp == 0)
            {
                m_previousRunTimestamp = Stopwatch.GetTimestamp();
                m_instance.ElapsedTime = TimeSpan.Zero;
            }
            else
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                var elapsedTime = (currentTimestamp - m_previousRunTimestamp) * Sync.RelativeSimulationRatio;
                m_instance.ElapsedTime = TimeSpan.FromSeconds(elapsedTime * STOPWATCH_FREQUENCY);
                m_previousRunTimestamp = currentTimestamp;
            }
            m_instance.GridTerminalSystem = terminalSystem;
            return true;
        }

        /// <summary>
        /// Attempts to run the currently loaded program.
        /// </summary>
        /// <param name="terminalSystem">The grid terminal system to pass into the program</param>
        /// <param name="argument">An optional argument to pass into the script's Main function</param>
        /// <param name="output">The output text, which is a combination of Echo calls and error messages.</param>
        /// <param name="runFlags">Behavior modification flags</param>
        /// <returns><c>true</c> if the script was run successfully, <c>false</c> otherwise</returns>
        public bool TryRun(MyGridTerminalSystem terminalSystem, string argument, out string output, RunFlags runFlags)
        {
            output = "";
            if (!PrepareToRun(terminalSystem, ref output))
                return false;

            m_isRunning = true;
            var success = false;
            try
            {
                EnsureConstructedState();

                if (!runFlags.HasFlag(RunFlags.ConstructionOnly))
                {
                    if (!m_instance.HasMainMethod)
                    {
                        m_faultMessage = output = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_NoMain);
                        OnProgramTermination();
                        return false;
                    }
                    m_instance.Main(argument);
                }

                if (m_echoOutput.Length > 0)
                    output = m_echoOutput.ToString();
                success = true;
            }
            catch (Exception ex)
            {
                // Since we just had an exception I'm not fussed about using old 
                // fashioned string concatenation here. We'll still want the echo
                // output, since its primary purpose is debugging.
                if (m_echoOutput.Length > 0)
                    output = m_echoOutput.ToString();
                OnProgramTermination();
                if (ex is ScriptOutOfRangeException)
                {
                    m_faultMessage = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_TooComplex);
                }
                else
                {
                    m_faultMessage = MyTexts.GetString(MySpaceTexts.ProgrammableBlock_Exception_ExceptionCaught) + ex.Message;
                }
                output += m_faultMessage;
            }
            finally
            {
                m_isRunning = false;
            }
            return success;
        }

        private void EnsureConstructedState()
        {
            // If there is no constructor to run, we're happy.
            if (m_constructor == null)
                return;

            // Make _perfectly_ sure the constructor won't be called twice - even though the
            // likelyhood of this is microscopically small.
            var constructor = m_constructor;
            m_constructor = null;

            constructor.Invoke(m_instance, null);
        }

        /// <summary>
        /// Adds a text line to the output of the grid program.
        /// This method will clip your string if it exceeds the maximum allowed length.
        /// </summary>
        /// <param name="line"></param>
        public void Echo(string line)
        {
            line = line ?? string.Empty;
            var lineLength = line.Length + 1; // line length + lineshift
            if (lineLength > MAX_ECHO_LENGTH)
            {
                // If the input line is already longer than the maximum allowed length,
                // we clear the current output and add only allowed portion of the string
                // to the output. Obviously this is unlikely to happen but it could.
                m_echoOutput.Clear();
                line = line.Substring(0, MAX_ECHO_LENGTH);
            }

            // Now we make sure the addition of this new line does not overshoot the 
            // maximum size by removing any excess amount of characters from the beginning
            // of the stream.
            var newLength = m_echoOutput.Length + lineLength;
            if (newLength > MAX_ECHO_LENGTH)
            {
                m_echoOutput.Remove(0, newLength - MAX_ECHO_LENGTH);
            }

            // Append the new line.
            m_echoOutput.Append(line);
            m_echoOutput.Append('\n');
        }
    }
}