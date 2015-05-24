using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyShipConnector:IMyFunctionalBlock
    {
        /// <summary>
        /// Connector is set to throw out (Read Only)
        /// </summary>
        bool ThrowOut { get; }

        /// <summary>
        /// Connector is set to pull items from connected inventories (Read Only)
        /// </summary>
        bool CollectAll { get; }

        /// <summary>
        /// DEPRECATED use ConnectState
        /// Connector is working and in range of another connector, could be connected or ready to lock (Read Only)
        /// </summary>
        [Obsolete("Deprecated, Use ConnectState instead")]
        bool IsLocked { get; }

        /// <summary>
        /// DEPRECATED use ConnectState
        /// Connector is connected to another connector (Read Only)
        /// </summary>
        [Obsolete("Deprecated, Use ConnectState instead")]
        bool IsConnected { get; }

        /// <summary>
        /// Represents state of connection
        /// OutOfRange - Not in range of another connector
        /// ReadyToLock - In range but not connected
        /// Connected - Connected to OtherConnector
        /// i.e. if (myConnector.Status == ConnectorStatus.Connected) DoStuffWhenConnected();
        /// </summary>
        ConnectorStatus Status { get; }

        /// <summary>
        /// The other connector that this connector is connected too.
        /// </summary>
        IMyShipConnector OtherConnector { get; }
    }

}
