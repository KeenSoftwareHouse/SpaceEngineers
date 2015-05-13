using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage;

namespace VRage.Serialization
{
    public interface ISerializer<T>
    {
        void Serialize(ByteStream destination, ref T data);
        void Deserialize(ByteStream source, out T data);
    }
}
