using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlType("AnimationIkChain")]
    public class MyObjectBuilder_AnimationFootIkChain : MyObjectBuilder_Base
    {
        [ProtoMember]
        public string FootBone;
        [ProtoMember]
        public int ChainLength = 1;
        [ProtoMember]
        public bool AlignBoneWithTerrain = true;
    }
}