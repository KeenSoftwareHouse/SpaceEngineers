using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalMaterialDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public float Density = 32000;

        [ProtoMember]
        public float HorisontalTransmissionMultiplier = 1;

        [ProtoMember]
        public float HorisontalFragility = 1;

        [ProtoMember]
        public float SupportMultiplier = 1;

        [ProtoMember]
        public float CollisionMultiplier = 1;
    }
}
