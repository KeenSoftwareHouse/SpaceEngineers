using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Identity : MyObjectBuilder_Base
    {
        //[ProtoMember(1)] Obsolete!
        public long PlayerId
        {
            get { Debug.Fail("Obsolete."); return IdentityId; }
            set { IdentityId = value; }
        }
        public bool ShouldSerializePlayerId() { return false; }

        [ProtoMember(1)]
        public long IdentityId;

        [ProtoMember(2)]
        public string DisplayName;

        [ProtoMember(3)]
        public long CharacterEntityId;

        [ProtoMember(4)]
        public string Model;

        [ProtoMember(5)]
        public SerializableVector3? ColorMask;
        public bool ShouldSerializeColorMask() { return ColorMask != null; }
    }
}
