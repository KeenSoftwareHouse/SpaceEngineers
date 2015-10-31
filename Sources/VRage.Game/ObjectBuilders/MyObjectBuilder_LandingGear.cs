using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LandingGear : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsLocked;

        [ProtoMember]
        public float BrakeForce = 1f;

        [ProtoMember]
        public bool AutoLock;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string LockSound;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string UnlockSound;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string FailedAttachSound;
    }
}
