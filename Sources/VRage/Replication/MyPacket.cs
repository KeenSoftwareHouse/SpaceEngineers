using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;

namespace VRage
{
    public struct MyPacket
    {
        public byte[] Data;
        public int PayloadOffset;
        public int PayloadLength;
        public EndpointId Sender;
        public TimeSpan Timestamp;
    }
}
