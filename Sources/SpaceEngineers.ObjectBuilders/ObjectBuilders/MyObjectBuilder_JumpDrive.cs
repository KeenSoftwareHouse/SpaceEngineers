using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_JumpDrive : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float StoredPower;
        
        [ProtoMember]
        public int? JumpTarget;
        
        [ProtoMember]
        public float JumpRatio = 100.0f;
        
        [ProtoMember]
        public bool Recharging = true;
    }
}
