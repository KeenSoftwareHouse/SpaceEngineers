﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Data;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [XmlType("VoxelMaterial")]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMaterialDefinition : MyObjectBuilder_DefinitionBase
    {
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

        [ProtoMember(17), DefaultValue(true)]
        public bool SpawnsInAsteroids = true;

        [ProtoMember(18), DefaultValue(true)]
        public bool SpawnsFromMeteorites = true;
    }
}
