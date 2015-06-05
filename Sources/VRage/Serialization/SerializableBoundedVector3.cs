using ProtoBuf;
using VRageMath;

namespace VRage
{
    [ProtoContract]
    public struct SerializableBoundedVector3 // TODO: Maybe templatize. Dunno if ProtoBuf would like that, though.
    {
        [ProtoMember]
        public SerializableVector3 Min;

        [ProtoMember]
        public SerializableVector3 Max;

        [ProtoMember]
        public SerializableVector3 Default;

        public SerializableBoundedVector3(SerializableVector3 min, SerializableVector3 max, SerializableVector3 def)
        {
            Min = min;
            Max = max;
            Default = def;
        }

        public static implicit operator MyBoundedVector3(SerializableBoundedVector3 v)
        {
            return new MyBoundedVector3(v.Min, v.Max, v.Default);
        }

        public static implicit operator SerializableBoundedVector3(MyBoundedVector3 v)
        {
            return new SerializableBoundedVector3(v.Min, v.Max, v.Default);
        }

    }
}
