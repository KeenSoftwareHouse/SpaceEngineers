using ProtoBuf;
using VRage.Data;
using VRage.ObjectBuilders;
using System.ComponentModel;
using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    public struct AnimationItem
    {
        [ProtoMember]
        public float Ratio;

        [ProtoMember]
        public string Animation;
    }

    [ProtoContract]
    public struct AnimationSet
    {
        [ProtoMember]
        public float Probability;

        [ProtoMember]
        public bool Continuous;

        [ProtoMember]
        public AnimationItem[] AnimationItems;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string AnimationModel;

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string AnimationModelFPS;

        [ProtoMember]
        public int ClipIndex;

        [ProtoMember]
        public string InfluenceArea;

        [ProtoMember]
        public bool AllowInCockpit = true;

        [ProtoMember]
        public bool AllowWithWeapon;

        [ProtoMember]
        public string SupportedSkeletons = "Humanoid";

        [ProtoMember]
        public bool Loop;

        [ProtoMember]
        public SerializableDefinitionId LeftHandItem;

        [ProtoMember, DefaultValue(null)]
        [XmlArrayItem("AnimationSet")]
        public AnimationSet[] AnimationSets;
    }
}