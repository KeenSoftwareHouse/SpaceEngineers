using ProtoBuf;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace Medieval.ObjectBuilders
{
    // Used in space as projectors but also using this in medieval for projectors as fundation block for blueprint building.
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MedievalProjector : MyObjectBuilder_ProjectorBase
    {
        
    }
}
