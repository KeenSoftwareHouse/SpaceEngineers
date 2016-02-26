using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    // animation state machine
	[ProtoContract]
	[MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationSM : MyObjectBuilder_Base
	{
        // name of this SM
        [ProtoMember]
        public string Name;

        // nodes of the SM
        [ProtoMember]
        [XmlArrayItem("Node")]
        public MyObjectBuilder_AnimationSMNode[] Nodes;

        // all transitions between nodes
        [ProtoMember]
        [XmlArrayItem("Transition")]
        public MyObjectBuilder_AnimationSMTransition[] Transitions;
	}
}
