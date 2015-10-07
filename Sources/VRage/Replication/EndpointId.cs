using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Id of network endpoint, opaque struct, internal value should not be accessed outside VRage.Network.
    /// EndpointId is not guid and can change when client reconnects to server.
    /// Internally it's SteamId or RakNetGUID.
    /// </summary>
    public struct EndpointId
    {
        public ulong Value;

        public bool IsNull
        {
            get { return Value == 0; }
        }

        public bool IsValid
        {
            get { return !IsNull; }
        }

        public EndpointId(ulong value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(EndpointId a, EndpointId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(EndpointId a, EndpointId b)
        {
            return a.Value != b.Value;
        }

        public bool Equals(EndpointId other)
        {
            return this.Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj is EndpointId)
                return this.Equals((EndpointId)obj);
            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
