using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [XmlType("VoxelMaterial")]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMaterialDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(2)]
        public string MinedOre;
        
        [ProtoMember(3)]
        public float MinedOreRatio;
        
        [ProtoMember(4)]
        public bool CanBeHarvested;
        
        [ProtoMember(5)]
        public bool IsRare;

        //[ProtoMember(6)]
        //public bool IsIndestructible;

        [ProtoMember(7)]
        public float DamageRatio;
        
        //[ProtoMember(8)]
        //public string AssetName;
        
        [ProtoMember(9)]
        public bool UseTwoTextures;
        
        [ProtoMember(10)]
        public float SpecularPower;
        
        [ProtoMember(11)]
        public float SpecularShininess;

        [ProtoMember(12)]
        [ModdableContentFile("dds")]
        public string DiffuseXZ;

        [ProtoMember(13)]
        [ModdableContentFile("dds")]
        public string NormalXZ;

        [ProtoMember(14)]
        [ModdableContentFile("dds")]
        public string DiffuseY;

        [ProtoMember(15)]
        [ModdableContentFile("dds")]
        public string NormalY;

        [ProtoMember(16)]
        public int MinVersion;
    }
}
