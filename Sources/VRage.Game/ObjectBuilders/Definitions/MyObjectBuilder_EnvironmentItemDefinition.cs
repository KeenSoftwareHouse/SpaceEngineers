using VRage.ObjectBuilders;
using ProtoBuf;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EnvironmentItemDefinition : MyObjectBuilder_PhysicalModelDefinition
    {
    }
}
