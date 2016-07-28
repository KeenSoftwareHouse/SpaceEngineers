using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    /// <summary>
    /// Base class for game-defined client state.
    /// It's set of data required by server, sent from client.
    /// E.g. current client area of interest, context (game, terminal, inventory etc...)
    /// Abstract class for performance reasons (often casting)
    /// </summary>
    public abstract class MyClientStateBase
    {
        /// <summary>
        /// Client endpoint, don't serialize it in Serialize()
        /// </summary>
        public EndpointId EndpointId { get; internal set; }

        /// <summary>
        /// Serializes state into/from bit stream.
        /// EndpointId should be ignored.
        /// </summary>
        public abstract void Serialize(BitStream stream,uint timeStamp);

        public uint ClientTimeStamp;

        public long? SupportId { get; protected set; }
    }
}
