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
        //[ProtoMember] Obsolete!
        public long PlayerId
        {
            get { Debug.Fail("Obsolete."); return IdentityId; }
            set { IdentityId = value; }
        }
        public bool ShouldSerializePlayerId() { return false; }

        [ProtoMember]
        public long IdentityId;

        [ProtoMember]
        public string DisplayName;

        [ProtoMember]
        public long CharacterEntityId;

        [ProtoMember]
        public string Model;

        [ProtoMember]
        public SerializableVector3? ColorMask;
        public bool ShouldSerializeColorMask() { return ColorMask != null; }
    }
}
