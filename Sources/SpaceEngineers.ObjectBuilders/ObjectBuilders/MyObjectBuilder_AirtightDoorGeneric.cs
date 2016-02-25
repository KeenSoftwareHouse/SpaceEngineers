using ProtoBuf;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AirtightDoorGeneric : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool Open = true;

        [ProtoMember]
        public float CurrOpening = 1f;
    }
}
