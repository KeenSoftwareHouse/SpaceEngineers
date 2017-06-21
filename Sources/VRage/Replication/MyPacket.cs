using VRage.Library.Utils;
using VRage.Network;

namespace VRage
{
    public struct MyPacket
    {
        public byte[] Data;
        public int PayloadOffset;
        public int PayloadLength;
        public EndpointId Sender;
        public MyTimeSpan Timestamp;
        public MyTimeSpan ReceivedTime;
    }
}
