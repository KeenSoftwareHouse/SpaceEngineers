using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRageMath;

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
        public abstract void Serialize(BitStream stream, bool outOfOrder);

        public MyTimeSpan ClientTimeStamp;

        public long? SupportId { get; protected set; }

        private Vector3D m_position;

        public virtual Vector3D Position
        {
            get { return m_position; }
            protected set { m_position = value; }
        }

        public abstract void Update();

        public abstract IMyReplicable ControlledReplicable { get; }
    }
}
