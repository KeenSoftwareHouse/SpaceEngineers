using VRage.Library.Collections;
using VRage.Library.Utils;

namespace VRage.Network
{
    public interface IMyStateGroup : IMyNetObject
    {
        StateGroupEnum GroupType { get; }

        /// <summary>
        /// Called on server new clients starts replicating this group.
        /// </summary>
        void CreateClientData(MyClientStateBase forClient);

        /// <summary>
        /// Called on server when client stops replicating this group.
        /// </summary>
        void DestroyClientData(MyClientStateBase forClient);

        /// <summary>
        /// Update method called on client.
        /// </summary>
        void ClientUpdate(MyTimeSpan clientTimestamp);

        /// <summary>
        /// Called when state group is being destroyed.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Gets priority related to client.
        /// When priority is lower than zero, it means the object is not relevant for client.
        /// Default priority is 1.0f.
        /// </summary>
        /// <param name="frameCountWithoutSync">How long (in update frame count) has client not received sync of this state group.</param>
        /// <param name="forClient">Client for whom is the priority get.</param>
        float GetGroupPriority(int frameCountWithoutSync, MyClientInfo forClient);

        /// <summary>
        /// (De)serializes group state or it's diff for client.
        /// When writing, you can write beyond maxBitPosition, but message won't be sent and ACKs won't be received for it.
        /// ReplicationServer will detect, that state group written beyond packet size and revert it.
        /// When nothing written, ReplicationServer will detect that and state group won't receive ACK for that packet id.
        /// </summary>
        /// <param name="stream">Stream to write to or read from.</param>
        /// <param name="forClient">When writing the client which will receive the data. When reading, it's null.</param>
        /// <param name="packetId">Id of packet in which the data will be sent or from which the data is received.</param>
        /// <param name="maxBitPosition">Maximum position in bit stream where you can write data, it's inclusive.</param>
        void Serialize(BitStream stream, EndpointId forClient, MyTimeSpan timestamp, byte packetId, int maxBitPosition);

        /// <summary>
        /// Called for each packet id sent to client from this state group.
        /// When ACK received, called immediatelly.
        /// When several other packets received from client, but some were missing, called for each missing packet.
        /// </summary>
        /// <param name="forClient">The client.</param>
        /// <param name="packetId">Id of the delivered or lost packet.</param>
        /// <param name="delivered">True when packet was delivered, false when packet is considered lost.</param>
        void OnAck(MyClientStateBase forClient, byte packetId, bool delivered);

        void ForceSend(MyClientStateBase clientData);
        void TimestampReset(MyTimeSpan timestamp);

        bool IsStillDirty(EndpointId forClient);

        IMyReplicable Owner { get; }
    }
}
