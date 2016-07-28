#if !XB1 // XB1_NOPROTOBUF

namespace ProtoBuf
{
    /// <summary>
    /// Used to hold particulars relating to nested objects. This is opaque to the caller - simply
    /// give back the token you are given at the end of an object.
    /// </summary>
    public struct SubItemToken
    {
        internal readonly int value;
        internal SubItemToken(int value) {
            this.value = value;
        }
    }
}
#endif // !XB1
