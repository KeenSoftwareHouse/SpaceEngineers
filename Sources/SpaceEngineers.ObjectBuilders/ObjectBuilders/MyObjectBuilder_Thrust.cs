using System.ComponentModel;
using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Thrust : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember, DefaultValue(0.0f)]
        public float ThrustOverride = 0.0f;
    }
}
