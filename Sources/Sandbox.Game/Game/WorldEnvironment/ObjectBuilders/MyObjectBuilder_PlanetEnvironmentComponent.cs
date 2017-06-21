using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Game;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;
using VRage;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PlanetEnvironmentComponent : MyObjectBuilder_ComponentBase
    {
        [XmlElement("Provider", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EnvironmentDataProvider>))]
        [DynamicNullableObjectBuilderItem]
        public MyObjectBuilder_EnvironmentDataProvider[] DataProviders = new MyObjectBuilder_EnvironmentDataProvider[6];

        public struct ObstructingBox
        {
            // Id of the obstructed sector.
            public long SectorId;

            // List of boxes that obstruct the sector.
            public List<MyOrientedBoundingBoxD> ObstructingBoxes;
        }

        [XmlArrayItem("Sector"), Nullable]
        public List<ObstructingBox> SectorObstructions;
    }
}
