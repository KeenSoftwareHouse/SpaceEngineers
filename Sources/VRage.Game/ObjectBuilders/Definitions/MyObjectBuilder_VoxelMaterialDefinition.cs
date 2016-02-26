using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Data;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [XmlType("VoxelMaterial")]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMaterialDefinition : MyObjectBuilder_DefinitionBase
    {
		[ProtoMember]
		public string MaterialTypeName = "Rock";

        [ProtoMember]
        public string MinedOre;
        
        [ProtoMember]
        public float MinedOreRatio;
        
        [ProtoMember]
        public bool CanBeHarvested;
        
        [ProtoMember]
        public bool IsRare;

        //[ProtoMember]
        //public bool IsIndestructible;

        [ProtoMember]
        public float DamageRatio;
        
        //[ProtoMember]
        //public string AssetName;
        
        [ProtoMember]
        public bool UseTwoTextures;
        
        [ProtoMember]
        public float SpecularPower;
        
        [ProtoMember]
        public float SpecularShininess;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string DiffuseXZ;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string NormalXZ;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string DiffuseY;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string NormalY;

        [ProtoMember]
        public int MinVersion;

        [ProtoMember]
        public bool SpawnsInAsteroids = true;

        [ProtoMember]
        public bool SpawnsFromMeteorites = true;

        [ProtoMember]
        public bool SpawnsFlora = false;

        [ProtoMember]
        [XmlArrayItem("Channel")]
        public int[] SpawnChannels;

        [ProtoMember, DefaultValue("")]
        public string ParticleEffect;

        // Voxel material to replace this if damaged.
        public string DamagedMaterial = null;

        // Damage Threshold
        public float DamageThreashold = 0.1f;
    }

}
