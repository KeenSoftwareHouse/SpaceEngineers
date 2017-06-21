#if !XB1 // XB1_NOPROTOBUF
#if !NO_RUNTIME

namespace ProtoBuf.Serializers
{
    interface ISerializerProxy
    {
        IProtoSerializer Serializer { get; }
    }
}
#endif
#endif // !XB1
