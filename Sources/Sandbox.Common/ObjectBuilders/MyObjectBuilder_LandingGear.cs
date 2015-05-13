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
        [ProtoMember(1)]
        public bool IsLocked;

        [ProtoMember(2)]
        public float BrakeForce = 1f;

        [ProtoMember(3)]
        public bool AutoLock;

        [ProtoMember(4)]
        public string LockSound;

        [ProtoMember(5)]
        public string UnlockSound;

        [ProtoMember(6)]
        public string FailedAttachSound;
    }
}
