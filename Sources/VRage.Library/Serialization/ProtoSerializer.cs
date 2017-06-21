#if !XB1 // XB1_NOPROTOBUF
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace VRage.Serialization
{
    public class ProtoSerializer<T> : ISerializer<T>
    {
        public readonly RuntimeTypeModel Model;

        public static readonly ProtoSerializer<T> Default = new ProtoSerializer<T>();

        public ProtoSerializer(RuntimeTypeModel model = null)
        {
            Model = model ?? RuntimeTypeModel.Default;
        }

        public void Serialize(ByteStream destination, ref T data)
        {
            Model.Serialize(destination, data);
        }

        public void Deserialize(ByteStream source, out T data)
        {
            data = (T)Model.Deserialize(source, null, typeof(T));
        }
    }
}
#endif // !XB1
