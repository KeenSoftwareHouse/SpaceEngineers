using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_LandingGearDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public string LockSound;

        [ProtoMember]
        public string UnlockSound;

        [ProtoMember]
        public string FailedAttachSound;
    }
}
