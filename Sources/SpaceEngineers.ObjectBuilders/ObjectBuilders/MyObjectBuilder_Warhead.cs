using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Warhead : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember]
        public int CountdownMs = 10000;

        [ProtoMember]
        public bool IsArmed = false;

        [ProtoMember]
        public bool IsCountingDown = false;
    }
}
