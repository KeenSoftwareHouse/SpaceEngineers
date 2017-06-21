using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Network;

namespace System
{
    public static class Extensions
    {
        [ThreadStatic]
        static List<IMyStateGroup> m_tmpStateGroupsPerThread;

        static List<IMyStateGroup> m_tmpStateGroups
        {
            get
            {
                if (m_tmpStateGroupsPerThread == null)
                    m_tmpStateGroupsPerThread = new List<IMyStateGroup>();
                return m_tmpStateGroupsPerThread;
            }
        }

        public static void ResetRead(this BitStream stream, MyPacket packet, bool copy = true)
        {
            stream.ResetRead(packet.Data, packet.PayloadOffset, packet.PayloadLength * 8, copy);
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

        /// <summary>
        /// Finds state group of specified type.
        /// Returns null when group of specified type not found.
        /// </summary>
        public static T FindStateGroup<T>(this IMyReplicable obj) 
            where T : class, IMyStateGroup
        {
            try
            {
                if (obj == null)
                    return null;

                obj.GetStateGroups(m_tmpStateGroups);
                foreach (var item in m_tmpStateGroups)
                {
                    var group = item as T;
                    if (group != null)
                        return group;
                }
                return null;
            }
            finally
            {
                m_tmpStateGroups.Clear();
            }
        }
    }
}
