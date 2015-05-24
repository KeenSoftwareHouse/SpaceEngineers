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
        /// DEPRECATED use IsReadyToLock or for same functionallity IsReadyToLock || IsConnected
        /// Connector is working and in range of another connector, could be connected or ready to lock (Read Only)
        /// </summary>
        [Obsolete("Deprecated, Use IsReadyToLock instead")]
        bool IsLocked { get; }

        /// <summary>
        /// Connector is connected to another connector (Read Only)
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connector is in range and ready to lock with another connector, returns false if already connected. (Read Only)
        /// </summary>
        bool IsReadyToLock { get; }

        /// <summary>
        /// The other connector that this connector is connected too.
        /// </summary>
        IMyShipConnector OtherConnector { get; }
    }
}
