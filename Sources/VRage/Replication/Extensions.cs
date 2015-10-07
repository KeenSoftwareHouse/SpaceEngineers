using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Library.Collections;
using VRage.Library.Utils;

namespace VRage.Network
{
    public static class Extensions
    {
        public static void ResetRead(this BitStream stream, MyPacket packet)
        {
            stream.ResetRead(packet.Data, packet.PayloadOffset, packet.PayloadLength * 8);
        }

        public static NetworkId ReadNetworkId(this VRage.Library.Collections.BitStream stream)
        {
            return new NetworkId(stream.ReadUInt32Variant());
        }

        public static TypeId ReadTypeId(this VRage.Library.Collections.BitStream stream)
        {
            return new TypeId(stream.ReadUInt32Variant());
        }

        public static void WriteNetworkId(this VRage.Library.Collections.BitStream stream, NetworkId networkId)
        {
            stream.WriteVariant((uint)networkId.Value);
        }

        public static void WriteTypeId(this VRage.Library.Collections.BitStream stream, TypeId typeId)
        {
            stream.WriteVariant((uint)typeId.Value);
        }
        
        public static bool IsRelevant(this IMyReplicable obj, MyClientStateBase state)
        {
            return obj.GetPriority(state) > 0;
        }

        public static bool IsRelevant(this IMyReplicable obj, MyClientStateBase state, out float priority)
        {
            priority = obj.GetPriority(state);
            return priority > 0;
        }
    }
}
