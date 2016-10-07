using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ObjectBuilders
{
    /// <summary>
    /// Base class of all object builders of animation tree nodes.
    /// </summary>
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

    /// <summary>
    /// Root node of the whole animation tree. Supports storing of orphaned nodes.
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationTree : MyObjectBuilder_AnimationTreeNode
    {
        // Link to first functional node of animation tree.
        [ProtoMember]
        public MyObjectBuilder_AnimationTreeNode Child;

        // Orphan nodes (not connected to root). Storing them because of editing of animation tree,
        // allowing artists to prepare a node structure which they can use later.
        [ProtoMember]
        public MyObjectBuilder_AnimationTreeNode[] Orphans;
    }

    /// <summary>
    /// Track node, storing information about track and playing settings.
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationTreeNodeTrack : MyObjectBuilder_AnimationTreeNode
    {
        /// <summary>
        /// Path to MWM file.
        /// </summary>
        [ProtoMember]
        public string PathToModel;

        /// <summary>
        /// Name of used track (animation) in MWM file.
        /// </summary>
        [ProtoMember]
        public string AnimationName;

        /// <summary>
        /// If true, animation will be looped. Default value is true.
        /// </summary>
        [ProtoMember]
        public bool Loop = true;

        /// <summary>
        /// Playing speed multiplier.
        /// </summary>
        [ProtoMember]
        public double Speed = 1.0f;

        /// <summary>
        /// Interpolate between keyframes. If false, track will be played frame by frame.
        /// </summary>
        [ProtoMember]
        public bool Interpolate = true;

        /// <summary>
        /// Synchronize time in this track with the specified layer.
        /// </summary>
        [ProtoMember] 
        public string SynchronizeWithLayer;
    }

    /// <summary>
    /// Helper struct: parameter mapping.
    /// </summary>
    [ProtoContract]
    public struct MyParameterAnimTreeNodeMapping
    {
        [ProtoMember]
        public float Param;  // parameter binding
        [ProtoMember]
        public MyObjectBuilder_AnimationTreeNode Node; // link to child node
    }

    /// <summary>
    /// Linear mixing node. Maps child nodes on 1D axis, interpolates according to parameter value.
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationTreeNodeMix1D : MyObjectBuilder_AnimationTreeNode
    {
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
        public MyParameterAnimTreeNodeMapping[] Children = null;

        /// <summary>
        /// True if the value wraps around.
        /// </summary>
        [ProtoMember]
        public bool Circular = false;

        /// <summary>
        /// Sensitivity to changes of parameter value. 1=immediate change, 0=no sensitivity.
        /// </summary>
        [ProtoMember]
        public float Sensitivity = 1.0f;

        /// <summary>
        /// Threshold: maximum change of variable to take sensitivity in account, if crossed, value is set immediatelly.
        /// </summary>
        [ProtoMember]
        public float? MaxChange;
    }

    /// <summary>
    /// Additive node. Child nodes are base node + additive node.
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationTreeNodeAdd: MyObjectBuilder_AnimationTreeNode
    {
        /// <summary>
        /// Name of parameter controlling blending inside this node.
        /// </summary>
        [ProtoMember]
        public string ParameterName;

        /// <summary>
        /// Child node, base "layer".
        /// </summary>
        [ProtoMember]
        public MyParameterAnimTreeNodeMapping BaseNode;

        /// <summary>
        /// Child node, additive "layer".
        /// </summary>
        [ProtoMember]
        public MyParameterAnimTreeNodeMapping AddNode;
    }
}
