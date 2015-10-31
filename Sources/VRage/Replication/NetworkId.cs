using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Network object identifier. Similar to entity id, but on network.
    /// Also one entity can have multiple NetworkIds, one main, one for physics sync, one for terminal sync and more.
    /// Opaque struct, it should not be necessary to internal member outside VRage.Network.
    /// NetworkId is not persistent and changes with server restart, never store it in persistent storage (saves).
    /// Internally takes advantage of small numbers.
    /// </summary>
    public struct NetworkId : IComparable<NetworkId>, IEquatable<NetworkId>
    {
        public static readonly NetworkId Invalid = new NetworkId(0);

        internal uint Value;

        public bool IsInvalid { get { return Value == 0; } }
        public bool IsValid { get { return Value != 0; } }

        internal NetworkId(uint value)
        {
            Value = value;
        }

        public int CompareTo(NetworkId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(NetworkId other)
        {
            return Value == other.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
