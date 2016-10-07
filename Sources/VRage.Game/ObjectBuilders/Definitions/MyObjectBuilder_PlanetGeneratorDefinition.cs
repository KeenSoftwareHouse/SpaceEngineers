using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace VRage.Game
{

    #region Material Data
    [ProtoContract]
    public struct MyPlanetMaterialLayer
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Material")]
        public string Material;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Depth")]
        public float Depth;
    }

    [ProtoContract]
    public class MyPlanetMaterialDefinition : ICloneable
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Material")]
        public string Material;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Value")]
        public byte Value = 0;

        [ProtoMember]
        [XmlAttribute(AttributeName = "MaxDepth")]
        public float MaxDepth = 1.0f;

        [ProtoMember]
        [XmlArrayItem("Layer")]
        public MyPlanetMaterialLayer[] Layers;

        public virtual bool IsRule { get { return false; } }

        /**
         * Weather this material has layers.
         */
        public bool HasLayers
        {
            get { return Layers != null && Layers.Length > 0; }
        }

        public string FirstOrDefault
        {
            get
            {
                if (Material != null) return Material;
                else if (HasLayers) return Layers[0].Material;
                else return null;
            }
        }

        public object Clone()
        {
            MyPlanetMaterialDefinition clone = new MyPlanetMaterialDefinition();
            clone.Material = Material;
            clone.Value = Value;
            clone.MaxDepth = MaxDepth;
            if (Layers != null)
                clone.Layers = Layers.Clone() as MyPlanetMaterialLayer[];
            else
                clone.Layers = null;
            return clone;
        }
    }

    /**
     * Important!
     * 
     * Due to the geometry in question the slope is stored as the cosine (used in dot product).
     * 
     * Meanwhile the dot product for the latitude yields the cosine of the modulus of the compliment of our angle.
     * This means after the maths are done that what we have is the *sine*, so the latitude is stored as the sine.
     */
    [ProtoContract]
    public class MyPlanetMaterialPlacementRule : MyPlanetMaterialDefinition, ICloneable
    {
        [ProtoMember]
        public SerializableRange Height = new SerializableRange(0, 1);

        [ProtoMember]
        public SymetricSerializableRange Latitude = new SymetricSerializableRange(-90, 90);

        [ProtoMember]
        public SerializableRange Longitude = new SerializableRange(-180, 180);

        [ProtoMember]
        public SerializableRange Slope = new SerializableRange(0, 90);

        public override bool IsRule { get { return true; } }

        public MyPlanetMaterialPlacementRule()
        {
        }

        public MyPlanetMaterialPlacementRule(MyPlanetMaterialPlacementRule copyFrom)
        {
            // Rule data
            Height = copyFrom.Height;
            Latitude = copyFrom.Latitude;
            Longitude = copyFrom.Longitude;
            Slope = copyFrom.Slope;

            // Material data
            Material = copyFrom.Material;
            Value = copyFrom.Value;
            MaxDepth = copyFrom.MaxDepth;
            Layers = copyFrom.Layers;
        }

        /**
         * Check that a rule matches terrain properties.
         * 
         * @param height Height ration to the height map.
         * @param latitude Latitude cosine
         * @param slope Surface dominant angle sine.
         */
        public bool Check(float height, float latitude, float slope)
        {
            return Height.ValueBetween(height) && Latitude.ValueBetween(latitude)
                   && Slope.ValueBetween(slope);
        }

        public object Clone()
        {
            MyPlanetMaterialPlacementRule clonedRule = new MyPlanetMaterialPlacementRule(this);
            return clonedRule;
        }
    }

    [ProtoContract]
    public class MyPlanetSurfaceRule : ICloneable
    {
        [ProtoMember]
        public SerializableRange Height = new SerializableRange(0, 1);

        [ProtoMember]
        public SymetricSerializableRange Latitude = new SymetricSerializableRange(-90, 90);

        [ProtoMember]
        public SerializableRange Longitude = new SerializableRange(-180, 180);

        [ProtoMember]
        public SerializableRange Slope = new SerializableRange(0, 90);

        /**
         * Check that a rule matches terrain properties.
         * 
         * @param height Height ration to the height map.
         * @param latitude Latitude cosine
         * @param slope Surface dominant angle cosine.
         */
        public bool Check(float height, float latitude, float longitude, float slope)
        {
            return Height.ValueBetween(height) && Latitude.ValueBetween(latitude) && Longitude.ValueBetween(longitude)
                         && Slope.ValueBetween(slope);
        }

        public object Clone()
        {
            MyPlanetSurfaceRule clonedRule = new MyPlanetSurfaceRule();
            clonedRule.Height = Height;
            clonedRule.Latitude = Latitude;
            clonedRule.Longitude = Longitude;
            clonedRule.Slope = Slope;
            return clonedRule;
        }
    }

    /**
     * Rule group defines a material mappable set of surface rules.
     */
    [XmlType("MaterialGroup")]
    [ProtoContract]
    public class MyPlanetMaterialGroup : ICloneable
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Value")]
        public byte Value = 0;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Name")]
        public string Name = "Default";

        [ProtoMember]
        [XmlElement("Rule")]
        public MyPlanetMaterialPlacementRule[] MaterialRules;

        public object Clone()
        {
            MyPlanetMaterialGroup clonedGroup = new MyPlanetMaterialGroup();
            clonedGroup.Value = Value;
            clonedGroup.Name = Name;
            clonedGroup.MaterialRules = new MyPlanetMaterialPlacementRule[MaterialRules.Length];
            for (int i = 0; i < MaterialRules.Length; i++)
                clonedGroup.MaterialRules[i] = MaterialRules[i].Clone() as MyPlanetMaterialPlacementRule;
            return clonedGroup;
        }
    }

    [ProtoContract]
    public class MyPlanetOreMapping
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Value")]
        public byte Value;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Type")]
        public string Type;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Start")]
        public float Start = 5.0f;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Depth")]
        public float Depth = 10.0f;
    }

    #endregion

    [ProtoContract]
    public class MyPlanetAnimal
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Type")]
        public string AnimalType;
    }

    [ProtoContract]
    public class MyPlanetAnimalSpawnInfo
    {
        [XmlArrayItem("Animal")]
        public MyPlanetAnimal[] Animals;

        [ProtoMember]
        public int SpawnDelayMin = 30 * 1000;

        [ProtoMember]
        public int SpawnDelayMax = 60 * 1000;

        [ProtoMember]
        public float SpawnDistMin = 10.0f;

        [ProtoMember]
        public float SpawnDistMax = 140.0f;

        [ProtoMember]
        public int KillDelay = 120000;

        [ProtoMember]
        public int WaveCountMin = 1;

        [ProtoMember]
        public int WaveCountMax = 5;
    }

    [ProtoContract]
    public class MyPlanetSurfaceDetail
    {
        [ProtoMember]
        public string Texture;

        [ProtoMember]
        public float Size;

        [ProtoMember]
        public float Scale;

        [ProtoMember]
        public SerializableRange Slope;

        [ProtoMember]
        public float Transition;
    }

    [ProtoContract]
    public class MyPlanetDistortionDefinition
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Type")]
        public string Type;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Value")]
        public byte Value = 0;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Frequency")]
        public float Frequency = 1.0f;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Height")]
        public float Height = 1.0f;

        [ProtoMember]
        [XmlAttribute(AttributeName = "LayerCount")]
        public int LayerCount = 1;

    }

    [ProtoContract]
    public struct MyPlanetMaps
    {
        [ProtoMember]
        [XmlAttribute]
        public bool Material;

        [ProtoMember]
        [XmlAttribute]
        public bool Ores;

        [ProtoMember]
        [XmlAttribute]
        public bool Biome;

        [ProtoMember]
        [XmlAttribute]
        public bool Occlusion;
    }

    [ProtoContract]
    public class MySerializablePlanetEnvironmentalSoundRule
    {
        [ProtoMember]
        public SerializableRange Height = new SerializableRange(0, 1);

        [ProtoMember]
        public SymetricSerializableRange Latitude = new SymetricSerializableRange(-90, 90);

        [ProtoMember]
        public SerializableRange SunAngleFromZenith = new SerializableRange(0, 180);

        [ProtoMember]
        public string EnvironmentSound;
    }

    [ProtoContract]
    public class MyMusicCategory
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Category")]
        public string Category;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Frequency")]
        public float Frequency = 1.0f;
    }

    #region Environment Item Data

    [ProtoContract]
    public class MyPlanetEnvironmentItemDef
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "TypeId")]
        public string TypeId;

        [ProtoMember]
        [XmlAttribute(AttributeName = "SubtypeId")]
        public string SubtypeId;

        [ProtoMember]
        [XmlAttribute(AttributeName = "GroupId")]
        public string GroupId = null;

        [ProtoMember]
        [XmlAttribute(AttributeName = "ModifierId")]
        public string ModifierId = null;

        [ProtoMember]
        public int GroupIndex = -1;

        [ProtoMember]
        public int ModifierIndex = -1;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Density")]
        public float Density;

        [ProtoMember]
        [XmlAttribute(AttributeName = "IsDetail")]
        public bool IsDetail = false;

        [ProtoMember]
        public Vector3 BaseColor = Vector3.Zero;

        [ProtoMember]
        public Vector2 ColorSpread = Vector2.Zero;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Offset")]
        public float Offset = 0f;

        [ProtoMember]
        [XmlAttribute(AttributeName = "MaxRoll")]
        public float MaxRoll = 0f;
    }

    [ProtoContract]
    public struct PlanetEnvironmentItemMapping
    {
        [ProtoMember]
        [XmlArrayItem("Material")]
        public string[] Materials;

        [ProtoMember]
        [XmlArrayItem("Biome")]
        public int[] Biomes;

        [ProtoMember]
        [XmlArrayItem("Item")]
        public MyPlanetEnvironmentItemDef[] Items;

        [ProtoMember]
        public MyPlanetSurfaceRule Rule;
    }

    #endregion

    [ProtoContract]
    public class MyAtmosphereColorShift
    {
        [ProtoMember]
        public SerializableRange R = new SerializableRange();

        [ProtoMember]
        public SerializableRange G = new SerializableRange();

        [ProtoMember]
        public SerializableRange B = new SerializableRange();
    }

    [ProtoContract]
    public struct MyPlanetMaterialBlendSettings
    {
        [ProtoMember]
        public string Texture;

        [ProtoMember]
        public int CellSize;
    }

    [ProtoContract]
    public class MyPlanetAtmosphere
    {
        [ProtoMember]
        [XmlElement]
        public bool Breathable = false;

        [ProtoMember]
        [XmlElement]
        public float OxygenDensity = 1f;

        [ProtoMember]
        [XmlElement]
        public float Density = 1f;

        [ProtoMember]
        [XmlElement]
        public float LimitAltitude = 2f;
    }


    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlType("PlanetGeneratorDefinition")]
    public class MyObjectBuilder_PlanetGeneratorDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public MyPlanetMaps? PlanetMaps;

        [ProtoMember]
        public bool? HasAtmosphere;

        [ProtoMember]
        [XmlArrayItem("CloudLayer")]
        public List<MyCloudLayerSettings> CloudLayers = null;

        [ProtoMember]
        public SerializableRange? HillParams;

        [ProtoMember]
        public float? GravityFalloffPower;

        [ProtoMember]
        public SerializableRange? MaterialsMaxDepth;

        [ProtoMember]
        public SerializableRange? MaterialsMinDepth;

        [ProtoMember]
        public MyAtmosphereColorShift HostileAtmosphereColorShift;

        [ProtoMember]
        [XmlArrayItem("Material")]
        public MyPlanetMaterialDefinition[] CustomMaterialTable;

        [ProtoMember]
        [XmlArrayItem("Distortion")]
        public MyPlanetDistortionDefinition[] DistortionTable;

        [ProtoMember]
        public MyPlanetMaterialDefinition DefaultSurfaceMaterial;

        [ProtoMember]
        public MyPlanetMaterialDefinition DefaultSubSurfaceMaterial;

        [ProtoMember]
        public float? SurfaceGravity;

        [ProtoMember]
        public MyPlanetAtmosphere Atmosphere;

        [ProtoMember]
        public MyAtmosphereSettings? AtmosphereSettings;

        [ProtoMember]
        public string FolderName;

        [ProtoMember]
        public MyPlanetMaterialGroup[] ComplexMaterials;

        [ProtoMember]
        [XmlArrayItem("SoundRule")]
        public MySerializablePlanetEnvironmentalSoundRule[] SoundRules;

        [ProtoMember]
        [XmlArrayItem("MusicCategory")]
        public List<MyMusicCategory> MusicCategories = null;

        [ProtoMember]
        [XmlArrayItem("Ore")]
        public MyPlanetOreMapping[] OreMappings;

        [ProtoMember]
        [XmlArrayItem("Item")]
        public PlanetEnvironmentItemMapping[] EnvironmentItems;

        [ProtoMember]
        public MyPlanetMaterialBlendSettings? MaterialBlending;

        [ProtoMember]
        public MyPlanetSurfaceDetail SurfaceDetail;

        [ProtoMember]
        public MyPlanetAnimalSpawnInfo AnimalSpawnInfo;

        [ProtoMember]
        public MyPlanetAnimalSpawnInfo NightAnimalSpawnInfo;

        public float? SectorDensity;

        [ProtoMember]
        public string InheritFrom;

        public SerializableDefinitionId? Environment;
    }

}
