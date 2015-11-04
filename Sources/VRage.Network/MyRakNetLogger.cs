//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace VRage.Network
//{
//    public delegate void SendDelegate(MyRakNetPeer peer, IntPtr data, int dataLen, PacketPriorityEnum priority, PacketReliabilityEnum realiability, MyChannelEnum channel, RakNetGUID recipient, bool broadcast);
//    public delegate void ReceiveDelegate(MyRakNetPeer peer, Packet packet, bool ignored);

//    public static class MyRakNetLogger
//    {
//        public static event SendDelegate PacketSent;
//        public static event ReceiveDelegate PacketReceived;

//        public static void OnSend(MyRakNetPeer peer, IntPtr data, int length, PacketPriorityEnum priority, PacketReliabilityEnum reliability, MyChannelEnum channel, RakNetGUID recipient, bool broadcast)
//        {
//            var handler = PacketSent;
//            if (handler != null)
//            {
//                handler(peer, data, length, priority, reliability, channel, recipient, broadcast);
//            }
//        }

//        public static void OnReceive(MyRakNetPeer peer, Packet packet, bool ignored)
//        {
//            var handler = PacketReceived;
//            if (handler != null)
//            {
//                handler(peer, packet, ignored);
//            }
//        }
//    }
//}
