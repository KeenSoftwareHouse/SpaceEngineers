using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public interface IMyServerCallback : IMyPeerCallback
    {
        void OnRequestServerData(EndpointId clientEndpoint, VRage.Library.Collections.BitStream outputStream);
        void OnRequestStateData(EndpointId clientEndpoint, VRage.Library.Collections.BitStream outputStream);
        void OnRequestWorld(EndpointId clientEndpoint, VRage.Library.Collections.BitStream outputStream);
        void OnClientReady(EndpointId clientEndpoint);

        /// <summary>
        /// Responsible for calling ValidationSuccessfull or ValidationUnsuccessfull
        /// </summary>
        void ValidateUser(EndpointId endpoint, VRage.Library.Collections.BitStream inputStream);

        /// <summary>
        /// Creates empty client state on server. It will be deserialized from data sent by client.
        /// This must be same type as client state sent by clients.
        /// </summary>
        MyClientStateBase CreateClientState();
    }
}
