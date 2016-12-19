using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Engine.Networking;
using SteamSDK;
using VRage.Serialization;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Engine.Utils;
using System.IO;
using VRage.Library.Collections;
using VRage.Network;
using VRage;
using Sandbox.Game.Replication;
using VRage.Library.Utils;

namespace Sandbox.Game.Multiplayer
{
    // Bridge to new multiplayer, will be removed eventually
    public partial class MySyncLayer
    {
        class Sender : IBitSerializable
        {
            public ByteStream SendStream = new ByteStream(64 * 1024, true);
            public ByteStream ReceiveStream = new ByteStream(64 * 1024, true);

            public bool Serialize(BitStream stream, bool validate)
            {
                if (stream.Writing)
                {
                    stream.WriteBytes(SendStream.Data, 0, (int)SendStream.Position);
                }
                else
                {
                    int size = stream.ByteLength - stream.BytePosition;
                    ReceiveStream.EnsureCapacity(size);
                    stream.ReadBytes(ReceiveStream.Data, 0, size);
                    ReceiveStream.Position = size;
                }
                return true;
            }

            public static implicit operator BitReaderWriter(Sender sync)
            {
                return new BitReaderWriter(sync);
            }
        }

        List<ulong> m_emptyRecipients = new List<ulong>();
        Sender m_sender = new Sender();

        /// <summary>
        /// Find replicable and write data into m_sender
        /// </summary>
        private MyExternalReplicable PrepareSend<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType)
            where TMsg : struct, IEntityMessage
        {
            long entityId = msg.GetEntityId();
            var entity = MyEntities.GetEntityById(entityId);
            Debug.Assert(entity is IMyEventProxy, "To use SendAsRpc, entity must be IMyEventProxy and must have replicable in replication layer");

            var id = TransportLayer.GetId<TMsg>(messageType);
            var callback = TransportLayer.GetCallback<TMsg>(messageType);
            m_sender.SendStream.Position = 0;
            m_sender.SendStream.WriteUShort(id.Item1);
            callback.Write(m_sender.SendStream, ref msg);
            var replicable = MyMultiplayer.Static.ReplicationLayer.GetProxyTarget((IMyEventProxy)entity) as MyExternalReplicable;
            Debug.Assert(replicable != null, "No replicable found for entity");
            Debug.Assert(replicable is IMyProxyTarget, "Replicable must be proxy target");
            return replicable;
        }

        /// <summary>
        /// Sends always to server (even server to self).
        /// </summary>
        public void SendAsRpcToServer<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct, IEntityMessage
        {
            if (Sync.IsServer)
            {
                // Sending to self
                Sync.Layer.TransportLayer.SendMessage(ref msg, m_emptyRecipients, messageType, true);
            }
            else
            {
                var replicable = PrepareSend<TMsg>(ref msg, messageType);
                if (replicable != null)
                {
                    MyMultiplayer.RaiseEvent(replicable, x => x.RpcToServer_Implementation, (BitReaderWriter)m_sender);
                }
            }
        }

        public void SendAsRpcToServerAndSelf<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct, IEntityMessage
        {
            // Send to self
            Sync.Layer.TransportLayer.SendMessage(ref msg, m_emptyRecipients, messageType, true);

            // When I'm not server, send also to server
            if (!Sync.IsServer)
            {
                var replicable = PrepareSend<TMsg>(ref msg, messageType);
                if (replicable != null)
                {
                    MyMultiplayer.RaiseEvent(replicable, x => x.RpcToServer_Implementation, (BitReaderWriter)m_sender);
                }
            }
        }

        public void SendAsRpcToAll<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct, IEntityMessage
        {
            Debug.Assert(Sync.IsServer, "Must be server to send message to all");
            if (MyMultiplayer.Static != null)
            {
                var replicable = PrepareSend<TMsg>(ref msg, messageType);
                if (replicable != null)
                {
                    MyMultiplayer.RaiseEvent(replicable, x => x.RpcToAll_Implementation, (BitReaderWriter)m_sender);
                }
            }
        }

        public void SendAsRpcToAllButOne<TMsg>(ref TMsg msg, ulong dontSentTo, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct, IEntityMessage
        {
            Debug.Assert(Sync.IsServer, "Must be server to send message to all");
            if (MyMultiplayer.Static != null)
            {
                var replicable = PrepareSend<TMsg>(ref msg, messageType);
                if (replicable != null)
                {
                    MyMultiplayer.RaiseEvent(replicable, x => x.RpcToAllButOne_Implementation, (BitReaderWriter)m_sender, new EndpointId(dontSentTo));
                }
            }
        }

        public void SendAsRpcToAllAndSelf<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct, IEntityMessage
        {
            Debug.Assert(Sync.IsServer, "Must be server to send message to all");
            // Sending to self
            Sync.Layer.TransportLayer.SendMessage(ref msg, m_emptyRecipients, messageType, true);
            // Send to all
            SendAsRpcToAll(ref msg, messageType);
        }

        public void ProcessRpc(BitReaderWriter reader)
        {
            reader.ReadData(m_sender, false);
            MyPacket packet;
            packet.Data = m_sender.ReceiveStream.Data;
            packet.Sender = MyEventContext.Current.Sender;
            if (packet.Sender.IsNull)
                packet.Sender = new EndpointId(Sync.MyId);
            packet.Timestamp = MyTimeSpan.Zero;
            packet.PayloadOffset = 0;
            packet.PayloadLength = (int)m_sender.ReceiveStream.Position;
            packet.ReceivedTime = MyTimeSpan.FromTicks(Stopwatch.GetTimestamp());
            TransportLayer.HandleOldGameEvent(packet);
        }
    }
}
