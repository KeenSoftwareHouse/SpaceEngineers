using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

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
        public string LockSound;

        [ProtoMember]
        public string UnlockSound;

        [ProtoMember]
        public string FailedAttachSound;
    }
}
