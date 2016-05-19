using ProtoBuf;
using System.Collections.Generic;
using VRage.Serialization;
using VRageMath;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    public struct MyEncounterId
    {
        [ProtoMember]
        public BoundingBoxD BoundingBox;
        [ProtoMember]
        public int Seed;
        [ProtoMember]
        public int EncounterId;

        public Vector3D PlacePosition;

        public MyEncounterId(BoundingBoxD box, int seed, int encounterId)
        {
            BoundingBox = box;
            Seed = seed;
            EncounterId = encounterId;
            PlacePosition = Vector3D.Zero;
        }

        public static bool operator ==(MyEncounterId x, MyEncounterId y)
        {
            return (x.BoundingBox == y.BoundingBox && x.Seed == y.Seed && x.EncounterId == y.EncounterId);
        }


        public static bool operator !=(MyEncounterId x, MyEncounterId y)
        {
            return !(x == y);
        }

        public override bool Equals(object o)
        {
            try
            {
                return (bool)(this == (MyEncounterId)o);
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return (int)((((uint)BoundingBox.GetHashCode()) << 16) ^ ((uint)Seed));
        }

    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Encounters : MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        public HashSet<MyEncounterId> SavedEcounters;

        [ProtoMember]
        public SerializableDictionary<MyEncounterId, Vector3D> MovedOnlyEncounters;
    }
}