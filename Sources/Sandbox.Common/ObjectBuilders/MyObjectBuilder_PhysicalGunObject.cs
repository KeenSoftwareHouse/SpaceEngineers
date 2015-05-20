using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Diagnostics;
using System.Xml.Serialization;
using VRage.Library.Utils;
using VRage.Utils;

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

        public override bool CanStack(MyObjectBuilderType type, MyStringId subtypeId, MyItemFlags flags)
        {
            return false; // weapons shouldn't stack
        }

    }
}
