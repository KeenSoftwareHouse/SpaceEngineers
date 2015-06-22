using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Library.Utils;
using VRage.Utils;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalGunObject : MyObjectBuilder_PhysicalObject
    {
        [ProtoMember]
        [XmlElement("GunEntity", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EntityBase>))]
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
