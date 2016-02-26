using System.Xml.Serialization;
using ProtoBuf;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    public struct SerializableBlockOrientation
    {
        public static readonly SerializableBlockOrientation Identity = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);

        [ProtoMember, XmlAttribute]
        public Base6Directions.Direction Forward;

        [ProtoMember, XmlAttribute]
        public Base6Directions.Direction Up;

        public SerializableBlockOrientation(Base6Directions.Direction forward, Base6Directions.Direction up)
        {
            Forward = forward;
            Up = up;
        }

        public SerializableBlockOrientation(ref Quaternion q)
        {
            Forward = Base6Directions.GetForward(q);
            Up = Base6Directions.GetUp(q);
        }

        public static implicit operator MyBlockOrientation(SerializableBlockOrientation v)
        {
            if (Base6Directions.IsValidBlockOrientation(v.Forward, v.Up))
                return new MyBlockOrientation(v.Forward, v.Up);
            else if (v.Up == default(Base6Directions.Direction))
                return new MyBlockOrientation(v.Forward, Base6Directions.Direction.Up);
            else
                return MyBlockOrientation.Identity;
        }

        public static implicit operator SerializableBlockOrientation(MyBlockOrientation v)
        {
            return new SerializableBlockOrientation(v.Forward, v.Up);
        }

        public static bool operator ==(SerializableBlockOrientation a, SerializableBlockOrientation b)
        {
            return a.Forward == b.Forward && a.Up == b.Up;
        }

        public static bool operator !=(SerializableBlockOrientation a, SerializableBlockOrientation b)
        {
            return a.Forward != b.Forward || a.Up != b.Up;
        }


    }
}
