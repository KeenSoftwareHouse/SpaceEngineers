using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    // animation controller contains animation layers
	[ProtoContract]
	[MyObjectBuilderDefinition]
    [XmlType("AnimationControllerDefinition")]
    public class MyObjectBuilder_AnimationControllerDefinition : MyObjectBuilder_DefinitionBase
	{
        // animation layers
        [ProtoMember]
        [XmlArrayItem("Layer")]
        public MyObjectBuilder_AnimationLayer[] Layers;

        [ProtoMember]
        [XmlArrayItem("StateMachine")]
        public MyObjectBuilder_AnimationSM[] StateMachines;

        [ProtoMember]
        [XmlArrayItem("FootIkChain")]
        public MyObjectBuilder_AnimationFootIkChain[] FootIkChains;

        [ProtoMember]
        [XmlArrayItem("Bone")]
        public string[] IkIgnoredBones;

	    public MyObjectBuilder_AnimationControllerDefinition()
	    {
	        Id.TypeId = typeof(MyObjectBuilder_AnimationControllerDefinition);
	    }
    }
}
