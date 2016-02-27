using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    // animation system layer - contains link to state machine and bone mask
	[ProtoContract]
	[MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationLayer : MyObjectBuilder_Base
	{
        [ProtoContract]
        public enum MyLayerMode
        {
            Replace,
            Add
        }

        // name of this layer
        [ProtoMember]
        public string Name;

        // layer mode: should we replace transformations that we currently have or combine them?
        [ProtoMember]
        public MyLayerMode Mode;

        // name of used animation SM
        [ProtoMember]
        public string StateMachine;

        // name of initial state machine node
        [ProtoMember]
        public string InitialSMNode = null;

        // bone mask of this SM, if null or empty, it affects all bones
        // bone names are separated with spaces
        [ProtoMember]
        public string BoneMask = null;
	}
}
