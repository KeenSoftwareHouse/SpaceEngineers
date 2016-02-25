using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender;

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
    public class MyPlanetMaterialDefinition
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
    public class MyPlanetMaterialPlacementRule : MyPlanetMaterialDefinition
    {
        [ProtoMember]
        public MyRangeValue Height = new MyRangeValue(0, 1);

        [ProtoMember]
        public MyReflectiveRangeValue Latitude = new MyReflectiveRangeValue(-90, 90);

        [ProtoMember]
        public MyRangeValue Longitude = new MyRangeValue(-180, 180);

        [ProtoMember]
        public MyRangeValue Slope = new MyRangeValue(0, 90);

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
    }

    [ProtoContract]
    public class MyPlanetSurfaceRule
    {
        [ProtoMember]
        public MyRangeValue Height = new MyRangeValue(0, 1);

        [ProtoMember]
        public MyReflectiveRangeValue Latitude = new MyReflectiveRangeValue(-90, 90);

        [ProtoMember]
        public MyRangeValue Longitude = new MyRangeValue(-180, 180);

        [ProtoMember]
        public MyRangeValue Slope = new MyRangeValue(0, 90);

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
    }

    /**
     * Rule group defines a material mappable set of surface rules.
     */
    [XmlType("MaterialGroup")]
    [ProtoContract]
    public class MyPlanetMaterialGroup
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
        public MyRangeValue Slope;

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
        public MyRangeValue Height = new MyRangeValue(0, 1);

        [ProtoMember]
        public MyReflectiveRangeValue Latitude = new MyReflectiveRangeValue(-90, 90);

        [ProtoMember]
        public MyRangeValue SunAngleFromZenith = new MyRangeValue(0, 180);

        [ProtoMember]
        public string EnvironmentSound;
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
    public struct MyRangeValue
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Min")]
        public float Min;
        [ProtoMember]
        [XmlAttribute(AttributeName = "Max")]
        public float Max;

        public MyRangeValue(float min, float max)
        {
            Max = max;
            Min = min;
        }

        public bool ValueBetween(float value)
        {
            return value >= Min && value <= Max;
        }

        public override string ToString()
        {
            return String.Format("Range[{0}, {1}]", Min, Max);
        }

        /**
         * When the range is an angle this method changes it to the cosines of the angle.
         * 
         * The angle is expected to be in degrees.
         * 
         * Also beware that cosine is a decreasing function in [0,90], for that reason the minimum and maximum are swaped.
         * 
         */
        public void ConvertToCosine()
        {
            float oldMax = Max;
            Max = (float)Math.Cos(Min * Math.PI / 180);
            Min = (float)Math.Cos(oldMax * Math.PI / 180);
        }

        /**
         * When the range is an angle this method changes it to the sines of the angle.
         * 
         * The angle is expected to be in degrees.
         */
        public void ConvertToSine()
        {
            Max = (float)Math.Sin(Max * Math.PI / 180);
            Min = (float)Math.Sin(Min * Math.PI / 180);
        }

        public void ConvertToCosineLongitude()
        {
            Max = MathHelper.MonotonicCosine((float)(Max * Math.PI / 180));
            Min = MathHelper.MonotonicCosine((float)(Min * Math.PI / 180));
        }

        public string ToStringAsin()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Asin(Min)), MathHelper.ToDegrees(Math.Asin(Max)));
        }

        public string ToStringAcos()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Acos(Min)), MathHelper.ToDegrees(Math.Acos(Max)));
        }

        public string ToStringLongitude()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(MathHelper.MonotonicAcos(Min)), MathHelper.ToDegrees(MathHelper.MonotonicAcos(Max)));
        }
    }

    /**
     * Reflective because it can be reflected to the oposite range.
     * 
     * Structs not inheriting from structs is stupid.
     */
    public struct MyReflectiveRangeValue
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Min")]
        public float Min;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Max")]
        public float Max;

        // Need this to force true to default.
        private bool m_notMirror;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Mirror")]
        public bool Mirror
        {
            get { return !m_notMirror; }
            set { m_notMirror = !value; }
        }

        public MyReflectiveRangeValue(float min, float max, bool mirror = true)
        {
            Max = max;
            Min = min;
            m_notMirror = !mirror;
        }

        public bool ValueBetween(float value)
        {
            if (!m_notMirror)
                value = Math.Abs(value);
            return value >= Min && value <= Max;
        }

        public override string ToString()
        {
            return String.Format("{0}[{1}, {2}]", Mirror ? "MirroredRange" : "Range", Min, Max);
        }

        /**
         * When the range is an angle this method changes it to the cosines of the angle.
         * 
         * The angle is expected to be in degrees.
         * 
         * Also beware that cosine is a decreasing function in [0,90], for that reason the minimum and maximum are swaped.
         * 
         */
        public void ConvertToCosine()
        {
            float oldMax = Max;
            Max = (float)Math.Cos(Min * Math.PI / 180);
            Min = (float)Math.Cos(oldMax * Math.PI / 180);
        }

        /**
         * When the range is an angle this method changes it to the sines of the angle.
         * 
         * The angle is expected to be in degrees.
         */
        public void ConvertToSine()
        {
            Max = (float)Math.Sin(Max * Math.PI / 180);
            Min = (float)Math.Sin(Min * Math.PI / 180);
        }

        public void ConvertToCosineLongitude()
        {
            Max = CosineLongitude(Max);
            Min = CosineLongitude(Min);
        }

        private static float CosineLongitude(float angle)
        {
            float val;
            if (angle > 0)
            {
                val = 2 - (float)Math.Cos(angle * Math.PI / 180);
            }
            else
            {
                val = (float)Math.Cos(angle * Math.PI / 180);
            }
            return val;
        }

        public string ToStringAsin()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Asin(Min)), MathHelper.ToDegrees(Math.Asin(Max)));
        }

        public string ToStringAcos()
        {
            return String.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Acos(Min)), MathHelper.ToDegrees(Math.Acos(Max)));
        }
    }

    [ProtoContract]
    public class MyAtmosphereColorShift
    {
        [ProtoMember]
        public MyRangeValue R = new MyRangeValue();

        [ProtoMember]
        public MyRangeValue G = new MyRangeValue();

        [ProtoMember]
        public MyRangeValue B = new MyRangeValue();
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
        public MyRangeValue? HillParams;

        [ProtoMember]
        public float? GravityFalloffPower;

        [ProtoMember]
        public MyRangeValue? MaterialsMaxDepth;

        [ProtoMember]
        public MyRangeValue? MaterialsMinDepth;

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
    }

}
