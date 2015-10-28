using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public interface IMyClientCallback : IMyPeerCallback
    {
        void OnConnectionAttemptFailed();
        void OnConnectionBanned();
        void OnInvalidPassword();
        void OnServerDataReceived(VRage.Library.Collections.BitStream incomingStream);
        void OnAlreadyConnected();
        void OnStateDataDownloadProgress(uint progress, uint total, uint partLength);
        void OnDisconnectionNotification();
        void OnLocalClientReady();
        void OnSendClientData(VRage.Library.Collections.BitStream outputStream);
        void OnWorldReceived(VRage.Library.Collections.BitStream incomingStream);
    }
}
