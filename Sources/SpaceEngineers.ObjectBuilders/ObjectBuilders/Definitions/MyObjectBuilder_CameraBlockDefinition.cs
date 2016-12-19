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
        [ProtoMember]
        public float RequiredChargingInput = 0.001f;
        [ProtoMember, ModdableContentFile(".dds")]
        public string OverlayTexture;
        [ProtoMember]
        public float MinFov = 0.1f;
        [ProtoMember]
        public float MaxFov = 1.04719755f;
        [ProtoMember]
        public float RaycastConeLimit = 45f;
        [ProtoMember]
        public double RaycastDistanceLimit = -1;
        [ProtoMember]
        public float RaycastTimeMultiplier = 2.0f;
    }
}
