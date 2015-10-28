using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Rpc
{
    public abstract class Serializer<T>
    {
        public abstract void Read(BitStream stream, out T value);
        public abstract void Write(BitStream stream, ref T value);
    }
}
