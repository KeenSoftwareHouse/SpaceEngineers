using System.Xml.Serialization;
using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Data;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlType("VR.PhysicalModelDefinition")]
    public class MyObjectBuilder_PhysicalModelDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model;

        [ProtoMember]
        public string PhysicalMaterial;

        [ProtoMember]
        public float Mass = 0;
    }
}
