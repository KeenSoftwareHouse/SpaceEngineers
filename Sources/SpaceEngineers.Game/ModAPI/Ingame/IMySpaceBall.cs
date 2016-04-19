namespace SpaceEngineers.Game.ModAPI.Ingame
{
    /// <summary>
    /// Spaceball interface
    /// </summary>
    public interface IMySpaceBall : IMyVirtualMass
    {
        /// <summary>
        /// Ball friction
        /// </summary>
        float Friction { get; }

        /// <summary>
        /// Ball restitution
        /// </summary>
        float Restitution { get; }

        /// <summary>
        /// Is broadcasting
        /// </summary>
        bool IsBroadcasting { get; }
    }
}
