using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ConveyorTurretBase : MyObjectBuilder_TurretBase
    {
        [ProtoMember, DefaultValue(true)]
        public bool UseConveyorSystem = true;
    }
}
