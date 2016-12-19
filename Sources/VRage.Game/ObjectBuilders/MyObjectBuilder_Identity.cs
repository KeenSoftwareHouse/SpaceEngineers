using System;
using ProtoBuf;
using System.Diagnostics;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Identity : MyObjectBuilder_Base
    {
        //[ProtoMember] Obsolete!
        [NoSerialize]
        public long PlayerId
        {
            get { Debug.Fail("Obsolete."); return IdentityId; }
            set { IdentityId = value; }
        }
        public bool ShouldSerializePlayerId() { return false; }

        [ProtoMember]
        public long IdentityId;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)] 
        public string DisplayName;

        [ProtoMember]
        public long CharacterEntityId;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)] 
        public string Model;

        [ProtoMember]
        public SerializableVector3? ColorMask;
        public bool ShouldSerializeColorMask() { return ColorMask != null; }

        [ProtoMember]
        public int BlockLimitModifier;

        [ProtoMember]
        public DateTime LastLoginTime;
    }
}
