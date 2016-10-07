using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [XmlType("VR.EI.VoxelMapCollection")]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMapCollectionDefinition : MyObjectBuilder_DefinitionBase
    {
        public struct VoxelMapStorage
        {
            [XmlAttribute("Storage")]
            public string Storage;

            [XmlAttribute("Probability")]
            public float Probability;
        }

        [XmlElement("Storage")]
        public VoxelMapStorage[] StorageDefs;

        public string Modifier;
    }
}
