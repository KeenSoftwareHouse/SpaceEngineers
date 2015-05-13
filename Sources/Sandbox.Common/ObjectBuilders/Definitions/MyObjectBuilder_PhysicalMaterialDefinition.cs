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
        [ProtoMember(1)]
        public float Density = 32000;

        [ProtoMember(2)]
        public float HorisontalTransmissionMultiplier = 1;

        [ProtoMember(3)]
        public float HorisontalFragility = 1;

        [ProtoMember(4)]
        public float SupportMultiplier = 1;

        [ProtoMember(5)]
        public float CollisionMultiplier = 1;
    }
}
