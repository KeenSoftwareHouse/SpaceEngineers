using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LandingGearDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public string LockSound;

        [ProtoMember(2)]
        public string UnlockSound;

        [ProtoMember(3)]
        public string FailedAttachSound;
    }
}
