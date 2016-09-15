using ProtoBuf;
using System.Xml.Serialization;
using VRageRender.Animations;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
	[MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationSMTransition : MyObjectBuilder_Base
    {
        // name of transition, can be null (name should be used on manual actions)
        [ProtoMember]
        [XmlAttribute]
        public string Name = null;

        // name of the source state 
        [ProtoMember]
        [XmlAttribute]
        public string From;

        // name of the target state 
        [ProtoMember]
        [XmlAttribute]
        public string To;

        [ProtoMember]
        [XmlAttribute]
        public double TimeInSec = 0.0f;

        [ProtoMember]
        [XmlAttribute]
        public MyAnimationTransitionSyncType Sync = MyAnimationTransitionSyncType.Restart;

        // array of condition conjunctions - if any of conjunction is fulfilled, then SM will follow this transition 
        [ProtoMember]
        [XmlArrayItem("Conjunction")]
        public MyObjectBuilder_AnimationSMConditionsConjunction[] Conditions = null;

        // Priority of the transition, lower is processed sooner.
        // Transitions with unset priorities are processed as last ones.
        [ProtoMember]
        public int? Priority;
	}
}
