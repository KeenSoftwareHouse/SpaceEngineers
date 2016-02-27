using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationTreeNode : MyObjectBuilder_Base
	{
        /// <summary>
        /// Position in editor.
        /// </summary>
	    [ProtoMember] 
        public Vector2I? EdPos;
	}

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationTree : MyObjectBuilder_AnimationTreeNode
    {
        // Root node of animation tree
        [ProtoMember]
        public MyObjectBuilder_AnimationTreeNode Child;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationTreeNodeTrack : MyObjectBuilder_AnimationTreeNode
    {
        /// <summary>
        /// Name of used track (animation).
        /// </summary>
        [ProtoMember]
        public string AnimationName;

        /// <summary>
        /// If true, animation will be looped. Default value is true.
        /// </summary>
        [ProtoMember]
        public bool Loop = true;

        /// <summary>
        /// Playing speed.
        /// </summary>
        [ProtoMember]
        public double Speed = 1.0f;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationTreeNodeMix1D : MyObjectBuilder_AnimationTreeNode
    {
        [ProtoContract]
        public struct MyParameterNodeMapping
        {
            [ProtoMember]
            public float Param;  // parameter binding
            [ProtoMember]
            public MyObjectBuilder_AnimationTreeNode Node; // link to child node
        }

        /// <summary>
        /// Name of parameter controlling blending inside this node.
        /// </summary>
        [ProtoMember]
        public string ParameterName;

        /// <summary>
        /// Mapping children to axis. Each child has assigned its value.
        /// </summary>
        [ProtoMember]
        [XmlElement("Child")]
        public MyParameterNodeMapping[] Children = null;
    }
}
