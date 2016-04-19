using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;
using VRage.Data;
using System.ComponentModel;
using System.Xml.Serialization;

namespace VRage.Game
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

        [ProtoMember]
        [ModdableContentFile("mwm")]
        [XmlArrayItem("Model")]
        public string[] Models = null;

        [ProtoMember, DefaultValue(null)]
        public string IconSymbol = null;
        public bool ShouldSerializeIconSymbol() { return IconSymbol != null; }

        [ProtoMember, DefaultValue(null)]
        public float? Volume = null; // in liters

        [ProtoMember, DefaultValue(null)]
        public float? ModelVolume = null; // in liters

        [ProtoMember]
        public string PhysicalMaterial;

        [ProtoMember]
        public string VoxelMaterial;

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

        [ProtoMember, DefaultValue(null)]
        public SerializableDefinitionId? DestroyedPieceId = null;
        
        [ProtoMember]
        public int DestroyedPieces = 0;

        [ProtoMember, DefaultValue(null)]
        public string ExtraInventoryTooltipLine = null;

        [ProtoMember]
        public MyFixedPoint MaxStackAmount = MyFixedPoint.MaxValue;
    }
}
