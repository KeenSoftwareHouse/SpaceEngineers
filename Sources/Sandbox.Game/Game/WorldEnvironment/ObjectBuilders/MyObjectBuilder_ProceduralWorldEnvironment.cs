using System.Diagnostics;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    public class MyEnvironmentItemTypeDefinition
    {
        // Name for the item
        [XmlAttribute]
        public string Name;

        // Lod to start considering this item.
        public int LodFrom = -1;

        // Definition for the procedural storage provider
        public SerializableDefinitionId? Provider;

        // List of runtime proxy modules.
        [XmlElement("Proxy")]
        public SerializableDefinitionId[] Proxies;
    }

    public class MyEnvironmentItemInfo
    {
        [XmlAttribute]
        public string Type;

        [XmlAttribute("Subtype")]
        public string SubtypeText
        {
            get { return Subtype.ToString(); }
            set { Subtype = MyStringHash.GetOrCompute(value); }
        }

        public MyStringHash Subtype;

        [XmlAttribute]
        public float Offset;

        [XmlAttribute]
        public float Density;
    }

    public class MyProceduralEnvironmentMapping
    {
        [XmlElement("Material")]
        public string[] Materials;

        [XmlElement("Biome")]
        public int[] Biomes;

        [XmlElement("Item")]
        public MyEnvironmentItemInfo[] Items;

        public SerializableRange Height = new SerializableRange(0, 1);

        public SymetricSerializableRange Latitude = new SymetricSerializableRange(-90, 90);

        public SerializableRange Longitude = new SerializableRange(-180, 180);

        public SerializableRange Slope = new SerializableRange(0, 90);
    }

    public enum MyProceduralScanningMethod
    {
        Random,
        Grid
    }

    [XmlType("VR.ProceduralWorldEnvironment")]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ProceduralWorldEnvironment : MyObjectBuilder_WorldEnvironmentBase
    {
        [XmlArrayItem("Item")]
        public MyEnvironmentItemTypeDefinition[] ItemTypes;

        [XmlArrayItem("Mapping")]
        public MyProceduralEnvironmentMapping[] EnvironmentMappings;

        public MyProceduralScanningMethod ScanningMethod = MyProceduralScanningMethod.Random;
    }
}
