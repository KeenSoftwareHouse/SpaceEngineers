namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Describes the current status of the piston.
    /// </summary>
    public enum PistonStatus
    {
        /// <summary>
        /// The piston velocity is 0 (stationary).
        /// </summary>
        Stopped,
        
        /// <summary>
        /// The piston is being extended (moving).
        /// </summary>
        Extending,

        /// <summary>
        /// The piston is in its extended position (stationary).
        /// </summary>
        Extended,

        /// <summary>
        /// The piston is being retracted (moving).
        /// </summary>
        Retracting,

        /// <summary>
        /// The piston is in its retracted position (stationary).
        /// </summary>
        Retracted
    }
}
