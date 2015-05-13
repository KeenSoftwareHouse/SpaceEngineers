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
        [ProtoMember(1)]
        [ModdableContentFile("mwm")]
        public string AnimationModel;

        [ProtoMember(2)]
        public int ClipIndex;

        [ProtoMember(3)]
        public MyBonesArea InfluenceArea;

        [ProtoMember(4)]
        public bool AllowInCockpit = true;

        [ProtoMember(5)]
        public bool AllowWithWeapon;

        [ProtoMember(6)]
        public string SupportedSkeletons = "Humanoid";

        [ProtoMember(7)]
        public bool Loop;

        [ProtoMember(8)]
        public SerializableDefinitionId LeftHandItem;
    }
}
