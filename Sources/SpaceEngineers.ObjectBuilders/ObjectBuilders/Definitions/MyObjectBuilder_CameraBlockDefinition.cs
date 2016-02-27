using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Data;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_CameraBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;
        [ProtoMember]
        public float RequiredPowerInput;
        [ProtoMember, ModdableContentFile(".dds")]
        public string OverlayTexture;
        [ProtoMember]
        public float MinFov = 0.1f;
        [ProtoMember]
        public float MaxFov = 1.04719755f;
    }
}
