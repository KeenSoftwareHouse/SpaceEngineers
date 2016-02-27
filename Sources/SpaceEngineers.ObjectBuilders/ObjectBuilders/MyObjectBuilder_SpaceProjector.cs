using ProtoBuf;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Projector : MyObjectBuilder_ProjectorBase
    {
        
    }
}
