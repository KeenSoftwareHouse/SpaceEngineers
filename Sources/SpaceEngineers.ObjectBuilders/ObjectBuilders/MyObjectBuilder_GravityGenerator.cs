using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage;
using System.ComponentModel;
using VRage.Game;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_GravityGenerator : MyObjectBuilder_GravityGeneratorBase
    {
        //[ProtoMember, DefaultValue(true)]
        //public bool Enabled = true;

        [ProtoMember]
        public SerializableVector3 FieldSize = new SerializableVector3(150f, 150f, 150f);
    }
}
