using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AdvancedDoor : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember, DefaultValue(false)]
        public bool Open = false;
    }
}
