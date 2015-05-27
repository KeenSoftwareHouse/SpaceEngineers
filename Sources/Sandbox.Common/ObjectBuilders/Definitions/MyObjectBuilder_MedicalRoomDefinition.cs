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
    public class MyObjectBuilder_MedicalRoomDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public string IdleSound;

        [ProtoMember]
        public string ProgressSound;
    }
}
