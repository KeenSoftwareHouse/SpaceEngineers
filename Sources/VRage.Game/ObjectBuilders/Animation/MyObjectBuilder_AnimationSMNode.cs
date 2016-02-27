using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationSMNode : MyObjectBuilder_Base
	{
        // name of this node
        [ProtoMember]
        public string Name;

        // name of underlying state machine, if null it is just a simple node
        [ProtoMember]
        public string StateMachineName = null;

        // animation tree, null if StateMachineName is not null
        [ProtoMember]
        public MyObjectBuilder_AnimationTree AnimationTree = null;

        // position in editor.
	    [ProtoMember] 
        public Vector2I? EdPos;
	}
}
