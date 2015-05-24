namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Describes the current status of the connector.
    /// </summary>
    public enum ConnectorStatus
    {
        /// <summary>
        /// Not in range of another connector
        /// </summary>
        OutOfRange,
        /// <summary>
        /// In range but not connected
        /// </summary>
        ReadyToLock,
        /// <summary>
        /// Connected to OtherConnector
        /// </summary>
        Connected
    }
}
