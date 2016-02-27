using VRage.ObjectBuilders;
using ProtoBuf;
using System.ComponentModel;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MotorRotor : MyObjectBuilder_CubeBlock
    {
        // We cannot save attached block entity IDs because copy/paste wouldn't work that way
        //[ProtoMember, DefaultValue(0)]
        //public long StatorEntityId = 0;
        //public bool ShouldSerializeStatorEntityId() { return StatorEntityId != 0; }
    }
}
