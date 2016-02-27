using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
	// This builder has been replaced by MyObjectBuilder_GasTank
	public class MyObjectBuilder_OxygenTank : MyObjectBuilder_GasTank
    {
    }
}
