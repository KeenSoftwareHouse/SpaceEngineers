//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using VRage.Library.Collections;
//using VRage.Library.Utils;

//namespace VRage.Network
//{
//    public static class Extensions
//    {
//        public static void SetPacket(this VRage.Library.Collections.BitStream stream, Packet packet, bool skipMessageId = true)
//        {
//            stream.ResetRead(packet.Data, packet.BitLength, false);
//            if (skipMessageId)
//                stream.ReadMessageId();
//        }
        
//        public static MessageIDEnum ReadMessageId(this VRage.Library.Collections.BitStream stream)
//        {
//            return (MessageIDEnum)stream.ReadByte();
//        }

//        public static void WriteMessageId(this VRage.Library.Collections.BitStream stream, MessageIDEnum id)
//        {
//            stream.WriteByte((byte)id);
//        }

//        public static void ResetWrite(this VRage.Library.Collections.BitStream stream, MessageIDEnum id)
//        {
//            stream.ResetWrite();
//            stream.WriteMessageId(id);
//        }

//        internal static EndpointId ToEndpoint(this RakNetGUID guid)
//        {
//            return new EndpointId(guid.G);
//        }

//        internal static RakNetGUID ToGuid(this EndpointId endpointId)
//        {
//            return new RakNetGUID(endpointId.Value);
//        }

//        internal static bool IsReliable(this PacketReliabilityEnum reliability)
//        {
//            return reliability == PacketReliabilityEnum.RELIABLE
//                || reliability == PacketReliabilityEnum.RELIABLE_ORDERED
//                || reliability == PacketReliabilityEnum.RELIABLE_ORDERED_WITH_ACK_RECEIPT
//                || reliability == PacketReliabilityEnum.RELIABLE_SEQUENCED
//                || reliability == PacketReliabilityEnum.RELIABLE_WITH_ACK_RECEIPT;
//        }

//        internal static void SendReliableOrEnqueue(this MyPacketQueue queue, MyRakNetPeer peer, BitStream stream, float priority, PacketReliabilityEnum reliability, PacketPriorityEnum packetPriority, MyChannelEnum channel, RakNetGUID recipient, bool broadcast)
//        {
//            throw new NotImplementedException();
//            //if (reliability.IsReliable())
//            //{
//            //    peer.SendMessage(stream, packetPriority, reliability, channel, recipient, broadcast);
//            //}
//            //else
//            //{
//            //    queue.Enqueue(stream, priority, reliability, packetPriority, channel, recipient, broadcast);
//            //}
//        }
//    }
//}
