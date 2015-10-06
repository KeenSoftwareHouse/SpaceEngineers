using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public interface IMyPeerCallback
    {
        /// <summary>
        /// Called when client is connected and validated.
        /// </summary>
        void OnClientJoined(EndpointId endpointId);

        /// <summary>
        /// Called when client leaves in any way.
        /// Clear per-client structure here.
        /// </summary>
        void OnClientLeft(EndpointId endpointId);

        /// <summary>
        /// Called when connection to client is lost.
        /// OnClientLeft is called right after this call returns.
        /// </summary>
        void OnConnectionLost(EndpointId endpointId);
    }
}
