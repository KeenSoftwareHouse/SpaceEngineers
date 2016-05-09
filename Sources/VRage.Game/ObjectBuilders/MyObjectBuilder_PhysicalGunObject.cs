using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Utils;
using System.Xml.Serialization;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalGunObject : MyObjectBuilder_PhysicalObject
    {
        [ProtoMember]
        [XmlElement("GunEntity", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EntityBase>))]
        [Serialize(MyObjectFlags.Dynamic | MyObjectFlags.Nullable, DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))]
        public MyObjectBuilder_EntityBase GunEntity;

        public MyObjectBuilder_PhysicalGunObject() : this(null) { }

        public MyObjectBuilder_PhysicalGunObject(MyObjectBuilder_EntityBase gunEntity)
        {
            GunEntity = gunEntity;
        }

        public override bool CanStack(MyObjectBuilderType type, MyStringHash subtypeId, MyItemFlags flags)
        {
            return false; // weapons shouldn't stack
        }
    }
}
