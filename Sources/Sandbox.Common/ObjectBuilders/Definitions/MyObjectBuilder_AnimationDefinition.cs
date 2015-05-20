using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [Flags]
    public enum MyBonesArea
    {
        Body            = 1 << 0,
        LeftHand        = 1 << 1,
        RightHand       = 1 << 2,
        LeftFingers     = 1 << 3,
        RightFingers    = 1 << 4,
        Head            = 1 << 5,
        Spine           = 1 << 6,
    }


    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string AnimationModel;

        [ProtoMember]
        public int ClipIndex;

        [ProtoMember]
        public MyBonesArea InfluenceArea;

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
    }
}
