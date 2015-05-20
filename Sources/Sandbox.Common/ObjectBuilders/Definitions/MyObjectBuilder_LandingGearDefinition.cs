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
        [ProtoMember]
        public string LockSound;

        [ProtoMember]
        public string UnlockSound;

        [ProtoMember]
        public string FailedAttachSound;
    }
}
