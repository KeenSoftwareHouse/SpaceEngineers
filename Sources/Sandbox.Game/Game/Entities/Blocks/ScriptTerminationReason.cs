namespace Sandbox.Game.Entities.Blocks
{
    /// <summary>
    /// Determines why (if at all) a script was terminated.
    /// </summary>
    public enum ScriptTerminationReason
    {
        /// <summary>
        /// The script was not terminated.
        /// </summary>
        None,

        /// <summary>
        /// There is no script (assembly) available.
        /// </summary>
        NoScript,

        /// <summary>
        /// No entry point (void Main(), void Main(string argument)) could be found.
        /// </summary>
        NoEntryPoint,

        /// <summary>
        /// The maximum allowed number of instructions has been reached.
        /// </summary>
        InstructionOverflow,

        /// <summary>
        /// The programmable block has changed ownership and must be rebuilt.
        /// </summary>
        OwnershipChange,

        /// <summary>
        /// A runtime exception happened during the execution of the script.
        /// </summary>
        RuntimeException,

        /// <summary>
        /// The script is already running (technically not a termination reason, but will be returned if a script tries to run itself in a nested fashion).
        /// </summary>
        AlreadyRunning
    }
}