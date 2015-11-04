using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using VRage.Data;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalItemDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public Vector3 Size; // in meters

        [ProtoMember]
        public float Mass; // in Kg

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model = @"Models\Components\Sphere.mwm";

        [ProtoMember, DefaultValue(null)]
        public string IconSymbol = null;
        public bool ShouldSerializeIconSymbol() { return IconSymbol != null; }

        [ProtoMember, DefaultValue(null)]
        public float? Volume = null; // in liters

        [ProtoMember]
        public string PhysicalMaterial;

        [ProtoMember, DefaultValue(true)]
        public bool CanSpawnFromScreen = true;

        // Adding these members to allow chaning the default orientation of the model on spawn
        [ProtoMember]
        public bool RotateOnSpawnX = false;

        [ProtoMember]
        public bool RotateOnSpawnY = false;

        [ProtoMember]
        public bool RotateOnSpawnZ = false;

        [ProtoMember]
        public int Health = 100;
    }
}
