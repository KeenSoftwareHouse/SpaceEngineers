using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_LargeTurretBaseDefinition : MyObjectBuilder_WeaponBlockDefinition
    {
        [ProtoMember, ModdableContentFile(".dds")]
        public string OverlayTexture;
        [ProtoMember]
        public bool AiEnabled = true;
        [ProtoMember]
        public int MinElevationDegrees = -180;
        [ProtoMember]
        public int MaxElevationDegrees = 180;
        [ProtoMember]
        public int MinAzimuthDegrees = -180;
        [ProtoMember]
        public int MaxAzimuthDegrees = 180;
        [ProtoMember]
        public bool IdleRotation = true;
        [ProtoMember]
        public float MaxRangeMeters = 800.0f;
        [ProtoMember]
        public float RotationSpeed = 0.005f;
        [ProtoMember]
        public float ElevationSpeed = 0.005f; 
    }
}
