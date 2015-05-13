using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    public struct MyEncounterId
    {
        [ProtoMember(1)]
        public BoundingBoxD BoundingBox;
        [ProtoMember(2)]
        public int Seed;
        [ProtoMember(2)]
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
    public class MyObjectBuilder_Encounters : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public HashSet<MyEncounterId> SavedEcounters;

        [ProtoMember(2)]
        public SerializableDictionary<MyEncounterId, Vector3D> MovedOnlyEncounters;
    }
}
