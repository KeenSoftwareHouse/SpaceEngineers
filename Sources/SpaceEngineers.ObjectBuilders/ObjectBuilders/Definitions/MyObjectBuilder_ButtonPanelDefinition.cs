using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ButtonPanelDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public int ButtonCount;

        [ProtoMember]
        public string[] ButtonSymbols;

        [ProtoMember]
        public Vector4[] ButtonColors;

        [ProtoMember]
        public Vector4 UnassignedButtonColor;
    }
}
