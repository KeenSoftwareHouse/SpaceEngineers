#if !XB1 // XB1_NOPROTOBUF
#if FEAT_COMPILER
namespace ProtoBuf.Compiler
{
    internal delegate void ProtoSerializer(object value, ProtoWriter dest);
    internal delegate object ProtoDeserializer(object value, ProtoReader source);
}
#endif
#endif // !XB1
