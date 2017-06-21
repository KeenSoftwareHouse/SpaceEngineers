using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationSMNode : MyObjectBuilder_Base
	{
        // all possible node types
	    public enum MySMNodeType
	    {
	        Normal,              // normal node
            PassThrough,         // pass-through node
            Any,                 // virtual node, any node in this state machine
            AnyExceptTarget      // virtual node, any node in this state machine except target of transition
	    }
        // name of this node
        [ProtoMember]
        public string Name;

        // name of underlying (EMBEDDED) state machine, if null it is just a simple node
        [ProtoMember]
        public string StateMachineName = null;

        // animation tree, null if StateMachineName is not null
        [ProtoMember]
        public MyObjectBuilder_AnimationTree AnimationTree = null;

        // position in editor.
	    [ProtoMember] 
        public Vector2I? EdPos;

        // type of the node.
        [ProtoMember]
        public MySMNodeType Type = MySMNodeType.Normal;
	}
}
